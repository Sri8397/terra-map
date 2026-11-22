# terra-map

Independent Unity 6000.6.8f1 Android game source. One player, fists (jab/cross), jetpack,
touch and keyboard/mouse controls. Fixed full quality; no lobby, settings, guns,
vehicles, missions, analytics, purchases, Cesium, or online map requests.

Entry scene: `Assets/TerraMap/Scenes/TerraMap.unity`.
The app identifier is `com.taurotechnologies.terramap`, separate from TERRA.

## Open in Unity

Copy and paste into Terminal:

```sh
cd /Users/srikant/Documents/srikant/terra-map
./tools/walk_editor.sh
```

That is the only step. On first run (or whenever any map block is missing) it downloads
the Helsinki map (~106 MB, needs internet once) from the GitHub release
[`helsinki-map-2017-v1`](https://github.com/Sri8397/terra-map/releases/tag/helsinki-map-2017-v1),
checks its SHA-256 and unpacks it into `Assets/TerraMap/Resources/Helsinki/`, then opens
this project and `Assets/TerraMap/Scenes/TerraMap.unity` in Unity **6000.6.8f1**
(adapted from `../terra-game/tools/walk_editor.sh`). Press **Play**.
Stop **Play** to unload the runtime map and player. Use one Unity instance for
this project. For another editor location:

```sh
UNITY=/path/to/Unity ./tools/walk_editor.sh
```

The map is the full offline Helsinki 3D reality mesh (see Map setup). It was moved from
`../terra-game`, and this project has **not been opened in Unity yet**: import, gameplay,
frame rate and Android APK size (the map is 106 MB) are **not verified**.
`TerraMapPlayStart.cs` (Editor) forces Play to start `Assets/TerraMap/Scenes/TerraMap.unity`.
glTFast was upgraded 6.14.1 -> 6.20.0 (fixes a skinned multi-primitive import crash of
`UAL1_Standard.glb`).

## Current state

Source prepared and statically checked only. Unity has not been opened; compilation,
import of the map and glTFast upgrade, gameplay, rendering, frame rate, device
performance and Android packaging (including APK size with the 106 MB map) remain
unverified. If the map files are missing the game shows a missing-map message and blocks
movement.

## Map setup

`Assets/TerraMap/Resources/Helsinki/` holds sixteen self-contained GLBs,
`Helsinki3D_2017_{672496,672497,673496,673497}{a,b,c,d}.glb` (500 m blocks, 106 MB total),
plus `ATTRIBUTION.txt`. The GLBs and their `.meta` files are not in git: they come from
`helsinki-map.zip` on the `helsinki-map-2017-v1` release, fetched by `tools/walk_editor.sh`.
To fetch the map without opening Unity, run `./tools/download_map.sh` (it does nothing when
all 16 blocks are present; delete a block to force a re-download). They are the City of Helsinki 3D reality mesh 2017
(photogrammetric, photo-textured, unlit materials via `KHR_materials_unlit`), converted
in terra-game (the converter tools stay there) and moved here, not copied. Coverage is
2 km x 2 km: X (east) and Z (north) in [-1000, 1000] m around EPSG:3879
E 25497000 N 6673000 (60.169871 N, 24.945959 E). Y is N2000 height in metres, not zero at
street level. Spawn is the Senate Square street at X 363, Z -57. Mesh colliders are added
at startup. Once included in a build the map lives in the installed application and needs
no downloads.

Attribution (CC BY 4.0): Contains the Helsinki 3D reality mesh 2017 by the City of
Helsinki, licensed under Creative Commons Attribution 4.0 International (CC BY 4.0),
https://creativecommons.org/licenses/by/4.0/ . Changes: level of detail L18 only,
textures repacked into atlases, recentred, split into 500 m GLBs.

## Controls

| Action | Desktop | Touch |
|---|---|---|
| Move/look | WASD/arrows; mouse | MOVE pad; drag background |
| Punch | Hold left mouse | Hold FIST |
| Run/jump | Hold Shift; Space | RUN toggle; JUMP |
| Crouch/prone | C/Ctrl; Z | CROUCH; PRONE |
| Jetpack | Q | JET |
| Ascend/descend | Space; C/Ctrl | UP; DOWN |
| Return to takeoff point | Y | RETURN |
| First/close/far view | P | VIEW |
| Free look | V | EYE |
| Reset to street | G | RESET |
| Pause/resume | Esc | PAUSE/RESUME |
| Release/capture mouse | Backtick | — |

Movement is limited to X/Z within ±999.6 m of the map centre (the 2 km square) and flight to 1,200 m height. Punches animate and
apply physics impulse; objects can receive damage through `TakePunch(float)`.
No enemies, training targets or weapon systems are included.

## Checks

Run `python3 tools/check_source.py`. This checks project wiring, local asset
dependencies, required animation clips, and absence of online map code. It does not
compile C# or run Unity. Full resolution, 8× MSAA and high shadows are fixed requests;
actual hardware support, memory use and frame rate need a device check.

## Sources

- Player outfit, head and hair: existing TERRA Quaternius assets (CC0),
  https://quaternius.com/packs/modularcharacteroutfitsfantasy.html
- Animation library: Quaternius Universal Animation Library (CC0),
  https://quaternius.com/packs/universalanimationlibrary.html
- Map: City of Helsinki 3D reality mesh 2017, CC BY 4.0,
  https://hri.fi/data/en_GB/dataset/helsingin-3d-kaupunkimalli
