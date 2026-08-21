# CastleMiner Z World Builder

An independent world-creation utility for the original Steam release of **CastleMiner Z 1.9.9.8**.

**Publisher:** AnlionGamer  
**Current release:** v1.0.4  
**Tool ID:** `cmz.worldbuilder`  
**Package format:** `.cmztool` format 1

## What it does

CastleMiner Z World Builder provides a dedicated interface for creating and managing CastleMiner Z worlds without embedding the tool into the Mod Manager itself.

- Create standard CastleMiner Z worlds using native-compatible world metadata.
- Generate worlds from trusted protocol-1 `.cmzscenario` packages.
- Import direct `.cmzscenario` packages and supported archive wrappers.
- Preserve normal CastleMiner Z world/save compatibility.
- Detect Steam profiles and resolve the player persona name without requiring an existing world.
- Keep World Builder settings, scenarios, and user data separate from the installed tool files.
- Run as its own `CMZ.WorldBuilder.exe` process rather than inside `CMZModManager.exe`.
- Provide 23 interface languages and the same official-tool visual design family used by CMZ Mod Manager.

No custom scenarios are bundled with the World Builder release.

## Requirements

- CastleMiner Z **1.9.9.8** (original Steam release)
- Windows
- CMZ Mod Manager **1.1.2 or newer** for `.cmztool` installation

World Builder is an external utility. It is **not** a runtime gameplay mod and does not use Harmony or the CMZ Mod Framework while generating worlds.

## Installation

1. Download `CMZ_World_Builder_v1.0.4.cmztool` from the GitHub Releases page.
2. Open **CMZ Mod Manager**.
3. Open **Tools**.
4. Select **Install Tool...**.
5. Choose the downloaded `.cmztool`.
6. Open **CastleMiner Z World Builder** from the Tools page.

The `.cmztool` package contains the standalone World Builder executable and all resources required by the tool.

## Updating

Install the newer `.cmztool` through the Mod Manager. The Manager recognizes the same tool ID as an update and replaces the installed program files while preserving World Builder ToolData.

World Builder can be updated independently of CMZ Mod Manager. A World Builder update does not require `CMZModManager.exe` to be rebuilt.

## User data

World Builder user data is stored separately from the installed program:

```text
%LOCALAPPDATA%\CastleMinerZ\CMZModManager\ToolData\cmz.worldbuilder
```

When the Mod Manager uses a custom data root, the corresponding `ToolData\cmz.worldbuilder` directory is used instead.

Updating or reinstalling the tool preserves ToolData. Normal uninstall also preserves ToolData unless the user explicitly chooses to remove it.

## Creating worlds

Before creating or generating a world, fully close `CastleMinerZ.exe`.

### Standard worlds

The standard-world path creates native-compatible world metadata and leaves normal terrain generation to CastleMiner Z's own terrain initialization.

### Custom scenarios

World Builder supports trusted protocol-1 `.cmzscenario` packages. Scenario packages are separate from the World Builder itself and are not included in this repository or release.

Scenario packages can define custom generation logic and options, so only install scenarios from sources you trust.

## Themes and interface

v1.0.4 standardizes the standalone tool around the official CMZ utility UI contract:

- explicit themed Button and ComboBox templates;
- readable accent text;
- consistent input, card, and page sizing;
- dark/light/material themes without Windows-native light-control leakage;
- 23 supported interface languages.

## Source

The repository contains the v1.0.4 World Builder source and release resources used by the standalone tool. The private/release builder is intentionally not required for installing or using the published `.cmztool`.

The `manifests/v1.0.4/tool.json` file is a reference copy of the manifest contained in the published v1.0.4 package.

## Release integrity

The v1.0.4 release SHA-256 is recorded in `SHA256SUMS.txt`.

```text
BE8A64D80EB64C32E94E65E512EC701F5CFE1AB8217F91F04F8F0434B488353E  CMZ_World_Builder_v1.0.4.cmztool
```

## Compatibility and testing

The package structure, payload hashes, language resources, themes, and compiled version identity have been independently checked against the v1.0.4 release artifact.

Automated/source validation is not a substitute for real CastleMiner Z runtime testing. Generated-world compatibility should ultimately be judged by successful creation, loading, saving, and gameplay in CastleMiner Z 1.9.9.8.

## License

Original project source is released under the MIT License. CastleMiner Z names, trademarks, and any third-party game material remain the property of their respective owners.

## Disclaimer

This is a community project and is not an official release from the original CastleMiner Z developers or Valve.
