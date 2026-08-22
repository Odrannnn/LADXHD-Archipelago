#!/usr/bin/env bash

cd "$(dirname "$0")" || exit

cat <<EOF
----------------------------------------------
Link's Awakening DX HD - macOS Permissions Fix
----------------------------------------------

EOF

PATCHER_APP="LADXHD-Patcher.app"

if [ ! -d "$PATCHER_APP" ]; then
	echo "Error: Could not find 'LADXHD-Patcher.app' in this folder."
	exit 1
fi

echo "Found: $PATCHER_APP"

echo "Removing quarantine flag..."
xattr -cr "$PATCHER_APP" 2>/dev/null

echo "Setting execution permissions..."
chmod +x "$PATCHER_APP/Contents/MacOS/Patcher"

printf "\nDone! You can now run the patcher.\n\n"
