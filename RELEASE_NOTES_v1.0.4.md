# CastleMiner Z World Builder v1.0.4

CastleMiner Z World Builder is an independent world-creation utility for the original Steam release of CastleMiner Z 1.9.9.8. It is distributed as a `.cmztool` package and runs as its own application rather than being compiled into CMZ Mod Manager.

## Highlights

- Create normal CastleMiner Z worlds using native-compatible world metadata.
- Generate custom worlds from trusted protocol-1 `.cmzscenario` packages.
- Install and update World Builder independently through the CMZ Mod Manager Tools page.
- Preserve World Builder settings, installed scenarios, and other ToolData across normal updates/reinstalls.
- 23 interface languages.
- Dark, Light, System, Bedrock, Copper Wall, Iron Wall, Gold Wall, Diamond Wall, Bloodstone, and Space Rock / Space Goo themes.

## v1.0.4 UI improvements

- Standardized the standalone World Builder around the official CMZ utility UI design family.
- Replaced Windows-native button chrome with explicit themed control templates.
- Fixed light-on-light states affecting Steam Profile and other dropdown controls.
- Added consistent themed hover, pressed, focused, and disabled states.
- Added readable accent-text handling.
- Aligned page headings, cards, inputs, and control proportions with CMZ Mod Manager conventions while preserving complete application independence.

## Requirements

- CastleMiner Z 1.9.9.8 (original Steam release)
- Windows
- CMZ Mod Manager 1.1.2 or newer for `.cmztool` installation

World Builder is not a runtime gameplay mod and does not use Harmony or the CMZ Mod Framework while generating worlds.

## Installation

1. Download `CMZ_World_Builder_v1.0.4.cmztool`.
2. Open CMZ Mod Manager.
3. Open **Tools**.
4. Select **Install Tool...**.
5. Choose the downloaded `.cmztool`.
6. Open CastleMiner Z World Builder from the Tools page.

## Release integrity

GitHub automatically publishes the SHA-256 digest of the uploaded release asset. The digest displayed by GitHub for the release asset is the authoritative public checksum for that file.

The release artifact was audited for package structure, declared payload integrity, path safety, duplicate entries, compiled version identity, theme JSON validity, and localization parity before publication.

## Testing note

Package/build validation does not replace real CastleMiner Z gameplay testing. Generated-world compatibility should ultimately be judged by successful world creation, loading, saving, and gameplay in CastleMiner Z 1.9.9.8.
