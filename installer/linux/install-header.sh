#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/sigilwoven"
BIN_DIR="${XDG_BIN_HOME:-$HOME/.local/bin}"
APPLICATION_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/applications"

printf 'Installing Sigilwoven to %s\n' "$APP_DIR"
mkdir -p "$APP_DIR" "$BIN_DIR" "$APPLICATION_DIR"

ARCHIVE_LINE="$(awk '/^__SIGILWOVEN_ARCHIVE_BELOW__$/ { print NR + 1; exit }' "$0")"
if [[ -z "$ARCHIVE_LINE" ]]; then
    echo "Installer payload marker is missing." >&2
    exit 1
fi

tail -n +"$ARCHIVE_LINE" "$0" | tar -xz -C "$APP_DIR"
chmod +x "$APP_DIR/Sigilwoven.x86_64"
ln -sfn "$APP_DIR/Sigilwoven.x86_64" "$BIN_DIR/sigilwoven"

DESKTOP_FILE="$APPLICATION_DIR/sigilwoven.desktop"
printf '%s\n' \
    '[Desktop Entry]' \
    'Type=Application' \
    'Name=Sigilwoven' \
    'Comment=Dark fantasy action RPG' \
    "Exec=$APP_DIR/Sigilwoven.x86_64" \
    'Terminal=false' \
    'Categories=Game;RolePlaying;' \
    > "$DESKTOP_FILE"
chmod +x "$DESKTOP_FILE"

printf '\nInstalled successfully.\n'
printf 'Launch from the application menu or run: %s\n' "$BIN_DIR/sigilwoven"
exit 0
__SIGILWOVEN_ARCHIVE_BELOW__
