# Better Junimos source compatibility review

Reviewed 2026-10-07 against `hawkfalcon/Stardew-Mods` commit `faa40d440fd75c9f702142a12efef616ab06fc9a`. This checks the published source, not the user's installed DLL or an in-game session. No Better Junimos-specific patch or dependency is added. Multiplayer remains out of scope. This does not audit the separate Better Junimos Forestry Redux fork.

## Harvesting and raisins

`HarvestCropsAbility` leaves actual crop harvesting to vanilla. `PatchTryToHarvestHere` starts the harvest timer; `PatchJunimoShake` adjusts timing/animation after vanilla update. Neither replaces the actual `Crop.harvest` call. Our readiness fallback, stored mutation, primary output decoration, bonus delivery, regrowth timing, Nurse Crop and Rooted therefore share the same harvest path.

Nurse Crop runs before Rooted resets the plant. Rooted changes the successful annual harvest's removal result to false after restarting it, so vanilla Junimo update leaves the plant in place. Both effects remain annual-only. The separately deferred island-walnut interaction has not been changed.

Vanilla Junimo update clears `lastItemHarvested`, calls `Crop.harvest`, then—if raisins are active—rolls 20% to add `lastItemHarvested.getOne()` and explicitly preserves quality. This copies **one item**, not the full potato/coffee/High Yield batch. The copy does not call `Crop.harvest` again, so it does not roll mutation, restart Rooted, grow trees or award other breeding bonuses again.

CropBreeding now retains the last actual primary item emitted by its harvest clone hook, after mutation and High Quality are applied. Bonus delivery restores that item as the raisin target in `finally`, even if an individual bonus delivery fails. Vanilla sunflower seeds/wheat hay and our Companion/material/Seed Saver/High Yield extras cannot replace the target. Same-ID Companion output is also excluded: identity comes from the original harvest output path, not a search through bonus items. Copying the outgoing item retains custom metadata and colored-item identity. Better Junimos' Botanist prefix can still set the outgoing item's quality before storage; the retained reference sees that change.

The field accessor is resolved once at startup. Harvesting adds a saved reference and constant-time field access, with no new update handler, crop scan, extra RNG call, or compatibility dispatch. If accessor initialization fails, it logs through the normal SMAPI/chat error handler and skips our Junimo bonus delivery to avoid replacing the vanilla raisin target; other harvest effects are still independent. Item-delivery errors are isolated and remaining bonus items are attempted once.

## Remaining compatibility issues

| Feature | Source finding | Practical effect / current workaround |
| --- | --- | --- |
| Automatic planting | `PlantCropsAbility.PerformAction` passes only `foundItem.ItemId`; its `Plant` creates `new Crop(...)` and assigns `hd.crop` directly. It bypasses `HoeDirt.plant` and never transfers seed metadata. | Bred seeds, including Seed Saver seeds placed into hut storage, are consumed into plants without inherited traits/Companion. Hand-plant bred seeds. Plain plants can still gain harvest mutations through our normal growth/harvest hooks. |
| Evergreen season selection | Plant eligibility caches `new Crop(seedId, ...).IsInSeason(location)` by seed ID, without the seed's traits. | With out-of-season avoidance enabled, Evergreen 5 seeds can be rejected outside normal seasons. Turning avoidance off does not fix the trait loss above. |
| Fertilizing planted crops | `FertilizeAbility.CheckSpeedGro` duplicates the vanilla speed formula and may call `Crop.ResetPhaseDays` directly, instead of patched `HoeDirt.applySpeedIncreases`. Its planting code also duplicates speed calculation. | On a trait plant, speed fertilizer or Agriculturist/paddy conditions can replace initial phase lengths without reapplying Fast Growth, Companion or Researcher adjustments, leaving our saved phase deltas stale. Fertilize empty ground before hand-planting; avoid having Junimos fertilize an existing trait plant. |

Watering changes soil water state; dead-crop cleanup calls `destroyCrop`. Neither invents another live-plant harvest or trait transfer path. Better Junimos flower/giant-crop avoidance settings can intentionally prevent harvesting; that is a selection setting, not missing harvest hooks.

Supporting automatic planting needs access to the actual selected seed item as well as its ID. Merely replacing `new Crop` with an ID-only `HoeDirt.plant` call would not supply our current planting hook with the Junimo's seed metadata. A future integration or upstream change should address both seed context and vanilla speed application. No such change is included in this review.

## Source links

- [HarvestCropsAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/HarvestCropsAbility.cs)
- [JunimoHarvesterPatches](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Patches/JunimoHarvesterPatches.cs)
- [PlantCropsAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/PlantCropsAbility.cs)
- [FertilizeAbility](https://github.com/hawkfalcon/Stardew-Mods/blob/faa40d440fd75c9f702142a12efef616ab06fc9a/BetterJunimos/Abilities/Base/FertilizeAbility.cs)

Automated checks and the remaining live checklist are in TESTING.md.
