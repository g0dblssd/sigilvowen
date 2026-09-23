#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
SOURCE_DIR="${1:-$PROJECT_DIR/build/linux}"
OUTPUT_FILE="${2:-$PROJECT_DIR/build/installers/Sigilwoven-Installer-Linux-x86_64.run}"
HEADER="$PROJECT_DIR/installer/linux/install-header.sh"

if [[ ! -x "$SOURCE_DIR/Sigilwoven.x86_64" || ! -f "$SOURCE_DIR/data_Sigilwoven_linuxbsd_x86_64/Sigilwoven.dll" ]]; then
    echo "A complete Linux export was not found in $SOURCE_DIR" >&2
    exit 1
fi

mkdir -p "$(dirname -- "$OUTPUT_FILE")"
cp "$HEADER" "$OUTPUT_FILE"
tar -C "$SOURCE_DIR" -czf - . >> "$OUTPUT_FILE"
chmod +x "$OUTPUT_FILE"
echo "Linux installer: $OUTPUT_FILE"
