# CastleMiner Z World Builder v1.1.2

World Builder v1.1.2 keeps the validated Normal World creation path and expands the general-purpose Custom Scenario framework and World Inspection diagnostics.

## Highlights

### Scenario-defined world foundations

Custom Scenarios now decide which base-world strategy they require.

A protocol-1 `.cmzscenario` may request the validated **official CMZ Normal World foundation** with:

```json
"worldGeneration": {
  "base": "official-cmz"
}
```

Scenarios that omit the declaration keep the existing scenario-owned staging behavior for backward compatibility.

This is a generic World Builder capability. World Builder does not contain scenario-specific fortress, city, biome, or other content logic.

### Normal World parity preserved

The v1.1.1 Normal World path is preserved:

- stock-style `New World <local date/time>` naming;
- owner/creator from the selected Steam profile;
- CMZ `world.info` v5 / terrain version 1 defaults;
- stock initial position and metadata defaults;
- automatic seed behavior matching CMZ's normal-world range;
- no fabricated terrain `.dat` files;
- no fabricated player `.inv` file.

CastleMiner Z 1.9.9.8 remains authoritative for native terrain generation and mode/difficulty-specific player inventory when the world session begins.

### Expanded World Inspection

World Inspection now reports additional objective persisted-world measurements:

- persisted chunk-record count;
- persisted `.dat` data size;
- minimum / median / average / maximum record size;
- farthest persisted X/Z record radius;
- initial persisted chunk-ID list payload used when CMZ advertises persisted chunk IDs to a joining client.

The initial ID-list payload is reported as an objective measurement only. v1.1.2 does **not** label any world size as multiplayer-safe or unsafe because no universal runtime safety threshold has been established.

## Compatibility

- **Game:** CastleMiner Z 1.9.9.8 (original Steam release)
- **CMZ Mod Manager:** v1.1.2 or newer
- **Package:** `CMZ_World_Builder_v1.1.2.cmztool`
- **Tool ID:** `cmz.worldbuilder`

No Custom Scenarios are bundled with World Builder.

## Validation status

The release artifact passed package integrity, manifest integrity, path hygiene, localization parity, JSON resource, compiled version-identity, privacy/reporting, and source-level scenario-package safety checks. The Windows builder completed successfully, and the resulting v1.1.2 tool has been exercised in CastleMiner Z 1.9.9.8.

Same-seed Normal World runtime comparisons have matched surface terrain, trees, cave topology, and ore placement in tested areas. This is evidence for the tested regions rather than a claim of exhaustive block-for-block comparison of the entire world.
