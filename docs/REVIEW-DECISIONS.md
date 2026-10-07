# Review decisions — 2026-10-07

Numbers refer to the 50-item code/gameplay review in this project's chat. Unmentioned items must be revisited later, not silently fixed. The mod has never been installed; no migration work or release is requested. Multiplayer is not currently supported.

| Review items | Decision / status |
| --- | --- |
| 1 | Resolved without a code change: let vanilla resolve Mixed Seeds into a known crop; that crop may participate normally when eligible. Mixed seed packets remain excluded breeding inputs. |
| 2 | Fix approved and implemented: preserve the actual primary crop as Junimo's raisin copy target, including traits/quality/color. Exclude vanilla byproducts and breeding bonuses; keep vanilla's roll and one-item count. Source review and targeted tests pass; live gameplay remains untested. |
| 3 | Fix approved and implemented: successful Rooted restarts preserve the skipped vanilla island hand/scythe 5% walnut roll via the shared IslandFarming limit of five. No additional roll for other harvests or direct automated callers. Build and targeted tests pass; live validation remains pending. |
| 4 | Fix harvest-preparation error isolation. Implemented; injected-failure tests pass. |
| 5 | Fix repeated SMAPI errors. New failures log in full; identical repeats are summarized. |
| 6 | Fix broad patch rollback. Only the failed installation's additions are removed. |
| 8, 44 | Multiplayer is out of scope, including synchronized config and harvest ownership rules. |
| 10 | Cache menu eligibility and removal labels until inputs, mode, selection, trait cap, or crop catalogue change. Implemented. |
| 11 | Remove idle station locks without recurring maintenance. Deferred one-shot cleanup implemented and tested. |
| 13, 14 | Fit the menu to the viewport and wrap/fit text. Shared scaled hitboxes and drawing implemented. |
| 15 | Controller validation approved. Navigation/source and geometry checked; actual Steam Deck/controller input and rendering still require the game. Do not claim live validation. |
| 16 | Leave the mutation icon as it is for now. |
| 17, 18, 19 | Keep breeding cost, annual-line economics, and quality/color stack restrictions. |
| 20 | Revisit coffee's direct-replanting/merging asymmetry later. |
| 21–32 | Keep the reviewed sunflower, trait eligibility, rounding, penalties, odds, and balance rules. |
| 36–43 | Keep the reviewed progression/compatibility boundaries and accepted limitations. |
| 45, 46 | Keep giant-crop trait loss and normal consumption of valuable trait crops. |
| 47 | Explain when settings apply in GMCM, Lookup Anything and documentation. No scan/rewrite of existing crops or rerolling ready mutations. Implemented. |
| 48 | Explanation requested only. A future Data/Crops entry producing ordinary resources is eligible unless its seed ID is explicitly excluded. No new general filter added. SVE Ancient Fiber remains allowed. |
| 49 | Existing live/multiplayer test gap accepted for now. |
| 50 | Explanation requested only. Broad cleanup of stale decision labels and obsolete tests is deferred; descriptions of this change's approved fixes were updated. |
| 7, 9, 12, 33, 34, 35 | Unmentioned: revisit later. This includes destruction edges, stacking optimization, unready harvest work, and remaining randomness concerns. |

Better Junimos compatibility was checked separately on 2026-10-07. Its harvesting path is shared vanilla code. The user subsequently approved fixing planting trait loss: optional integration now transfers the exact selected seed's traits/Companion and applies initial trait timing. The user then approved fixing fertilizing and restricting it to empty soil/phase-0 crops: optional integration now routes speed recalculation through the normal game method with trait hooks and blocks later-phase actions before consumption. Better Junimos' out-of-season seed filter does not recognize Evergreen; the user explicitly accepted leaving this unchanged. Details are in BETTER-JUNIMOS.md.

The older README's “Decisions still open” and numbered test notes predate this review and must not override these explicit choices. Planned/in-game checks in TESTING.md are not claims that those behaviors were verified.
