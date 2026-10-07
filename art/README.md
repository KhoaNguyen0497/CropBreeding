# Crop Breeding artwork

Both assets are static RGBA PNGs with binary transparency and no baked ground shadow. The game retains normal big-craftable rendering. Enlarged previews use nearest-neighbor scaling.

| Asset | Native size | Preview |
|---|---|---|
| `../src/assets/breeding-machine.png` | 16×32 | `breeding-machine-preview.png`, 10× |
| `../src/assets/trait-sparkle.png` | 9×9 | `trait-sparkle-preview.png`, 16× |

Created with the built-in image-generation tool from the user-approved designs, then exported to native grids using nearest-neighbor sampling and limited palettes (up to 24 colors for the machine, 5 including transparency for the badge). The user-supplied vanilla workbench was a style reference, not a shipped asset.

Machine prompt: “A Stardew Valley crop-breeding biology station based on the vanilla wooden workbench. Keep its narrow amber wooden base, drawer and feet. Add a pale blue flask with green liquid, a tiny potted seedling and cream seed specimen tray. Frontal slightly top-down pixel art, limited palette, dark outline, transparent background, no animation, glow or ground shadow.”

Badge prompt: “Single tiny four-point gold sparkle, 9×9 logical pixels, dark brown outline, gold body and pale center. Hard stepped pixels, transparent background, no enclosing button, additional sparkles, glow or animation.”

The badge is drawn at 18×18 pixels on a normal 64-pixel inventory/crop icon. Inventory badges mean inherited item traits; Crop Harvest Bubbles badges mean a saved successful mutation waiting to be harvested.
