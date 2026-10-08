# Changelog

## Unreleased

- Restored the Breeding Machine's vanilla workbench side borders, base, feet and silhouette, keeping biology details inside the wooden frame.
- Restored the Research Machine's original Slime Incubator dome contour, crisp glass palette and highlights. The sprout still appears only while processing; timing remains two in-game hours.
- Added an on-demand, read-only `cropbreeding_research_check` console diagnostic for machine input and callback failures. Automate's reported input rejection is still under investigation; this is not a confirmed compatibility fix.

## 1.0.11

- Reduced Research Machine processing to a fixed 2 in-game hours (120 minutes), with no configuration option.
- Updated machine and Researcher descriptions. Already-running batches retain their saved remaining time; new batches use the two-hour timer.

## 1.0.10

- Research Machine processing now takes a fixed 4 in-game hours (240 minutes), instead of finishing the next morning. No configuration option is added.
- Updated machine and Researcher descriptions. Already-running batches keep their stored remaining time; new batches use the four-hour timer.

## 1.0.9

- Research Machine now shows an empty chamber while idle and a sprout while processing, with vanilla processing wobble.
- When output is ready, the machine stops wobbling and returns to the empty-chamber frame with the normal output bubble.
- Preserved the existing working sprite, native 16×32 frame size, machine body and shadow. Frame switching uses vanilla machine settings with no extra scans or rendering patches.

## 1.0.8

- Researcher is now a single-level trait with no mutation-chance bonus or growth penalty. Existing Researcher 1 crops and seeds retain their saved traits.
- Added a Research Machine: one Researcher seed becomes one seed with two successful trait increases the next morning. Researcher is removed first, and each roll respects crop eligibility, trait slots and level caps.
- Research results are stored at insertion. Native machine processing supports Automate; breaking the machine destroys its contents.
- Added a recolored Slime Incubator-style sprout sprite at the original 16×32 size, and updated Researcher's Lookup Anything description.

## 1.0.7

- Added controller X quick-insert for the selected inventory stack in Breed, Set Companion and Remove Trait modes, including expanded inventory pages.
- Valid crops and seeds go to the appropriate slot; matching stacks combine up to capacity. Invalid combinations and ready output are not overwritten.
- Full left-slot stacks can be staged. Processing still consumes one recipe's required amount and returns unused left-slot ingredients when the result replaces them.
- Suppressed vanilla's X split-stack action inside the station and updated controller hints.

## 1.0.6

- Fixed opening the Breeding Machine while holding a crop or other item consuming one item from the active stack, including when using controller A.
- World item input now always rejects deposits; the normal interaction opens the menu, where ingredients can be inserted explicitly.

## 1.0.5

- Balanced Companion output: its 20% chance per level is multiplied by the main crop's base regrowth days / 10, or base growth days / 7 for single-harvest crops, capped at full chance.
- Regrowing crops use their natural regrowth interval for every harvest, including the first. Fertilizer, traits and Companion's delay do not affect this chance.
- Kept the existing growth and regrowth penalty: half the companion crop's base growth time.
- Updated Lookup Anything descriptions and the README to explain the new chance.

## 1.0.4

- Replaced the gold sparkle with the vanilla Qi Gem in trait-bearing item icons and Crop Harvest Bubbles mutation icons.
- Increased the badge from 18×18 to 32×32 at normal UI size, using exact 2× native pixels.
- Removed the unused custom sparkle assets.

## 1.0.3

- Replaced the incubator-style machine sprite with a static wooden biology workbench.
- Added a small gold sparkle to trait-bearing crops and seeds in inventory/item menus. Quality stars and stack counts keep their usual positions.

- Shortened Lookup Anything trait descriptions and removed the outdated settings-timing row.

- Removed the standalone mutation star. Added an optional Crop Harvest Bubbles integration: the same gold sparkle marks prepared mutations in the bubble's crop icon, following its visibility, size and opacity.

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
