import argparse
import os
from pathlib import Path
import platform
import shutil
import subprocess

parser = argparse.ArgumentParser(
    description="Construct Universal dylibs for nuget package"
)
parser.add_argument(
    "arm64_input_directory", help="ARM64 Input directory containing dylibs"
)
parser.add_argument(
    "x86_64_input_directory", help="x86_64 Input directory containing dylibs"
)
parser.add_argument("output_directory", help="Output directory")
parser.add_argument("rglob", help="rglob")

args = parser.parse_args()

# Use Apple LLVM on Darwin, otherwise standard LLVM.
if platform.system() == "Darwin":
    LIPO = "lipo"
else:
    LIPO = shutil.which("llvm-lipo")

    if LIPO is None:
        for llvm_ver in [17, 16, 15, 14, 13]:
            lipo_path = shutil.which(f"llvm-lipo-{llvm_ver}")
            if lipo_path is not None:
                LIPO = lipo_path
                break

if LIPO is None:
    raise Exception("Cannot find a valid location for LLVM lipo!")

arm64_input_directory: Path = Path(args.arm64_input_directory)
x86_64_input_directory: Path = Path(args.x86_64_input_directory)
output_directory: Path = Path(args.output_directory)
rglob = args.rglob


def get_new_name(
    input_directory: Path, output_directory: str, input_dylib_path: Path
) -> Path:
    input_component = str(input_dylib_path).replace(str(input_directory), "")[1:]
    return Path(os.path.join(output_directory, input_component))


def is_fat_file(dylib_path: Path) -> str:
    res = subprocess.check_output([LIPO, "-info", str(dylib_path.absolute())]).decode(
        "utf-8"
    )

    return not res.split("\n")[0].startswith("Non-fat file")


def construct_universal_dylib(
    arm64_input_dylib_path: Path, x86_64_input_dylib_path: Path, output_dylib_path: Path
):
    if output_dylib_path.exists() or output_dylib_path.is_symlink():
        os.remove(output_dylib_path)

    os.makedirs(output_dylib_path.parent, exist_ok=True)

    if arm64_input_dylib_path.is_symlink():
        os.symlink(
            os.path.basename(arm64_input_dylib_path.resolve()), output_dylib_path
        )
    else:
        if is_fat_file(arm64_input_dylib_path) or not x86_64_input_dylib_path.exists():
            with open(output_dylib_path, "wb") as dst:
                with open(arm64_input_dylib_path, "rb") as src:
                    dst.write(src.read())
        else:
            subprocess.check_call(
                [
                    LIPO,
                    str(arm64_input_dylib_path.absolute()),
                    str(x86_64_input_dylib_path.absolute()),
                    "-output",
                    str(output_dylib_path.absolute()),
                    "-create",
                ]
            )


print(rglob)
for path in arm64_input_directory.rglob("**/*.dylib"):
    construct_universal_dylib(
        path,
        get_new_name(arm64_input_directory, x86_64_input_directory, path),
        get_new_name(arm64_input_directory, output_directory, path),
    )
