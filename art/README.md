# Crop Breeding artwork

Machine assets are native RGBA PNGs. The breeding station preserves the supplied vanilla workbench's outer wooden border, base, feet and silhouette. The research machine retains the Slime Incubator’s dome outline, glass palette, highlights, silhouette and ground-contact alpha. Both use normal big-craftable rendering. Enlarged previews use nearest-neighbor scaling.

| Asset | Native size | Preview |
|---|---|---|
| `../src/assets/breeding-machine.png` | 16×32 | `breeding-machine-preview.png`, 10× |
| `../src/assets/research-machine.png` | 32×32 sheet; two 16×32 frames | `research-machine-preview.png`, 10× |

Biology details were created with the built-in image-generation tool, exported to native grids using nearest-neighbor sampling and a limited palette, then confined to the interior of the user's supplied vanilla workbench. The vanilla border/base pixels are retained directly to avoid clipping the sides or changing the edge shading.

Machine prompt: “A Stardew Valley crop-breeding biology station based on the vanilla wooden workbench. Keep its narrow amber wooden base, drawer and feet. Add a pale blue flask with green liquid, a tiny potted seedling and cream seed specimen tray. Frontal slightly top-down pixel art, limited palette, dark outline, transparent background, no animation, glow or ground shadow.”


The trait badge reuses the vanilla Qi Gem item sprite, `(O)858`, via ItemRegistry. Its native 16×16 sprite is drawn at exactly 2× for a 32×32 badge on a normal 64-pixel inventory/crop icon. No resampled or generated gem artwork is shipped. Inventory badges mean inherited item traits; Crop Harvest Bubbles badges mean a saved successful mutation waiting to be harvested.

## Research Machine

Generated with the built-in image-generation tool using the [vanilla Slime Incubator](https://stardewvalleywiki.com/File:Slime_Incubator.png) as the reference. Exported to native 16×32 pixels with nearest-neighbor sampling and a limited palette. The original silhouette/alpha mask preserves the machine footprint. The sheet places the empty idle/ready frame at index 0 and the original sprout working frame at index 1. Vanilla machine settings select the working frame and wobble during processing; no custom rendering, animation loop or shadow is added.

The latest correction keeps the vanilla dome's non-liquid pixels directly, including its dark stepped rim and white/mint highlight clusters. Generated empty glass replaces the green liquid using the vanilla glass palette; generated bronze/moss housing occupies the lower body with the original alpha footprint. The existing sprout detail is composited inside the active chamber. Both frames keep the same dome/body alignment.

Prompt: “Edit the supplied vanilla Slime Incubator. Preserve the silhouette, native 16×32 pixel grid, canvas proportions, perspective, dark outline, glass dome, lower mechanical housing and tiny baked ground-contact shadow. Replace the slime with a tiny two-leaf green sprout and brown soil patch in pale-blue glass. Recolor the purple housing to muted bronze/copper with moss-green accents. Native Stardew pixel art, no subpixel detail, smoothing, extra parts, effects, labels or animation; transparent background.”

Latest idle-frame edit prompt, built-in image-generation tool: “Make an empty-glass version of the vanilla Slime Incubator. Remove the green slime, preserving the exact dome contour, stepped dark rim and white/mint reflection pixels. Recolor only the lower purple housing to muted bronze and moss green while preserving its shade pattern. Same native 16×32 grid, canvas and transparency; no smoothing or added detail.” Exact vanilla glass pixels are restored during sprite assembly; the active sprout is a separate interior detail.

Workbench correction prompt, built-in image-generation tool: “Turn only the central gray tool/display area into a biology workbench with a pale blue flask containing green liquid, a tiny potted sprout and a cream seed sample tray. Preserve the exact vanilla wooden frame, tabletop border, side strips, drawer, legs and shadow. All contents fit inside the frame. Same native 16×32 grid, warm palette and transparency.” The previously approved, larger biology details were retained inside the restored vanilla border after comparing the generated variant.
