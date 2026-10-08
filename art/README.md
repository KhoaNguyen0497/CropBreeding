# Crop Breeding artwork

Machine assets are native RGBA PNGs. The breeding station preserves the supplied vanilla workbench's outer wooden border, base, feet and silhouette. The research machine retains the Slime Incubator’s dome silhouette and ground-contact alpha, with a blue-gray glass rim and warm hardwood housing. Both use normal big-craftable rendering. Enlarged previews use nearest-neighbor scaling.

| Asset | Native size | Preview |
|---|---|---|
| `../src/assets/breeding-machine.png` | 16×32 | `breeding-machine-preview.png`, 10× |
| `../src/assets/research-machine.png` | 32×32 sheet; two 16×32 frames | `research-machine-preview.png`, 10× |

Biology details were created with the built-in image-generation tool, exported to native grids using nearest-neighbor sampling and a limited palette, then confined to the interior of the user's supplied vanilla workbench. The vanilla border/base pixels are retained directly to avoid clipping the sides or changing the edge shading.

Machine prompt: “A Stardew Valley crop-breeding biology station based on the vanilla wooden workbench. Keep its narrow amber wooden base, drawer and feet. Add a pale blue flask with green liquid, a tiny potted seedling and cream seed specimen tray. Frontal slightly top-down pixel art, limited palette, dark outline, transparent background, no animation, glow or ground shadow.”


The trait badge reuses the vanilla Qi Gem item sprite, `(O)858`, via ItemRegistry. Its native 16×16 sprite is drawn at exactly 2× for a 32×32 badge on a normal 64-pixel inventory/crop icon. No resampled or generated gem artwork is shipped. Inventory badges mean inherited item traits; Crop Harvest Bubbles badges mean a saved successful mutation waiting to be harvested.

## Research Machine

The sheet contains two aligned 16×32 frames: soil-only idle/ready at index 0 and a sprout rooted in the same soil at index 1. Vanilla machine settings select the working frame and wobble during processing. Both frames share their glass, soil and hardwood base pixels outside the sprout region. The original alpha mask preserves the machine footprint and ground-contact transparency.

The latest edit replaces the inherited purple rim with muted blue-gray, removes the long bright reflection streaks, and replaces the moss/olive housing with clean brown hardwood. Small pale-blue highlights keep the dome readable. Soil is visible in both states, with green confined to the active sprout. Exported using nearest-neighbor sampling to the native grid and a limited palette; preview is exactly 10× nearest-neighbor.

Latest edit prompt (built-in image-generation tool): “Edit the two-frame research machine sprite sheet, preserving its 16×32 frame grid, silhouette and transparency. Replace purple dome rims with muted blue-gray, remove long white/mint glare streaks and keep small subtle highlights. Recolor the mossy olive base to clean warm brown hardwood with tan highlights. Add dark brown soil inside both domes. Only the active right frame has a tiny green sprout rooted in that soil. Keep both machines otherwise identical; no smoothing, extra detail or text.”

Workbench correction prompt, built-in image-generation tool: “Turn only the central gray tool/display area into a biology workbench with a pale blue flask containing green liquid, a tiny potted sprout and a cream seed sample tray. Preserve the exact vanilla wooden frame, tabletop border, side strips, drawer, legs and shadow. All contents fit inside the frame. Same native 16×32 grid, warm palette and transparency.” The previously approved, larger biology details were retained inside the restored vanilla border after comparing the generated variant.
