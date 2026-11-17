# Sprint Mod for Car Mechanic Simulator 2018

Customizable sprinting with native pause-menu settings for **Car Mechanic Simulator 2018** on Windows, powered by **BepInEx 5**.

[![Preview](https://img.shields.io/badge/preview-v1.3.0-orange)](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.3.0)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](#requirements)
[![All releases](https://img.shields.io/badge/downloads-all_releases-green)](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases)

## Download

**[Download v1.3.0 preview](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/download/v1.3.0/SPRINT_MOD_CMS_2018_v1.3.0_PREVIEW.zip)** · [Release notes](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.3.0) · [All versions](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases)

The newest package includes the plugin DLL, complete source, build script, documentation, and checksums. **v1.3.0 is a preview:** typed speed input and footstep audio changes still need live gameplay testing. For an older version whose native settings menu was user-confirmed working, see [v1.1.0](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.1.0).

## Features

- Hold or toggle sprint with a configurable keyboard binding.
- Adjustable movement speed from 100% to 250%.
- Native **Sprint Mod Settings** submenu above **Continue** in the pause menu.
- Optional crouched sprint, sprint enable/disable, and reset controls.
- Footstep playback scales with active sprint speed in v1.3.0; playback pitch changes too.

Defaults: **either Shift key**, **hold to sprint**, **150% speed**, crouched sprint **off**.

## Requirements

- Car Mechanic Simulator 2018 on Windows.
- BepInEx 5 installed in the game folder.

The supplied build was compiled against BepInEx 5.4.23.5, Harmony 2.9.0, and Unity 2017.4.24. Game assemblies and third-party dependencies are not bundled.

## Installation

1. Close the game and move any existing sprint mod DLL outside `BepInEx/plugins` as a backup.
2. Download the **versioned ZIP** from a release's Assets section. GitHub's automatic source archives are for development and do not contain the compiled plugin.
3. For v1.3.0, extract the ZIP and merge its `BepInEx` folder into the game folder. The installed file should be:
   ```text
   Car Mechanic Simulator 2018/
   └── BepInEx/
       └── plugins/
           └── SPRINT MOD CMS 2018.dll
   ```
4. Start the game and open **Escape → Sprint Mod Settings**.

**Install only one version at a time.** Older archive ZIPs contain the DLL at their root; copy that DLL into `BepInEx/plugins` yourself.

## Settings

Click the speed field, type an integer, and press **Enter** or click away. Values are clamped to 100–250%; empty or invalid input retains the previous setting. **Escape** cancels rebinding or returns from the settings submenu; **Back** also returns.

Configuration is stored at `BepInEx/config/cms2018.sprint.standalone.cfg`.

## Version downloads

| Version | Status | Changes |
| --- | --- | --- |
| [v1.3.0](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.3.0) | Preview | Sprint-scaled footstep playback; gameplay testing pending |
| [v1.2.0](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.2.0) | Archived preview | Typed speed input; gameplay testing pending |
| [v1.1.0](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.1.0) | Historical | Native settings submenu; menu user-confirmed working |
| [v1.0.2](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.0.2) | Historical | Escape handling, menu cleanup, configuration save fixes |
| [v1.0.1](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.0.1) | Historical | Corrected runtime references and menu error handling |
| [v1.0.0](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/releases/tag/v1.0.0) | Known broken — archive only | Initial build; menu TypeLoadException |

Each tag preserves the files available for that version. Exact source exists for **1.0.0, 1.0.2, and 1.3.0**; intermediate source snapshots were not retained. No reconstructed source is presented as original. See [the changelog](CHANGELOG.md).

## Build from source

On Windows, with the game and BepInEx installed:

```powershell
cd Source
.\Build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Car Mechanic Simulator 2018'
```

The script uses assemblies from your own installation, including the game's CLR 2 libraries, and outputs `SPRINT MOD CMS 2018.dll` alongside the source. Copy it into `BepInEx/plugins` to install. The build does not install automatically.

## Verification and reporting issues

Release assets retain the original ZIP bytes. Each release includes a SHA-256 checksum file; on Windows, compare it with:

```powershell
Get-FileHash '.\SPRINT_MOD_CMS_2018_v1.3.0_PREVIEW.zip' -Algorithm SHA256
```

The supplied documentation reports compilation, CLR reference, audio patch layout, and DLL checksum checks. Live testing remains outstanding for numeric input and audio; an external Harmony runtime dry-run did not complete. Test menu navigation, rebinding, hold/toggle, crouching, footsteps at 100/150/200/250%, scene changes, and compatibility with other mods before treating the preview as stable.

[Report an issue](https://github.com/importantbatata/sprint-mod-for-CMS-2018-BepInEx-mod/issues) with your game version, mod version, reproduction steps, and relevant `BepInEx/LogOutput.log` errors. Remove personal details before sharing logs.

## Uninstall

Close the game and remove `BepInEx/plugins/SPRINT MOD CMS 2018.dll`. Configuration can be kept for a future reinstall. The mod does not write save-game data.

## License

No license was supplied with these files. This repository does not imply a grant of redistribution or modification rights for the mod or any third-party dependencies.