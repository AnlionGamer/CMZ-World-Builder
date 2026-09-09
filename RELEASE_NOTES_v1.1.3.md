# CastleMiner Z World Builder v1.1.3

World Builder v1.1.3 is a focused World Inspection correctness release. It fixes metadata parsing for established worlds whose `world.info` contains populated saved-object collections.

## World Inspection fix

CastleMiner Z does not store the crate, door, and spawner fields as three standalone integers. Each value is a collection count followed by variable-length records. Earlier World Builder inspection code consumed the counts but not the records, which could misalign the remainder of `world.info` and eventually produce an invalid string-length error on populated worlds.

v1.1.3 now follows the CastleMiner Z 1.9.9.8 layout:

- crate collection for `world.info` v1+;
- door collection for v3+;
- spawner collection for v4+;
- existing v5 Hell-boss fields remain correctly aligned.

World Inspection also reports the saved crate, door, and spawner counts and exposes whether metadata parsing succeeded in World Health.

## Preserved behavior

- World Inspection remains read-only.
- Protected-save integrity and selected-profile readability checks are preserved.
- Persisted-record scale, coverage, and multiplayer chunk-ID payload measurements are preserved.
- Normal World creation behavior is unchanged.
- Scenario-defined `worldGeneration.base` behavior introduced in v1.1.2 is unchanged.
- No custom scenarios are bundled with World Builder.

## Compatibility

- CastleMiner Z: **1.9.9.8**
- CMZ Mod Manager: **1.1.2 or newer**
- Tool ID: `cmz.worldbuilder`

## Validation

The v1.1.3 builder completed successfully on Windows. The resulting tool was exercised against the previously failing populated custom-scenario world, and World Inspection parsed it successfully. Package integrity, manifest payload integrity, release identity, and legal-file inclusion were also audited before publication.

## License

World Builder v1.1.3 is governed by the **AnlionGamer Community Distribution Terms v1.0**. World Builder v1.1.2 and earlier copies already published under MIT retain the permissions that accompanied those historical releases.
