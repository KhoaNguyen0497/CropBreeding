# Crop Breeding

Grow crops with inheritable traits, then breed their produce into matching seeds. For **Stardew Valley 1.6 + SMAPI 4**, including Stardew Valley Expanded. **Single-player only.**

## Install

Download the installable ZIP from [Releases](https://github.com/KhoaNguyen0497/CropBreeding/releases/latest), extract it into your game's `Mods` folder, and launch through SMAPI.

Optional: **Generic Mod Config Menu** for settings, **Lookup Anything** for trait descriptions and growth information, and [**Crop Harvest Bubbles**](https://www.nexusmods.com/stardewvalley/mods/17761) for mutation badges.

## Start breeding

1. Grow crops on tilled ground. When a crop becomes ready, it has a **5% mutation chance** by default. With Crop Harvest Bubbles installed, a Qi Gem badge on its crop icon marks a prepared mutation.
2. Harvest it. All primary produce from that harvest receives the same traits and mutation.
3. Craft a **Breeding Machine** at Farming level 5: **50 Wood, 5 Iron Bars and 1 Battery Pack**.
4. Open its two-slot menu and combine **3 trait crops + 3 matching seeds → 1 bred seed** by default. Each group must come from one matching stack.
5. Plant the bred seed to use its traits. New harvest mutations affect the produce, not the standing plant.

Seeds are matched to crops using actual game data, including SVE crops. Default limits are **3 traits per seed**, each up to **level 5**, except single-level **Researcher**.

Trait-bearing crops and seeds also show the Qi Gem badge in inventory and item menus, with no bubble mod required. The wooden biology station has direct mode tabs and supports expanded backpacks with controller-friendly paging.

## Machine modes

Empty both slots before switching modes.

| Mode | Inputs | Result |
|---|---|---|
| Breed | 3 trait crops + 3 matching seeds by default | One seed with the donor crop's traits. |
| Set Companion | 1 seed with Companion + 1 eligible crop | Assigns a different crop as companion; consumes the crop. Cannot choose the seed's own crop. |
| Remove Trait | 1 seed with traits | Removes one selected trait for free. |

**Breeding cost:** configurable from 1 to 10. A value of `x` requires `x` seeds and `x` donor crops, producing one seed. Set Companion and Remove Trait keep their one-seed costs.

**Merging:** plain seeds copy the donor. A seed with exactly **one level-1 trait** can merge it into the donor's traits: matching traits gain one level; a different trait is added. Merges exceeding either limit are rejected.

Coffee beans can inherit traits and be replanted directly. They cannot be breeding donors, but support Set Companion and Remove Trait.

The station uses a controller-friendly menu with tabs for each mode. Select an inventory stack and press **X** to insert it into the appropriate input slot; compatible stacks combine up to the slot limit. Press the action button to process one recipe. Unused left-slot ingredients return to your inventory when replaced by the result. Expanded backpacks display up to four rows, with page buttons, mouse wheel and LB/RB for larger inventories. It has no automatic processing or Automate input support. **Breaking it destroys stored contents.**

## Research Machine

Insert **1 seed with Researcher** to receive **1 researched seed the next morning**. Other traits are allowed. Researcher is removed, then two successful rolls add or upgrade traits. Each roll rechecks available slots, crop eligibility and maximum levels; Researcher cannot be rolled back onto the result.

For example, `Researcher + X4 + Y5` can become `X5 + Y5 + Z1`. A plain Researcher seed can gain two different traits or one level-2 trait. If changed settings leave fewer than two legal increases, insertion is rejected without consuming the seed.

The result is fixed when processing starts. This machine has no menu and uses vanilla processing, including **Automate** support. Its chamber is empty and still when idle; a sprout appears and the machine wobbles while processing. Ready output uses the empty, still machine with the normal output bubble. Craft it at Farming level 5 with **50 Wood, 5 Iron Bars and 1 Battery Pack**. **Breaking it destroys its contents.**

Existing Researcher 1 crops and seeds keep their saved traits. Researcher no longer changes mutation chance or growth time; breed Researcher produce into matching seeds to use it in the machine.

## Traits

Trait percentages are fixed, per level unless stated otherwise.

| Trait | Effect |
|---|---|
| Fast Growth | 5% shorter growth and natural regrowth, applied after other timing adjustments. |
| High Yield | 20% more primary produce; whole extras are guaranteed, with a roll for the fractional remainder. |
| High Quality | 5% chance per primary item to upgrade its normal harvest quality by one tier. |
| Companion | Up to 20% chance for one chosen, plain companion crop. Chance scales with the main crop's base regrowth days / 10, or base growth days / 7 for single-harvest crops, capped at full chance. Adds half the companion's base growth time to growth and regrowth. |
| Evergreen | Level 5 allows all-season planting and survival, including winter. Levels 1–4 are dormant. |
| Researcher | Single level. Research Machine replaces it with two successful trait rolls by the next morning. |
| Seed Saver | 10% chance to return one matching seed with the parent's original traits. |
| Rooted | 10% chance for a single-harvest crop to restart from seed stage with its original traits. |
| Nurse Crop | 30% chance to advance adjacent non-fruit trees by one stage; excess chance adds stages. Stops one stage before maturity. Single-harvest crops only. |
| Copper / Iron / Gold Bearing | Three separate traits producing their respective bars, using the material rule below. |
| Maple / Resin / Tar Bearing | Three separate traits producing Maple Syrup, Oak Resin or Pine Tar, using the same rule. |

**Material rule:** each complete five days of the crop's base initial growth gives a 5% chance per level for one item. Chances above 100% guarantee items plus a roll for the remainder. Regrowing crops use their initial growth time for every harvest.

Mutation rolls choose from all eligible traits. Picking a maxed trait, or a new trait when all slots are full, gives no mutation. Prepared results stay fixed until harvest, including failed rolls. Regrowing and Rooted crops prepare a new result for each new harvest cycle.

## Compatibility and limits

- Supports ordinary tilled ground, including greenhouse and Ginger Island soil. Trait seeds cannot be planted in Garden Pots.
- Mixed seed packets, seasonal forage seeds, vanilla Fiber Seeds and Qi Beans are excluded. Once mixed seeds become an eligible known crop, that crop can mutate. **SVE Ancient Fiber is eligible.**
- Uses vanilla harvest hooks for hand/scythe harvesting, Junimos and the updated **Auto Harvester**. Harvesters bypassing vanilla `Crop.harvest` are not covered.
- **Better Junimos** transfers the actual selected seed's traits and preserves trait timing when fertilizing. Its existing seasonal selection, winter work settings and inventory-cache limitations still apply. [Details](docs/BETTER-JUNIMOS.md).
- Full Junimo-hut overflow can lose traits. Giant crops yield ordinary, untraited produce.
- Processing and crafting outputs, including Seed Maker seeds, do not inherit traits, except the Research Machine’s researched seeds. Trait produce can be consumed normally by recipes and other machines.
- Settings support Generic Mod Config Menu: mutation chance, maximum traits, regrowing mutations, breeding cost, and chat errors. Chat errors are enabled by default.

Build and automated checks are covered; live gameplay, controller and end-to-end mod integration testing remain outstanding. See [testing notes](docs/TESTING.md).

## More information

- [Detailed mechanics and commands](docs/MECHANICS.md)
- [Build from source](docs/BUILDING.md)
- [Release notes](CHANGELOG.md)

To uninstall, back up your save, run `cropbreeding_cleanup` in the SMAPI console while the mod is installed, save and quit, then remove the mod. Cleanup removes traits and both machines, including their contents.
