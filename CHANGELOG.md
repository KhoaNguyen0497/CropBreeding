# Changelog

## Unreleased

- Set Companion now rejects the seed's own harvest crop, using the actual seed-to-crop mapping.

- Redesigned the breeding menu with Stardew Profit-style panels, direct mode tabs, item summaries and controller hints.
- Expanded backpacks fit up to four rows; larger inventories use page buttons, mouse wheel or controller LB/RB without moving or copying items.
- Fixed input labels to display the configured breeding cost instead of a hardcoded five.

- Increased the default breeding cost to 3 matching seeds and 3 trait crops per resulting seed. The accepted breeding cost range is now 1–10, including GMCM.

## 1.0.1

- Breeding now costs 1 matching seed and 1 trait crop by default. New `BreedingCost` setting applies the same quantity to both inputs, including merging.
- Removed trait percentage settings; effects retain their previous default rates. Base mutation chance remains configurable.
- Enabled error messages in local chat by default.
- Updated the station UI, Lookup Anything descriptions, GMCM and documentation to match. Stored donor batches must match the current cost, preventing accidental extra consumption after a setting change.

Build, trait-rule and safety checks pass, including configurable breeding costs. Live game/controller testing remains pending.

## 1.0.0

Initial release.

- Breed matching seeds and trait crops, assign Companion crops, and remove unwanted seed traits through a controller-friendly station menu.
- Includes 15 traits with five levels, saved mutation outcomes and support for regrowing crops.
- Supports SVE crop data, shared vanilla harvesting, and the updated Auto Harvester.
- Optional integrations for Better Junimos planting/fertilizing, Lookup Anything and Generic Mod Config Menu.
- Includes isolated error logging with optional chat messages.

Single-player only. Automated build/rule/safety checks pass; live game and controller validation is still pending. Known compatibility boundaries are documented in the README and detailed notes.
