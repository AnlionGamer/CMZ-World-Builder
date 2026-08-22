# Changelog

All notable public changes to CastleMiner Z World Builder are documented here.

## 1.1.2

- Added optional scenario-defined `worldGeneration.base` behavior.
  - `scenario`: existing custom staging behavior and the backward-compatible default.
  - `official-cmz`: stages the validated stock-equivalent Normal World foundation before scenario changes are applied.
- Kept the generation-strategy decision inside each `.cmzscenario` package rather than imposing one base generator on all scenarios.
- Preserved the v1.1.1 Normal World path.
- Expanded World Inspection with persisted-record average/max size, farthest X/Z record radius, and the serialized initial persisted chunk-ID list payload used by CMZ multiplayer joining.
- Multiplayer values are measurements only; no unsupported safe/unsafe threshold is asserted.

## 1.1.1

- Corrected Normal World naming to the stock CMZ 1.9.9.8 `New World <local date/time>` pattern.
- Made Normal World owner/creator derive from the selected Steam profile.
- Preserved `world.info` v5 / terrain version 1 defaults, start location, server defaults, boss state, and finite-resource state.
- Normal World creation no longer fabricates terrain `.dat` or player `.inv` files; CastleMiner Z remains authoritative for native terrain and mode/difficulty-specific inventory when the session starts.
- Blank seed generation now uses a fresh `System.Random().Next()` per creation, matching CMZ's automatic normal-world seed range.

## 1.1.0

- Added read-only World Inspection.
- Added existing-world discovery by Steam profile.
- Added world metadata, persisted record statistics/bounds, `.inv` counts, RTSD envelope/integrity checks, selected-profile payload-read checks, and X/Z persisted-data visualization.
- Added privacy-safe Copy Report and Export Report.
- Inspection intentionally does not claim undocumented block-level semantics for every persisted `.dat` record.

## 1.0.4

- Standardized the standalone World Builder around the official CMZ utility UI design family.
- Replaced Windows-native button and ComboBox chrome with explicit themed templates.
- Fixed light-on-light dropdown states under dark/material themes.
- Added readable accent text and themed hover/pressed/focused/disabled states.
- Preserved the standalone `.cmztool` architecture and separate ToolData storage.

## 1.0.3

- Introduced explicit themed ComboBox rendering for Steam Profile and custom-scenario choice dropdowns.
- Removed the primary light-on-light dropdown rendering defect under dark themes.

## 1.0.2

- Extracted World Builder from CMZ Mod Manager into its own standalone process.
- Introduced distribution as the independent `cmz.worldbuilder` `.cmztool` package.
- Added a dedicated single-instance guard and non-destructive migration from earlier World Builder data locations.

## Earlier development lineage

Historical v0.2.1 reference documentation remains in release packages for implementation/validation context. The current 1.x line is the independently packaged `.cmztool` architecture.
