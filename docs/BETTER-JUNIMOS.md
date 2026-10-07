# Better Junimos source compatibility review

Reviewed 2026-10-07 against `hawkfalcon/Stardew-Mods` commit `faa40d440fd75c9f702142a12efef616ab06fc9a`. This checks the published source, not the user's installed DLL or an in-game session. Optional Better Junimos planting and fertilizer patches now preserve seed traits and growth timing; there is no required dependency or special harvesting patch. Multiplayer remains out of scope. This does not audit the separate Better Junimos Forestry Redux fork.

## Harvesting and raisins

`HarvestCropsAbility` leaves actual crop harvesting to vanilla. `PatchTryToHarvestHere` starts the harvest timer; `PatchJunimoShake` adjusts timing/animation after vanilla update. Neither replaces the actual `Crop.harvest` call. Our readiness fallback, stored mutation, primary output decoration, bonus delivery, regrowth timing, Nurse Crop and Rooted therefore share the same harvest path.

Nurse Crop runs before Rooted resets the plant. Rooted changes the successful annual harvest's removal result to false after restarting it, so vanilla Junimo update leaves the plant in place. Both effects remain annual-only. The island-walnut interaction is now fixed for vanilla hand/scythe soil actions. Direct Junimo harvesting still uses its existing vanilla walnut behavior; this does not add a new automated walnut roll.

Vanilla Junimo update clears `lastItemHarvested`, calls `Crop.harvest`, then—if raisins are active—rolls 20% to add `lastItemHarvested.getOne()` and explicitly preserves quality. This copies **one item**, not the full potato/coffee/High Yield batch. The copy does not call `Crop.harvest` again, so it does not roll mutation, restart Rooted, grow trees or award other breeding bonuses again.

CropBreeding now retains the last actual primary item emitted by its harvest clone hook, after mutation and High Quality are applied. Bonus delivery restores that item as the raisin target in `finally`, even if an individual bonus delivery fails. Vanilla sunflower seeds/wheat hay and our Companion/material/Seed Saver/High Yield extras cannot replace the target. Same-ID Companion output is also excluded: identity comes from the original harvest output path, not a search through bonus items. Copying the outgoing item retains custom metadata and colored-item identity. Better Junimos' Botanist prefix can still set the outgoing item's quality before storage; the retained reference sees that change.

The field accessor is resolved once at startup. Harvesting adds a saved reference and constant-time field access, with no new update handler, crop scan, extra RNG call, or compatibility dispatch. If accessor initialization fails, it logs through the normal SMAPI/chat error handler and skips our Junimo bonus delivery to avoid replacing the vanilla raisin target; other harvest effects are still independent. Item-delivery errors are isolated and remaining bonus items are attempted once.

## Planting inheritance fix

The integration registers at GameLaunched only when Better Junimos is installed. It scopes an actual seed reference to one `PerformAction` call, captures that action's `PlantableSeed` result, and transfers it after successful `Plant`. It checks the ability instance, location, tile, seed ID and eligible ground crop; it never guesses from another same-ID stack or the player inventory. Availability probes outside that action do not capture seeds. A finalizer restores the previous context even on nested calls or Better Junimos exceptions.

The same `SeedInheritance.Apply` helper now serves vanilla planting and this integration. Traits and Companion selection transfer before initial growth is recalculated through the normal `HoeDirt.applySpeedIncreases` hooks. An instant-ready crop discards any premature mutation result and prepares from its inherited traits. Failure restores the pre-transfer crop snapshot and logs without replaying planting or consuming another seed. Better Junimos retains all control over planting, seed consumption, paddy watering and visuals.

The four method hooks are signature-checked before installation and rolled back individually if registration fails. No crop/chest scan, daily maintenance or repeating event is added. Plain and excluded seeds retain Better Junimos behavior. This fixes inherited data loss; fertilizer compatibility is handled separately below. Out-of-season seed-selection policy remains unchanged.

## Fertilizer timing fix

Better Junimos normally allows fertilizing unfertilized soil with no crop or crops at internal phase 0 or 1. Its copied speed formula resets phase durations without calling our growth hooks. Phase 0 already has trait-adjusted durations, so merely blocking phase 1 would not preserve them.

The optional fertilizer integration now:

- Restricts available fertilizer jobs to empty soil or internal phase 0, retaining all of Better Junimos' other checks. This restriction applies to plain crops too.
- Rechecks the phase in `PerformAction`, before Better Junimos applies or consumes fertilizer, so a crop advancing after job selection cannot slip through.
- Replaces `CheckSpeedGro` with `HoeDirt.applySpeedIncreases(Game1.player)`. The existing growth hooks remove previous trait deltas, allow normal fertilizer/profession/paddy calculation, then apply Companion followed by Fast Growth once. Existing traits and the mutation outcome are not rerolled. Empty soil requires no growth calculation.

Better Junimos still chooses, applies and consumes the fertilizer and controls the visuals. This adds only tile checks to its existing ability calls and recalculates growth when fertilizing actually happens; there is no new update handler or crop scan. Hand fertilizing is unchanged. If growth recalculation throws, the crop snapshot is restored, the error is logged through the normal SMAPI/chat handler, and Better Junimos' original calculation is allowed to run. The action/consumption is not replayed.

The three fertilizer hooks are signature-checked as one group. Registration failure removes only that group's attempted additions; planting and harvesting integration remain installed. Live mod/game validation remains pending.

## Accepted limitation

Better Junimos still caches out-of-season eligibility by seed ID without reading Evergreen. With out-of-season avoidance enabled it may reject Evergreen 5 seeds. The user explicitly accepted ignoring this limitation for now; hand-plant those seeds when necessary. Evergreen traits still transfer whenever Better Junimos successfully plants the seed.

Watering changes soil water state; dead-crop cleanup calls `destroyCrop`. Neither invents another live-plant harvest or trait transfer path. Better Junimos flower/giant-crop avoidance settings can intentionally prevent harvesting; that is a selection setting, not missing harvest hooks.

## Source links

- [HarvestCropsAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/HarvestCropsAbility.cs)
- [JunimoHarvesterPatches](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Patches/JunimoHarvesterPatches.cs)
- [PlantCropsAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/PlantCropsAbility.cs)
- [FertilizeAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/FertilizeAbility.cs)

Automated checks and the remaining live checklist are in TESTING.md.

## Additional accepted upstream behavior

Better Junimos' copied planting speed formula can reduce an already-zero phase below zero under strong bonuses. This can waste a growth reduction (e.g. Taro with Hyper Speed-Gro, Agriculturist and paddy water), while a later Rooted restart uses vanilla's correct calculation. The user accepted leaving this upstream bug unchanged. This is separate from the fertilizer recalculation integration.

Its seed-availability cache can miss seeds newly deposited by Seed Saver or ordinary harvests until a hut-menu close or day-start refresh. This too is accepted unchanged. Its selected seed determines a replacement crop's traits; it does not preserve the harvested plant's traits unless those are present on the selected seed. Winter/rain work settings also apply when workers are serving a greenhouse from an outdoor hut.
