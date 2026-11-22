#!/usr/bin/env bash
# Opens terra-map's Unity project and entry scene. Press Play to start.
# Adapted from ../terra-game/tools/walk_editor.sh for this standalone project.
set -euo pipefail

TERRA_MAP_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TERRA_MAP_SCENE="$TERRA_MAP_ROOT/Assets/TerraMap/Scenes/TerraMap.unity"

if [[ -z "${UNITY:-}" ]]; then
  TERRA_MAP_EDITOR_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$TERRA_MAP_ROOT/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
  UNITY="/Applications/Unity/Hub/Editor/$TERRA_MAP_EDITOR_VERSION/Unity.app/Contents/MacOS/Unity"
fi

[[ -x "$UNITY" ]] || { echo "terra-map: Unity not found at $UNITY; set UNITY=/path/to/Unity." >&2; exit 1; }
[[ -f "$TERRA_MAP_SCENE" ]] || { echo "terra-map: entry scene is missing: $TERRA_MAP_SCENE" >&2; exit 1; }
# Download the Helsinki map before Unity imports, if any block is missing.
"$TERRA_MAP_ROOT/tools/download_map.sh"
printf 'Opening terra-map: %s\nPress Play in Unity to start.\n' "$TERRA_MAP_ROOT"
exec "$UNITY" -projectPath "$TERRA_MAP_ROOT" -openfile "$TERRA_MAP_SCENE" "$@"
