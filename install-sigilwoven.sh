#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
INSTALL_DIR="${XDG_BIN_HOME:-$HOME/.local/bin}"
TARGET="$INSTALL_DIR/sigilwoven"

mkdir -p "$INSTALL_DIR"
ln -sfn "$PROJECT_DIR/sigilwoven" "$TARGET"
chmod +x "$PROJECT_DIR/sigilwoven"

echo "Installed: $TARGET"
case ":${PATH}:" in
    *":$INSTALL_DIR:"*) ;;
    *) echo "Add this to your shell profile: export PATH=\"$INSTALL_DIR:\$PATH\"" ;;
esac
echo "Run: sigilwoven"
