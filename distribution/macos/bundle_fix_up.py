import argparse
import hashlib
import os
from pathlib import Path
import platform
import shutil
import struct
import subprocess
from typing import List, Optional, Tuple

parser = argparse.ArgumentParser(description="Fixup for MacOS application bundle")
parser.add_argument("input_directory", help="Input directory (Application path)")
parser.add_argument("executable_sub_path", help="Main executable sub path")

# Use Apple LLVM on Darwin, otherwise standard LLVM.
if platform.system() == "Darwin":
    OTOOL = "otool"
    INSTALL_NAME_TOOL = "install_name_tool"
else:
    OTOOL = shutil.which("llvm-otool")
    if OTOOL is None:
        for llvm_ver in [17, 16, 15, 14, 13]:
            otool_path = shutil.which(f"llvm-otool-{llvm_ver}")
            if otool_path is not None:
                OTOOL = otool_path
                INSTALL_NAME_TOOL = shutil.which(f"llvm-install-name-tool-{llvm_ver}")
                break
    else:
        INSTALL_NAME_TOOL = shutil.which("llvm-install-name-tool")


args = parser.parse_args()


def get_dylib_id(dylib_path: Path) -> str:
    res = subprocess.check_output([OTOOL, "-D", str(dylib_path.absolute())]).decode(
        "utf-8"
    )

    return res.split("\n")[1]


def get_dylib_dependencies(dylib_path: Path) -> List[str]:
    output = (
        subprocess.check_output([OTOOL, "-L", str(dylib_path.absolute())])
        .decode("utf-8")
        .split("\n")[1:]
    )

    res = []

    for line in output:
        line = line.strip()
        index = line.find(" (compatibility version ")
        if index == -1:
            continue

        line = line[:index]

        res.append(line)

    return res


def replace_dylib_id(dylib_path: Path, new_id: str):
    subprocess.check_call(
        [INSTALL_NAME_TOOL, "-id", new_id, str(dylib_path.absolute())]
    )


def change_dylib_link(dylib_path: Path, old: str, new: str):
    subprocess.check_call(
        [INSTALL_NAME_TOOL, "-change", old, new, str(dylib_path.absolute())]
    )


def add_dylib_rpath(dylib_path: Path, rpath: str):
    subprocess.check_call(
        [INSTALL_NAME_TOOL, "-add_rpath", rpath, str(dylib_path.absolute())]
    )


def fixup_dylib(
    dylib_path: Path,
    replacement_path: str,
    search_path: List[str],
    content_directory: Path,
):
    dylib_id = get_dylib_id(dylib_path)
    new_dylib_id = replacement_path + "/" + os.path.basename(dylib_id)
    replace_dylib_id(dylib_path, new_dylib_id)

    dylib_dependencies = get_dylib_dependencies(dylib_path)
    dylib_new_mapping = {}

    for dylib_dependency in dylib_dependencies:
        if (
            not dylib_dependency.startswith("@executable_path")
            and not dylib_dependency.startswith("/usr/lib")
            and not dylib_dependency.startswith("/System/Library")
        ):
            dylib_dependency_name = os.path.basename(dylib_dependency)
            library_found = False
            for library_base_path in search_path:
                lib_path = Path(os.path.join(library_base_path, dylib_dependency_name))

                if lib_path.exists():
                    target_replacement_path = get_path_related_to_target_exec(
                        content_directory, lib_path
                    )

                    dylib_new_mapping[dylib_dependency] = (
                        target_replacement_path
                        + "/"
                        + os.path.basename(dylib_dependency)
                    )
                    library_found = True

            if not library_found:
                raise Exception(
                    f"{dylib_id}: Cannot find dependency {dylib_dependency_name} for fixup"
                )

    for key in dylib_new_mapping:
        change_dylib_link(dylib_path, key, dylib_new_mapping[key])


FILE_TYPE_ASSEMBLY = 1

ALIGN_REQUIREMENTS = 4096


def parse_embedded_string(data: bytes) -> Tuple[bytes, str]:
    first_byte = data[0]

    if (first_byte & 0x80) == 0:
        size = first_byte
        data = data[1:]
    else:
        second_byte = data[1]

        assert (second_byte & 0x80) == 0

        size = (second_byte << 7) | (first_byte & 0x7F)

        data = data[2:]

    res = data[:size].decode("utf-8")
    data = data[size:]

    return (data, res)


