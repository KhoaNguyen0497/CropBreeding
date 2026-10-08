# Crop Breeding artwork

Machine assets are static RGBA PNGs. The breeding station has binary transparency and no baked ground shadow. The research machine retains the Slime Incubator’s silhouette and ground-contact footprint. Both use normal big-craftable rendering. Enlarged previews use nearest-neighbor scaling.

| Asset | Native size | Preview |
|---|---|---|
| `../src/assets/breeding-machine.png` | 16×32 | `breeding-machine-preview.png`, 10× |
| `../src/assets/research-machine.png` | 32×32 sheet; two 16×32 frames | `research-machine-preview.png`, 10× |

Created with the built-in image-generation tool from the user-approved machine design, then exported to native grids using nearest-neighbor sampling and a limited palette (up to 24 colors). The user-supplied vanilla workbench was a style reference, not a shipped asset.

Machine prompt: “A Stardew Valley crop-breeding biology station based on the vanilla wooden workbench. Keep its narrow amber wooden base, drawer and feet. Add a pale blue flask with green liquid, a tiny potted seedling and cream seed specimen tray. Frontal slightly top-down pixel art, limited palette, dark outline, transparent background, no animation, glow or ground shadow.”


The trait badge reuses the vanilla Qi Gem item sprite, `(O)858`, via ItemRegistry. Its native 16×16 sprite is drawn at exactly 2× for a 32×32 badge on a normal 64-pixel inventory/crop icon. No resampled or generated gem artwork is shipped. Inventory badges mean inherited item traits; Crop Harvest Bubbles badges mean a saved successful mutation waiting to be harvested.

## Research Machine

Generated with the built-in image-generation tool using the [vanilla Slime Incubator](https://stardewvalleywiki.com/File:Slime_Incubator.png) as the reference. Exported to native 16×32 pixels with nearest-neighbor sampling and a limited palette. The original silhouette/alpha mask preserves the machine footprint. The sheet places the empty idle/ready frame at index 0 and the original sprout working frame at index 1. Vanilla machine settings select the working frame and wobble during processing; no custom rendering, animation loop or shadow is added.

Prompt: “Edit the supplied vanilla Slime Incubator. Preserve the silhouette, native 16×32 pixel grid, canvas proportions, perspective, dark outline, glass dome, lower mechanical housing and tiny baked ground-contact shadow. Replace the slime with a tiny two-leaf green sprout and brown soil patch in pale-blue glass. Recolor the purple housing to muted bronze/copper with moss-green accents. Native Stardew pixel art, no subpixel detail, smoothing, extra parts, effects, labels or animation; transparent background.”

Idle-frame edit prompt, built-in image-generation tool: “Create the idle version of this exact machine. Change only the glass interior: remove the sprout, stem and soil, replacing them with empty pale blue glass matching the surrounding shading and reflections. Keep the bronze/copper frame, moss-green sides, dark base, silhouette, shadow, proportions and native 16×32 pixel grid identical. Transparent background; no extra detail or smoothing.” The generated glass interior was exported on the existing pixel grid and composited into the original frame. All pixels outside the chamber and the entire working frame remain unchanged.
