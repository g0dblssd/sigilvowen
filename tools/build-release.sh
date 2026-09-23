#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
BUILD_DIR="$PROJECT_DIR/build"

if [[ -n "${GODOT_BIN:-}" ]]; then
    GODOT="$GODOT_BIN"
elif command -v godot-mono >/dev/null 2>&1; then
    GODOT="$(command -v godot-mono)"
elif command -v godot >/dev/null 2>&1; then
    GODOT="$(command -v godot)"
else
    echo "Godot 4.7.2 .NET editor not found. Set GODOT_BIN to its executable." >&2
    exit 127
fi

rm -rf "$BUILD_DIR/linux" "$BUILD_DIR/windows"
mkdir -p "$BUILD_DIR/linux" "$BUILD_DIR/windows" "$BUILD_DIR/packages"
rm -f \
    "$BUILD_DIR/packages/Sigilwoven-linux-x86_64.tar.gz" \
    "$BUILD_DIR/packages/Sigilwoven-windows-x86_64.zip"

dotnet build "$PROJECT_DIR/Sigilwoven.csproj" --configuration Release
"$GODOT" --headless --path "$PROJECT_DIR" --export-release "Linux x86_64" "$BUILD_DIR/linux/Sigilwoven.x86_64"
if [[ ! -f "$BUILD_DIR/linux/Sigilwoven.x86_64" || ! -f "$BUILD_DIR/linux/data_Sigilwoven_linuxbsd_x86_64/Sigilwoven.dll" ]]; then
    echo "Linux export did not produce the executable and .NET runtime. Check matching .NET export templates." >&2
    exit 1
fi
"$GODOT" --headless --path "$PROJECT_DIR" --export-release "Windows x86_64" "$BUILD_DIR/windows/Sigilwoven.exe"
if [[ ! -f "$BUILD_DIR/windows/Sigilwoven.exe" || ! -f "$BUILD_DIR/windows/data_Sigilwoven_windows_x86_64/Sigilwoven.dll" ]]; then
    echo "Windows export did not produce the executable and .NET runtime. Check matching .NET export templates." >&2
    exit 1
fi

chmod +x "$BUILD_DIR/linux/Sigilwoven.x86_64"
tar -C "$BUILD_DIR/linux" -czf "$BUILD_DIR/packages/Sigilwoven-linux-x86_64.tar.gz" .

if command -v zip >/dev/null 2>&1; then
    (
        cd "$BUILD_DIR/windows"
        zip -q -r "$BUILD_DIR/packages/Sigilwoven-windows-x86_64.zip" .
    )
else
    echo "zip is not installed; Windows files were exported but not archived." >&2
fi

echo "Release packages: $BUILD_DIR/packages"