def write_embedded_string(file, string: str):
    raw_str = string.encode("utf-8")
    raw_str_len = len(raw_str)

    assert raw_str_len < 0x7FFF

    if raw_str_len > 0x7F:
        file.write(struct.pack("b", raw_str_len & 0x7F | 0x80))
        file.write(struct.pack("b", raw_str_len >> 7))
    else:
        file.write(struct.pack("b", raw_str_len))

    file.write(raw_str)


class BundleFileEntry(object):
    offset: int
    size: int
    compressed_size: int
    file_type: int
    relative_path: str
    data: bytes

    def __init__(
        self,
        offset: int,
        size: int,
        compressed_size: int,
        file_type: int,
        relative_path: str,
        data: bytes,
    ) -> None:
        self.offset = offset
        self.size = size
        self.compressed_size = compressed_size
        self.file_type = file_type
        self.relative_path = relative_path
        self.data = data

    def write(self, file):
        self.offset = file.tell()

        if (
            self.file_type == FILE_TYPE_ASSEMBLY
            and (self.offset % ALIGN_REQUIREMENTS) != 0
        ):
            padding_size = ALIGN_REQUIREMENTS - (self.offset % ALIGN_REQUIREMENTS)
            file.write(b"\0" * padding_size)
            self.offset += padding_size

        file.write(self.data)

    def write_header(self, file):
        file.write(
            struct.pack(
                "QQQb", self.offset, self.size, self.compressed_size, self.file_type
            )
        )
        write_embedded_string(file, self.relative_path)


class BundleManifest(object):
    major: int
    minor: int
    bundle_id: str
    deps_json: BundleFileEntry
    runtimeconfig_json: BundleFileEntry
    flags: int
    files: List[BundleFileEntry]

    def __init__(
        self,
        major: int,
        minor: int,
        bundle_id: str,
        deps_json: BundleFileEntry,
        runtimeconfig_json: BundleFileEntry,
        flags: int,
        files: List[BundleFileEntry],
    ) -> None:
        self.major = major
        self.minor = minor
        self.bundle_id = bundle_id
        self.deps_json = deps_json
        self.runtimeconfig_json = runtimeconfig_json
        self.flags = flags
        self.files = files

    def write(self, file) -> int:
        for bundle_file in self.files:
            bundle_file.write(file)

        bundle_header_offset = file.tell()
        file.write(struct.pack("iiI", self.major, self.minor, len(self.files)))
        write_embedded_string(file, self.bundle_id)

        if self.deps_json is not None:
            deps_json_location_offset = self.deps_json.offset
            deps_json_location_size = self.deps_json.size
        else:
            deps_json_location_offset = 0
            deps_json_location_size = 0

        if self.runtimeconfig_json is not None:
            runtimeconfig_json_location_offset = self.runtimeconfig_json.offset
            runtimeconfig_json_location_size = self.runtimeconfig_json.size
        else:
            runtimeconfig_json_location_offset = 0
            runtimeconfig_json_location_size = 0

        file.write(
            struct.pack("qq", deps_json_location_offset, deps_json_location_size)
        )
        file.write(
            struct.pack(
                "qq",
                runtimeconfig_json_location_offset,
                runtimeconfig_json_location_size,
            )
        )
        file.write(struct.pack("q", self.flags))

        for bundle_file in self.files:
            bundle_file.write_header(file)

        return bundle_header_offset


def read_file_entry(
    raw_data: bytes, header_bytes: bytes
) -> Tuple[bytes, BundleFileEntry]:
    (
        offset,
        size,
        compressed_size,
        file_type,
    ) = struct.unpack("QQQb", header_bytes[:0x19])
    (header_bytes, relative_path) = parse_embedded_string(header_bytes[0x19:])

    target_size = compressed_size

    if target_size == 0:
        target_size = size

    return (
        header_bytes,
        BundleFileEntry(
            offset,
            size,
            compressed_size,
            file_type,
            relative_path,
            raw_data[offset : offset + target_size],
        ),
    )


