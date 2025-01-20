#!/bin/bash

set -e

if [ "$#" -lt 8 ]; then
    echo "usage <BASE_DIR> <TEMP_DIRECTORY> <OUTPUT_DIRECTORY> <ENTITLEMENTS_FILE_PATH> <VERSION> <SOURCE_REVISION_ID> <CONFIGURATION> <CANARY>"
    exit 1
fi

mkdir -p "$1"
mkdir -p "$2"
mkdir -p "$3"

BASE_DIR=$(readlink -f "$1")
TEMP_DIRECTORY=$(readlink -f "$2")
OUTPUT_DIRECTORY=$(readlink -f "$3")
ENTITLEMENTS_FILE_PATH=$(readlink -f "$4")
VERSION=$5
SOURCE_REVISION_ID=$6
CONFIGURATION=$7
CANARY=$8

if [ "$CANARY" == "1" ]; then
  RELEASE_TAR_FILE_NAME=nogui-ryujinx-canary-$VERSION-macos_universal.tar
elif [ "$VERSION" == "1.1.0" ]; then
  RELEASE_TAR_FILE_NAME=nogui-ryujinx-$CONFIGURATION-$VERSION+$SOURCE_REVISION_ID-macos_universal.tar
else
  RELEASE_TAR_FILE_NAME=nogui-ryujinx-$VERSION-macos_universal.tar
fi

ARM64_OUTPUT="$TEMP_DIRECTORY/publish_arm64"
X64_OUTPUT="$TEMP_DIRECTORY/publish_x64"
UNIVERSAL_OUTPUT="$OUTPUT_DIRECTORY/publish"
EXECUTABLE_SUB_PATH=Ryujinx.Headless.SDL2

rm -rf "$TEMP_DIRECTORY"
mkdir -p "$TEMP_DIRECTORY"

DOTNET_COMMON_ARGS=(-p:DebugType=embedded -p:Version="$VERSION" -p:SourceRevisionId="$SOURCE_REVISION_ID" --self-contained true $EXTRA_ARGS)

dotnet restore
dotnet build -c "$CONFIGURATION" src/Ryujinx.Headless.SDL2
dotnet publish -c "$CONFIGURATION" -r osx-arm64 -o "$TEMP_DIRECTORY/publish_arm64" "${DOTNET_COMMON_ARGS[@]}" src/Ryujinx.Headless.SDL2
dotnet publish -c "$CONFIGURATION" -r osx-x64 -o "$TEMP_DIRECTORY/publish_x64" "${DOTNET_COMMON_ARGS[@]}" src/Ryujinx.Headless.SDL2

# Get rid of the support library for ARMeilleure for x64 (that's only for arm64)
rm -rf "$TEMP_DIRECTORY/publish_x64/libarmeilleure-jitsupport.dylib"

# Get rid of libsoundio from arm64 builds as we don't have a arm64 variant
# TODO: remove this once done
rm -rf "$TEMP_DIRECTORY/publish_arm64/libsoundio.dylib"

rm -rf "$OUTPUT_DIRECTORY"
mkdir -p "$OUTPUT_DIRECTORY"

# Let's copy one of the two different outputs and remove the executable
cp -R "$ARM64_OUTPUT/" "$UNIVERSAL_OUTPUT"
rm "$UNIVERSAL_OUTPUT/$EXECUTABLE_SUB_PATH"

# Make its libraries universal
python3 "$BASE_DIR/distribution/macos/construct_universal_dylib.py" "$ARM64_OUTPUT" "$X64_OUTPUT" "$UNIVERSAL_OUTPUT" "**/*.dylib"

if ! [ -x "$(command -v lipo)" ];
then
    if ! [ -x "$(command -v llvm-lipo-17)" ];
    then
        LIPO=llvm-lipo
    else
        LIPO=llvm-lipo-17
    fi
else
    LIPO=lipo
fi

# Make the executable universal
$LIPO "$ARM64_OUTPUT/$EXECUTABLE_SUB_PATH" "$X64_OUTPUT/$EXECUTABLE_SUB_PATH" -output "$UNIVERSAL_OUTPUT/$EXECUTABLE_SUB_PATH" -create

# Now sign it
if ! [ -x "$(command -v codesign)" ];
then
    if ! [ -x "$(command -v rcodesign)" ];
    then
        echo "Cannot find rcodesign on your system, please install rcodesign."
        exit 1
    fi

    # NOTE: Currently require https://github.com/indygreg/apple-platform-rs/pull/44 to work on other OSes.
    # cargo install --git "https://github.com/marysaka/apple-platform-rs" --branch "fix/adhoc-app-bundle" apple-codesign --bin "rcodesign"
    echo "Using rcodesign for ad-hoc signing"
    for FILE in "$UNIVERSAL_OUTPUT"/*; do
        if [[ $(file "$FILE") == *"Mach-O"* ]]; then
            rcodesign sign --entitlements-xml-path "$ENTITLEMENTS_FILE_PATH" "$FILE"
        fi
    done  
else
    echo "Using codesign for ad-hoc signing"
    for FILE in "$UNIVERSAL_OUTPUT"/*; do
        if [[ $(file "$FILE") == *"Mach-O"* ]]; then
            codesign --entitlements "$ENTITLEMENTS_FILE_PATH" -f -s - "$FILE"
        fi
    done    
fi

echo "Creating archive"
pushd "$OUTPUT_DIRECTORY"
tar --exclude "publish/Ryujinx.Headless.SDL2" -cvf "$RELEASE_TAR_FILE_NAME" publish 1> /dev/null
python3 "$BASE_DIR/distribution/misc/add_tar_exec.py" "$RELEASE_TAR_FILE_NAME" "publish/Ryujinx.Headless.SDL2" "publish/Ryujinx.Headless.SDL2"
gzip -9 < "$RELEASE_TAR_FILE_NAME" > "$RELEASE_TAR_FILE_NAME.gz"
rm "$RELEASE_TAR_FILE_NAME"
popd

echo "Done"
