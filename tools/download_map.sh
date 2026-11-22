#!/usr/bin/env bash
# Downloads the offline Helsinki map (16 GLBs + Unity .meta files) into
# Assets/TerraMap/Resources/Helsinki if any block is missing. Safe to re-run.
# Map: City of Helsinki 3D reality mesh 2017, CC BY 4.0 (see ATTRIBUTION.txt there).
set -euo pipefail

TERRA_MAP_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MAP_DIR="$TERRA_MAP_ROOT/Assets/TerraMap/Resources/Helsinki"
MAP_URL="${TERRA_MAP_MAP_URL:-https://github.com/Sri8397/terra-map/releases/download/helsinki-map-2017-v1/helsinki-map.zip}"
MAP_SHA256="4e5cf3403ccba9eba8670cd52ba311480dd7a94464038dd2ae6bb8e65e591b4c"

missing=0
for block in 672496 672497 673496 673497; do
  for part in a b c d; do
    name="Helsinki3D_2017_${block}${part}.glb"
    [[ -s "$MAP_DIR/$name" && -s "$MAP_DIR/$name.meta" ]] || missing=$((missing + 1))
  done
done
if [[ $missing -eq 0 ]]; then
  echo "terra-map: Helsinki map present (16 blocks)."
  exit 0
fi

echo "terra-map: $missing of 16 Helsinki blocks missing; downloading map (~106 MB)..."
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
curl -fL --retry 3 --progress-bar -o "$tmp/helsinki-map.zip" "$MAP_URL"
actual="$(shasum -a 256 "$tmp/helsinki-map.zip" | cut -d' ' -f1)"
if [[ "$actual" != "$MAP_SHA256" ]]; then
  echo "terra-map: checksum mismatch for $MAP_URL (got $actual, expected $MAP_SHA256)." >&2
  exit 1
fi
mkdir -p "$MAP_DIR"
unzip -oq "$tmp/helsinki-map.zip" -d "$MAP_DIR"
echo "terra-map: Helsinki map installed in $MAP_DIR."
