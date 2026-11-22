"""Source/asset contract check; never opens Unity or a device."""
import json
from pathlib import Path
import re
import struct

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
scripts = ASSETS / "TerraMap/Scripts"
required = {"TerraMapGame", "TerraMapPlayer", "TerraMapAvatar", "TerraMapControls", "TerraMapPad", "WalkDress"}
assert {p.stem for p in scripts.glob("*.cs")} == required
guids = {}
for asset in ASSETS.rglob("*"):
    # Unity ignores hidden files (macOS .DS_Store) and gives them no .meta.
    if asset.suffix == ".meta" or asset.name.startswith("."):
        continue
    meta = Path(str(asset) + ".meta")
    assert meta.is_file(), f"Missing metadata: {asset}"
    guid = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.M)[1]
    assert guid not in guids, f"Duplicate GUID: {guid}"
    guids[guid] = asset

scene = ASSETS / "TerraMap/Scenes/TerraMap.unity"
for guid in re.findall(r"guid: ([0-9a-f]{32})", scene.read_text()):
    assert guid.startswith("0000000000000000") or guid in guids, f"Broken scene reference: {guid}"
build = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text()
assert "Assets/TerraMap/Scenes/TerraMap.unity" in build
assert re.search(r"guid: (\w+)", Path(str(scene) + ".meta").read_text())[1] in build
quality = (ROOT / "ProjectSettings/QualitySettings.asset").read_text()
assert re.findall(r"    name: (.*)", quality) == ["Full Quality"]
settings = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text()
assert "Android: com.taurotechnologies.terramap" in settings
assert "androidUseCustomKeystore: 0" in settings

packages = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
assert all(name in packages for name in ["com.unity.cloud.gltfast", "com.unity.inputsystem", "com.unity.ugui"])
assert "com.cesium.unity" not in packages
for source in [*scripts.glob("*.cs"), *(ASSETS / "TerraMap/Editor").glob("*.cs")]:
    text = source.read_text()
    assert not any(token in text for token in ["UnityWebRequest", "HttpClient", "Cesium", "PlayerPrefs", "TerraWeapon"]), source

def glb_json(path):
    data = path.read_bytes()
    magic, version, size = struct.unpack_from("<III", data)
    assert (magic, version, size) == (0x46546C67, 2, len(data)), path
    length, kind = struct.unpack_from("<II", data, 12)
    assert kind == 0x4E4F534A, path
    content = json.loads(data[20:20 + length])
    assert not any("uri" in entry for category in ("buffers", "images") for entry in content.get(category, [])), f"External asset dependency: {path}"
    return content

player = ASSETS / "TerraMap/Resources/Player"
assert {p.name for p in player.glob("*.glb")} == {"Male_Peasant.glb", "Head_Male.glb", "Hair_Buzzed.glb", "UAL1_Standard.glb"}
for path in player.glob("*.glb"):
    glb_json(path)
animations = {a["name"] for a in glb_json(player / "UAL1_Standard.glb")["animations"]}
selected = set(re.findall(r'"([A-Za-z_]+)"', (scripts / "TerraMapAvatar.cs").read_text().split("static readonly string[] Names =")[1].split(";")[0]))
assert selected <= animations, f"Missing clips: {selected - animations}"
for action in ["Punch", "Jump", "ToggleJet", "Return", "ChangeView", "ResetToStreet"]:
    assert f"player.{action}(" in (scripts / "TerraMapControls.cs").read_text() or f"Player.{action}(" in (scripts / "TerraMapControls.cs").read_text(), action

map_dir = ASSETS / "TerraMap/Resources/Helsinki"
expected_tiles = {f"Helsinki3D_2017_{block}{part}.glb" for block in ("672496", "672497", "673496", "673497") for part in "abcd"}
present = {p.name for p in map_dir.glob("*.glb")}
if present:
    assert present == expected_tiles, "Helsinki folder must contain exactly the 16 expected GLBs; re-run tools/download_map.sh"
    for path in map_dir.glob("*.glb"):
        assert Path(str(path) + ".meta").is_file(), f"Missing metadata: {path}"
        glb_json(path)
else:
    print("NOTE: Helsinki map not downloaded yet (tools/walk_editor.sh or tools/download_map.sh fetches it); GLB checks skipped.")
download = (ROOT / "tools/download_map.sh").read_text()
assert "helsinki-map-2017-v1/helsinki-map.zip" in download and re.search(r'MAP_SHA256="[0-9a-f]{64}"', download)
assert "tools/download_map.sh" in (ROOT / "tools/walk_editor.sh").read_text(), "walk_editor.sh must download a missing map"
attribution = map_dir / "ATTRIBUTION.txt"
assert attribution.is_file() and "CC BY 4.0" in attribution.read_text(), "Helsinki ATTRIBUTION.txt missing or lacks CC BY 4.0"
assert not (ASSETS / "TerraMap/Resources/NewYork").exists(), "Old New York map must be removed"

assert not (ROOT / "RawMaps/DA_WISE_Multipatch.zip").exists(), "Old New York source archive must be removed"
print("PASS: source project, scene references, fixed quality, offline-only code, player assets and animation contracts.")
print("Unity compilation, gameplay and device performance: NOT CHECKED.")
