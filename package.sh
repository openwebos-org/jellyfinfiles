#!/usr/bin/env bash
set -euo pipefail

VERSION="1.0.0.0"
OUT="dist"
ROOT="package/WebFileManager"

rm -rf "$OUT" package
mkdir -p "$ROOT" "$OUT"

if [ ! -f "publish/Jellyfin.Plugin.WebFileManager.dll" ]; then
  echo "publish/Jellyfin.Plugin.WebFileManager.dll not found"
  exit 1
fi

cp publish/Jellyfin.Plugin.WebFileManager.dll "$ROOT/"
cp publish/Jellyfin.Plugin.WebFileManager.pdb "$ROOT/" 2>/dev/null || true

cat > "$ROOT/meta.json" <<EOF
{
  "guid": "4a5d7d7e-1b2e-4c64-a3c1-8b5f2a9e71d4",
  "name": "Web File Manager",
  "description": "Web file manager for the Jellyfin /media directory.",
  "overview": "Browse and manage Jellyfin media files from the dashboard.",
  "owner": "Thirdy",
  "category": "General",
  "version": "$VERSION",
  "targetAbi": "12.0.0.0"
}
EOF

(
  cd package
  zip -r "../$OUT/Jellyfin.Plugin.WebFileManager-$VERSION-jf12.zip" WebFileManager
)
