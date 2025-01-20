#!/bin/sh
set -eu

ROOTDIR="$(readlink -f "$(dirname "$0")")"/../../../
cd "$ROOTDIR"

BUILDDIR=${BUILDDIR:-publish}
OUTDIR=${OUTDIR:-publish_appimage}
UFLAG=${UFLAG:-"gh-releases-zsync|Ryubing|ryujinx|latest|*-x64.AppImage.zsync"}

rm -rf AppDir
mkdir -p AppDir/usr/bin

cp distribution/linux/Ryujinx.desktop AppDir/Ryujinx.desktop
cp distribution/linux/appimage/AppRun AppDir/AppRun
cp distribution/misc/Logo.svg AppDir/Ryujinx.svg


cp -r "$BUILDDIR"/* AppDir/usr/bin/

# Ensure necessary bins are set as executable
chmod +x AppDir/AppRun AppDir/usr/bin/Ryujinx*

mkdir -p "$OUTDIR"

appimagetool --comp zstd --mksquashfs-opt -Xcompression-level --mksquashfs-opt 21 \
    -u "$UFLAG" \
    AppDir "$OUTDIR"/Ryujinx.AppImage

# Move zsync file needed for delta updates
if [ "$RELEASE" = "1" ]; then
    mv ./*.AppImage.zsync "$OUTDIR"
fi
