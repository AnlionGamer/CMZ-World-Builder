# Changelog

All notable public changes to CastleMiner Z World Builder are documented here.

## 1.0.4

- Standardized the standalone World Builder around the official CMZ utility UI design family.
- Replaced Windows-native button chrome with explicit themed button templates.
- Replaced the platform-default ComboBox rendering with explicit themed templates.
- Fixed light-on-light states affecting Steam Profile and other dropdown controls under dark/material themes.
- Added readable accent-text calculation and themed hover, pressed, focused, and disabled states.
- Aligned page titles, cards, inputs, and control proportions with CMZ Mod Manager conventions while keeping World Builder fully independent.
- Preserved the standalone `.cmztool` architecture and separate ToolData storage.

## 1.0.3

- Introduced explicit themed ComboBox rendering for Steam Profile and custom-scenario choice dropdowns.
- Removed the primary light-on-light dropdown rendering defect under dark themes.

## 1.0.2

- Extracted World Builder from CMZ Mod Manager into its own standalone process.
- Introduced distribution as the independent `cmz.worldbuilder` `.cmztool` package.
- Added a dedicated single-instance guard.
- Added non-destructive migration from earlier integrated/standalone World Builder data locations.
- Kept tool binaries separate from persistent ToolData so updates do not replace user scenarios/settings.

## Earlier development lineage

World Builder originated as a standalone utility and later existed temporarily as an integrated CMZ Mod Manager tool. Historical v0.2.1 reference documentation remains in the release package for implementation and validation context. The current 1.x line is the independently packaged `.cmztool` architecture.