def get_dotnet_bundle_data(data: bytes) -> Optional[Tuple[int, int, BundleManifest]]:
    offset = data.find(hashlib.sha256(b".net core bundle\n").digest())

    if offset == -1:
        return None

    raw_header_offset = data[offset - 8 : offset]
    (header_offset,) = struct.unpack("q", raw_header_offset)
    header_bytes = data[header_offset:]

    (
        major,
        minor,
        files_count,
    ) = struct.unpack("iiI", header_bytes[:0xC])
    header_bytes = header_bytes[0xC:]

    (header_bytes, bundle_id) = parse_embedded_string(header_bytes)

    # v2 header
    (
        deps_json_location_offset,
        deps_json_location_size,
    ) = struct.unpack("qq", header_bytes[:0x10])
    (
        runtimeconfig_json_location_offset,
        runtimeconfig_json_location_size,
    ) = struct.unpack("qq", header_bytes[0x10:0x20])
    (flags,) = struct.unpack("q", header_bytes[0x20:0x28])
    header_bytes = header_bytes[0x28:]

    files = []

    deps_json = None
    runtimeconfig_json = None

    for _ in range(files_count):
        (header_bytes, file_entry) = read_file_entry(data, header_bytes)

        files.append(file_entry)

        if file_entry.offset == deps_json_location_offset:
            deps_json = file_entry
        elif file_entry.offset == runtimeconfig_json_location_offset:
            runtimeconfig_json = file_entry

    file_entry = files[0]

    return (
        file_entry.offset,
        header_offset,
        BundleManifest(
            major, minor, bundle_id, deps_json, runtimeconfig_json, flags, files
        ),
    )


LC_SYMTAB = 0x2
LC_SEGMENT_64 = 0x19
LC_CODE_SIGNATURE = 0x1D


def fixup_linkedit(file, data: bytes, new_size: int):
    offset = 0

    (
        macho_magic,
        macho_cputype,
        macho_cpusubtype,
        macho_filetype,
        macho_ncmds,
        macho_sizeofcmds,
        macho_flags,
        macho_reserved,
    ) = struct.unpack("IiiIIIII", data[offset : offset + 0x20])

    offset += 0x20

    linkedit_offset = None
    symtab_offset = None
    codesign_offset = None

    for _ in range(macho_ncmds):
        (cmd, cmdsize) = struct.unpack("II", data[offset : offset + 8])

        if cmd == LC_SEGMENT_64:
            (
                cmd,
                cmdsize,
                segname_raw,
                vmaddr,
                vmsize,
                fileoff,
                filesize,
                maxprot,
                initprot,
                nsects,
                flags,
            ) = struct.unpack("II16sQQQQiiII", data[offset : offset + 72])
            segname = segname_raw.decode("utf-8").split("\0")[0]

            if segname == "__LINKEDIT":
                linkedit_offset = offset
        elif cmd == LC_SYMTAB:
            symtab_offset = offset
        elif cmd == LC_CODE_SIGNATURE:
            codesign_offset = offset

        offset += cmdsize
        pass

    assert linkedit_offset is not None and symtab_offset is not None

    # If there is a codesign section, clean it up.
    if codesign_offset is not None:
        (
            codesign_cmd,
            codesign_cmdsize,
            codesign_dataoff,
            codesign_datasize,
        ) = struct.unpack("IIII", data[codesign_offset : codesign_offset + 16])
        file.seek(codesign_offset)
        file.write(b"\0" * codesign_cmdsize)

        macho_ncmds -= 1
        macho_sizeofcmds -= codesign_cmdsize
        file.seek(0)
        file.write(
            struct.pack(
                "IiiIIIII",
                macho_magic,
                macho_cputype,
                macho_cpusubtype,
                macho_filetype,
                macho_ncmds,
                macho_sizeofcmds,
                macho_flags,
                macho_reserved,
            )
        )

        file.seek(codesign_dataoff)
        file.write(b"\0" * codesign_datasize)

    (
        symtab_cmd,
        symtab_cmdsize,
        symtab_symoff,
        symtab_nsyms,
        symtab_stroff,
        symtab_strsize,
    ) = struct.unpack("IIIIII", data[symtab_offset : symtab_offset + 24])

    symtab_strsize = new_size - symtab_stroff

    new_symtab = struct.pack(
        "IIIIII",
        symtab_cmd,
        symtab_cmdsize,
        symtab_symoff,
        symtab_nsyms,
        symtab_stroff,
        symtab_strsize,
    )

    file.seek(symtab_offset)
    file.write(new_symtab)

    (
        linkedit_cmd,
        linkedit_cmdsize,
        linkedit_segname_raw,
        linkedit_vmaddr,
        linkedit_vmsize,
        linkedit_fileoff,
        linkedit_filesize,
        linkedit_maxprot,
        linkedit_initprot,
        linkedit_nsects,
        linkedit_flags,
    ) = struct.unpack("II16sQQQQiiII", data[linkedit_offset : linkedit_offset + 72])

    linkedit_filesize = new_size - linkedit_fileoff
    linkedit_vmsize = linkedit_filesize

    new_linkedit = struct.pack(
        "II16sQQQQiiII",
        linkedit_cmd,
        linkedit_cmdsize,
        linkedit_segname_raw,
        linkedit_vmaddr,
        linkedit_vmsize,
        linkedit_fileoff,
        linkedit_filesize,
        linkedit_maxprot,
        linkedit_initprot,
        linkedit_nsects,
        linkedit_flags,
    )
    file.seek(linkedit_offset)
    file.write(new_linkedit)


