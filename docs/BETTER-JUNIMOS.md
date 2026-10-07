# Better Junimos source compatibility review

Reviewed 2026-10-07 against `hawkfalcon/Stardew-Mods` commit `faa40d440fd75c9f702142a12efef616ab06fc9a`. This checks the published source, not the user's installed DLL or an in-game session. An optional Better Junimos planting patch now transfers seed traits; there is no required dependency or special harvesting patch. Multiplayer remains out of scope. This does not audit the separate Better Junimos Forestry Redux fork.

## Harvesting and raisins

`HarvestCropsAbility` leaves actual crop harvesting to vanilla. `PatchTryToHarvestHere` starts the harvest timer; `PatchJunimoShake` adjusts timing/animation after vanilla update. Neither replaces the actual `Crop.harvest` call. Our readiness fallback, stored mutation, primary output decoration, bonus delivery, regrowth timing, Nurse Crop and Rooted therefore share the same harvest path.

Nurse Crop runs before Rooted resets the plant. Rooted changes the successful annual harvest's removal result to false after restarting it, so vanilla Junimo update leaves the plant in place. Both effects remain annual-only. The separately deferred island-walnut interaction has not been changed.

Vanilla Junimo update clears `lastItemHarvested`, calls `Crop.harvest`, then—if raisins are active—rolls 20% to add `lastItemHarvested.getOne()` and explicitly preserves quality. This copies **one item**, not the full potato/coffee/High Yield batch. The copy does not call `Crop.harvest` again, so it does not roll mutation, restart Rooted, grow trees or award other breeding bonuses again.

CropBreeding now retains the last actual primary item emitted by its harvest clone hook, after mutation and High Quality are applied. Bonus delivery restores that item as the raisin target in `finally`, even if an individual bonus delivery fails. Vanilla sunflower seeds/wheat hay and our Companion/material/Seed Saver/High Yield extras cannot replace the target. Same-ID Companion output is also excluded: identity comes from the original harvest output path, not a search through bonus items. Copying the outgoing item retains custom metadata and colored-item identity. Better Junimos' Botanist prefix can still set the outgoing item's quality before storage; the retained reference sees that change.

The field accessor is resolved once at startup. Harvesting adds a saved reference and constant-time field access, with no new update handler, crop scan, extra RNG call, or compatibility dispatch. If accessor initialization fails, it logs through the normal SMAPI/chat error handler and skips our Junimo bonus delivery to avoid replacing the vanilla raisin target; other harvest effects are still independent. Item-delivery errors are isolated and remaining bonus items are attempted once.

## Planting inheritance fix

The integration registers at GameLaunched only when Better Junimos is installed. It scopes an actual seed reference to one `PerformAction` call, captures that action's `PlantableSeed` result, and transfers it after successful `Plant`. It checks the ability instance, location, tile, seed ID and eligible ground crop; it never guesses from another same-ID stack or the player inventory. Availability probes outside that action do not capture seeds. A finalizer restores the previous context even on nested calls or Better Junimos exceptions.

The same `SeedInheritance.Apply` helper now serves vanilla planting and this integration. Traits and Companion selection transfer before initial growth is recalculated through the normal `HoeDirt.applySpeedIncreases` hooks. An instant-ready crop discards any premature mutation result and prepares from its inherited traits. Failure restores the pre-transfer crop snapshot and logs without replaying planting or consuming another seed. Better Junimos retains all control over planting, seed consumption, paddy watering and visuals.

The four method hooks are signature-checked before installation and rolled back individually if registration fails. No crop/chest scan, daily maintenance or repeating event is added. Plain and excluded seeds retain Better Junimos behavior. This fixes inherited data loss; the existing out-of-season seed-selection policy and later fertilizing code are unchanged.

## Remaining compatibility issues

| Feature | Source finding | Practical effect / current workaround |
| --- | --- | --- |
| Evergreen season selection | Plant eligibility caches `new Crop(seedId, ...).IsInSeason(location)` by seed ID, without the seed's traits. | With out-of-season avoidance enabled, Evergreen 5 seeds can be rejected outside normal seasons. Trait inheritance is now fixed, but out-of-season selection still requires a separate change; hand-plant Evergreen seeds when this filter rejects them. |
| Fertilizing planted crops | `FertilizeAbility.CheckSpeedGro` duplicates the vanilla speed formula and may call `Crop.ResetPhaseDays` directly, instead of patched `HoeDirt.applySpeedIncreases`. Its planting code also duplicates speed calculation. | Better Junimos allows unfertilized soil with no crop or a crop at internal phase 0 or 1 (`currentPhase > 1` is rejected). Even phase 0 already has its trait-adjusted durations. On a trait plant, speed fertilizer or Agriculturist/paddy conditions can replace initial phase lengths without reapplying Fast Growth, Companion or Researcher adjustments, leaving our saved phase deltas stale. Fertilize empty ground before hand-planting; avoid having Junimos fertilize an existing trait plant. |

Watering changes soil water state; dead-crop cleanup calls `destroyCrop`. Neither invents another live-plant harvest or trait transfer path. Better Junimos flower/giant-crop avoidance settings can intentionally prevent harvesting; that is a selection setting, not missing harvest hooks.

The inheritance fix deliberately leaves Better Junimos seed-selection filters and its later fertilizer ability unchanged. In particular, phase-0-only fertilizing would still need to preserve trait timings: the durations are assigned at planting, not when the crop reaches its next phase. This concerns stored growth durations, not erasing the trait metadata itself.

## Source links

- [HarvestCropsAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/HarvestCropsAbility.cs)
- [JunimoHarvesterPatches](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Patches/JunimoHarvesterPatches.cs)
- [PlantCropsAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/PlantCropsAbility.cs)
- [FertilizeAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/FertilizeAbility.cs)

Automated checks and the remaining live checklist are in TESTING.md.
