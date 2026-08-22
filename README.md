# CastleMiner Z World Builder

An independent world-creation and inspection utility for the original Steam release of **CastleMiner Z 1.9.9.8**.

> **Unofficial community project:** CastleMiner Z World Builder is independently created and published by AnlionGamer. It is not an official CastleMiner Z release and is not affiliated with, sponsored by, approved by, or endorsed by the game's developers, publisher, or Valve.

**Publisher:** AnlionGamer  
**Current release:** v1.1.2  
**Tool ID:** `cmz.worldbuilder`  
**Package format:** `.cmztool` format 1

## What it does

CastleMiner Z World Builder runs as its own utility rather than being compiled into CMZ Mod Manager.

- Creates normal CastleMiner Z worlds using audited stock-compatible metadata while leaving terrain and player inventory creation to CastleMiner Z itself.
- Imports and runs trusted protocol-1 `.cmzscenario` packages.
- Lets each scenario define whether it uses legacy scenario-owned staging or the validated `official-cmz` Normal World foundation.
- Inspects existing worlds without rewriting them.
- Validates protected-save envelopes and selected-profile readability.
- Reports persisted-world data scale, record sizes, coordinate coverage, and the initial persisted chunk-ID payload relevant to multiplayer joining.
- Keeps multiplayer metrics factual; v1.1.2 does not invent a safe/unsafe threshold.
- Preserves World Builder ToolData across updates.
- Supports 23 interface languages and the CMZ Mod Manager community-tool visual design family.

No custom scenarios are bundled with the World Builder release.

## Requirements

- CastleMiner Z **1.9.9.8** (original Steam release)
- Windows
- CMZ Mod Manager **1.1.2 or newer** for `.cmztool` installation

World Builder is an external utility. It is **not** a runtime gameplay mod and does not use Harmony or CMZ Runtime while generating worlds.

## Installation

1. Download `CMZ_World_Builder_v1.1.2.cmztool` from GitHub Releases.
2. Open **CMZ Mod Manager**.
3. Open **Tools**.
4. Select **Install Tool...**.
5. Choose the downloaded `.cmztool`.
6. Open **CastleMiner Z World Builder** from the Tools page.

Before creating or generating a world, fully close `CastleMinerZ.exe`.

## Normal World creation

The Normal World path mirrors the audited CastleMiner Z 1.9.9.8 world-creation metadata lifecycle as closely as an offline tool can while keeping CMZ authoritative for native terrain and mode/difficulty-specific inventory creation.

Normal worlds use:

- CMZ-compatible `world.info` v5 / terrain version 1;
- the selected Steam profile for owner/creator;
- stock-style `New World <local date/time>` naming;
- the stock start position and initial metadata defaults;
- no pre-generated terrain `.dat` files;
- no fabricated player `.inv` file.

Same-seed runtime comparison has verified matching surface terrain, trees, cave topology, and ore placement in tested areas. This is runtime evidence for the tested regions, not a claim of exhaustive block-for-block comparison of an effectively infinite world.

## Custom scenarios

World Builder supports trusted protocol-1 `.cmzscenario` packages. Scenario packages remain separate products and are not included in this repository or release.

### Scenario-defined base generation

v1.1.2 adds an optional scenario manifest declaration:

```json
"worldGeneration": {
  "base": "official-cmz"
}
```

A scenario may choose:

- `scenario` — the existing scenario-owned staging behavior; this remains the default when the declaration is absent.
- `official-cmz` — stage the validated stock-compatible CastleMiner Z Normal World foundation first, then let the scenario apply its own changes.

`official-cmz` is an internal World Builder protocol identifier. It describes the stock-compatible CMZ generation foundation and **does not mean that World Builder, a scenario, or its output is an official CastleMiner Z product or endorsed by the game's rights holders**.

The decision belongs to each scenario package. World Builder remains neutral and contains no scenario-specific world design logic.

Only install scenario packages from sources you trust. Scenario generators are executable content by design.

## World Inspection

World Inspection is read-only with respect to the inspected world and provides:

- `world.info` metadata summary;
- persisted `.dat` and `.inv` counts;
- coordinate bounds and X/Z coverage;
- record size statistics and write-time range;
- RTSD protected-save envelope/integrity validation;
- selected-profile payload-read validation;
- privacy-safe copy/export reports;
- initial multiplayer persisted chunk-ID payload measurement: `4 + (persisted record count × 4)` bytes, excluding transport framing.

The exported privacy-safe report omits local paths, Steam profile IDs, player/inventory identifiers, owner/creator names, server message, and server password.

## User data

World Builder data is stored separately from installed program files:

```text
%LOCALAPPDATA%\CastleMinerZ\CMZModManager\ToolData\cmz.worldbuilder
```

When CMZ Mod Manager uses a custom data root, the corresponding `ToolData\cmz.worldbuilder` directory is used instead. Updating/reinstalling preserves ToolData.

## Source

This repository contains the public World Builder source snapshot and supporting documentation for the current release. The private/release **builder is intentionally not included** in this repository.

Reference manifests from published packages are stored under `manifests/`. Historical reference manifests are preserved as records of the packages that were actually released and should not be interpreted as the licensing policy for future builds.

## Release integrity

The SHA-256 for the current release is recorded in `SHA256SUMS.txt`.

```text
4529E7C4EE14EE473143E5A8963D648854B4DBF19484F06BFA442F2171365829  CMZ_World_Builder_v1.1.2.cmztool
```

## Release validation

The v1.1.2 artifact has been checked for:

- package ZIP integrity;
- manifest/file SHA-256 agreement and no undeclared payload files;
- path traversal hygiene;
- expected tool/game/manager version identity;
- 23-language key and format-placeholder parity;
- valid theme/language JSON;
- compiled v1.1.2 identity and publisher strings;
- no bundled custom scenarios;
- no hard-coded local user paths found in the compiled executable;
- source-level absence of network/web client code;
- scenario package path/hash validation and transactional staging behavior;
- privacy-safe World Inspection report behavior.

Windows build and real-game runtime testing remain the final authority. v1.1.2 has been built successfully on Windows and exercised with CastleMiner Z 1.9.9.8.

## License

The current repository `main` branch and future World Builder work are distributed under the **AnlionGamer Community Use, Modification, and Noncommercial Distribution License v1.0**. See [`LICENSE`](LICENSE).

The license permits community use, study, modification, free forks, and free redistribution while prohibiting sale, paid access, deceptive rebranding, and deliberately malicious use. Independently created scenarios and extensions remain the property of their own authors unless they incorporate substantial World Builder material.

**Historical license:** World Builder v1.1.2 and earlier copies already published under the MIT License retain the MIT permissions that accompanied those releases. Relicensing the repository going forward does not revoke those historical permissions. See [`LICENSE_HISTORY.md`](LICENSE_HISTORY.md).

Future `.cmztool` release packages should carry the applicable license and project notice inside the package so the terms remain attached when the tool is shared separately from GitHub.

CastleMiner Z names, trademarks, and third-party game material remain the property of their respective owners.

## Disclaimer

This is an independent community project and is not an official release from the original CastleMiner Z developers, publisher, or Valve.