def write_bundle_data(
    output,
    old_bundle_base_offset: int,
    new_bundle_base_offset: int,
    bundle: BundleManifest,
) -> int:
    # Write bundle data
    bundle_header_offset = bundle.write(output)
    total_size = output.tell()

    # Patch the header position
    offset = file_data.find(hashlib.sha256(b".net core bundle\n").digest())
    output.seek(offset - 8)
    output.write(struct.pack("q", bundle_header_offset))

    return total_size - new_bundle_base_offset


input_directory: Path = Path(args.input_directory)
content_directory: Path = Path(os.path.join(args.input_directory, "Contents"))
executable_path: Path = Path(os.path.join(content_directory, args.executable_sub_path))


def get_path_related_to_other_path(a: Path, b: Path) -> str:
    temp = b

    parts = []

    while temp != a:
        temp = temp.parent
        parts.append(temp.name)

    parts.remove(parts[-1])
    parts.reverse()

    return "/".join(parts)


def get_path_related_to_target_exec(input_directory: Path, path: Path):
    return "@executable_path/../" + get_path_related_to_other_path(
        input_directory, path
    )


search_path = [
    Path(os.path.join(content_directory, "Frameworks")),
    Path(os.path.join(content_directory, "Resources/lib")),
]


for path in content_directory.rglob("**/*.dylib"):
    current_search_path = [path.parent]
    current_search_path.extend(search_path)

    fixup_dylib(
        path,
        get_path_related_to_target_exec(content_directory, path),
        current_search_path,
        content_directory,
    )

for path in content_directory.rglob("**/*.so"):
    current_search_path = [path.parent]
    current_search_path.extend(search_path)

    fixup_dylib(
        path,
        get_path_related_to_target_exec(content_directory, path),
        current_search_path,
        content_directory,
    )


with open(executable_path, "rb") as input:
    file_data = input.read()


(bundle_base_offset, bundle_header_offset, bundle) = get_dotnet_bundle_data(file_data)

add_dylib_rpath(executable_path, "@executable_path/../Frameworks/")

# Recent "vanilla" version of LLVM (LLVM 13 and upper) seems to really dislike how .NET package its assemblies.
# As a result, after execution of install_name_tool it will have "fixed" the symtab resulting in a missing .NET bundle...
# To mitigate that, we check if the bundle offset inside the binary is valid after install_name_tool and readd .NET bundle if not.
output_file_size = os.stat(executable_path).st_size
if output_file_size < bundle_header_offset:
    print("LLVM broke the .NET bundle, readding bundle data...")
    with open(executable_path, "r+b") as output:
        file_data = output.read()
        bundle_data_size = write_bundle_data(
            output, bundle_base_offset, output_file_size, bundle
        )

        # Now patch the __LINKEDIT section
        new_size = output_file_size + bundle_data_size
        fixup_linkedit(output, file_data, new_size)
