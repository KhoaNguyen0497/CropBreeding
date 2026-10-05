# In-game validation checklist

Use a copied save and test together with the actual SVE, Automate and AutoHarvester versions installed on PC/Steam Deck.

1. Temporarily set mutation chance to 1. Grow a single-harvest crop and check one added/upgraded trait on every primary output. Compare hand/scythe/Junimo/AutoHarvester. Check full inventory/chest behavior does not lose or duplicate harvests.
2. Craft a Breeding Machine at Farming 5. Insert a trait crop and wrong seed (reject), then exact matching seed (one donor and ten identical seeds consumed; one output). Check plain-seed copying; X,Y donor + ten X1 seeds gives X2,Y; + ten Z1 gives X,Y,Z. Reject fewer than ten seeds, level-2 seeds, multi-trait seeds and merges over the count/level limits without consuming inputs. Check full inventory collection, saving with pending donor/output, controller use and destroying with contents.
3. With Automate, test empty and pending machines, wrong seeds, split stacks totaling 9 versus 10, different seed traits/levels that must not combine, full output storage, mixed-quality donors and repeated update cycles. Confirm no duplicate outputs or unrelated input consumption.
4. Replant in ordinary soil, greenhouse and Ginger Island. Test pots (trait seed refused) and ordinary seeds in pots (normal behavior, no mutation). Compare initial growth/retention with fertilizer, Agriculturalist and rice/taro water adjacency. Regrowth remains normal.
5. Check trait and level differences separate stacks and getOne preserves traits/quality/color. Process and craft trait crops: outputs have no traits and input quality behaves normally; premium must not inflate artisan base value.
6. Sunflower: flower may mutate; bonus seeds inherit original traits. Wheat hay has none. Coffee mutated beans can be planted directly. Mixed/forage/fiber/Qi crops never mutate when newly planted. Run catalog and inspect all installed SVE mappings, including Ancient Fiber.
7. Grow a giant crop using differently traited plants. Formation remains vanilla; axe drops have no traits.
8. Test cleanup on a backup with crops, placed machines, chest/inventory machines and pending contents; save/reload without the mod and check for missing-item remnants.
9. Test multiplayer clients planting, harvesting, collecting and reconnecting. Trait modData should sync; everyone must have the mod.

Known unresolved design decisions are recorded in README (12, 16, 18). Reflection integration deliberately fails closed and logs a warning if AutoHarvester changes its planner signature.

10. Load legacy bare-ID trait metadata: it displays as level 1. Harvest max-count plants with level-1 traits: upgrades remain possible without adding types. Harvest plants with all traits at level 5: no trait loss. Check level-scaled speed, premium price, yield and water retention at levels 1 and 5.
