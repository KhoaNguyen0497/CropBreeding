# Crop Breeding artwork

Machine assets are native RGBA PNGs. The breeding station preserves the supplied vanilla workbench's outer wooden border, base, feet and silhouette. The research machine preserves its corrected blue-gray glass dome above a custom terracotta pot, with a fitted rim and integrated foot. Both use normal big-craftable rendering. Enlarged previews use nearest-neighbor scaling.

| Asset | Native size | Preview |
|---|---|---|
| `../src/assets/breeding-machine.png` | 16×32 | `breeding-machine-preview.png`, 10× |
| `../src/assets/research-machine.png` | 32×32 sheet; two 16×32 frames | `research-machine-preview.png`, 10× |

Biology details were created with the built-in image-generation tool, exported to native grids using nearest-neighbor sampling and a limited palette, then confined to the interior of the user's supplied vanilla workbench. The vanilla border/base pixels are retained directly to avoid clipping the sides or changing the edge shading.

Machine prompt: “A Stardew Valley crop-breeding biology station based on the vanilla wooden workbench. Keep its narrow amber wooden base, drawer and feet. Add a pale blue flask with green liquid, a tiny potted seedling and cream seed specimen tray. Frontal slightly top-down pixel art, limited palette, dark outline, transparent background, no animation, glow or ground shadow.”


The trait badge reuses the vanilla Qi Gem item sprite, `(O)858`, via ItemRegistry. Its native 16×16 sprite is drawn at exactly 2× for a 32×32 badge on a normal 64-pixel inventory/crop icon. No resampled or generated gem artwork is shipped. Inventory badges mean inherited item traits; Crop Harvest Bubbles badges mean a saved successful mutation waiting to be harvested.

## Research Machine

The sheet contains two aligned 16×32 frames: soil-only idle/ready at index 0 and a sprout rooted in the same soil at index 1. Vanilla machine settings select the working frame and wobble during processing. Timing and rendering code are unchanged.

The approved pot concept replaces the entire wooden base with clean terracotta, a thick fitted top lip in slightly top-down perspective, tapered body, small integrated foot and a side adjustment knob. No wooden stand, metallic parts or added cast-shadow ellipse are used. The same native pot pixels and silhouette are shared across both states.

The built-in image-generation tool supplied the pot artwork. Export used nearest-neighbor sampling, a limited palette and binary alpha at the native grid. Rows 0–18 of the existing corrected sheet were retained exactly, preserving the dome, highlights, soil, sprout and neutral border without soil-color bleed. The new pot occupies rows 19–31. The preview is exactly 10× nearest-neighbor scaling.

Pot edit prompt (built-in image-generation tool): “Use the existing two-frame sheet for exact layout and the approved larger terracotta pot concept for the lower body. Replace the wooden base with a clean terracotta pot directly supporting the dome, with a thick oval top lip in Stardew’s slightly top-down perspective, tapered body, small integrated foot and tiny side knob. No wooden stand or platform, metal, moss or extra shadow. Preserve the blue-gray glass border and keep soil inside it. Idle has soil only; active has the sprout. Same 16×32 native pixel grid per frame, limited flat palette, transparent background; frames identical outside the sprout.”

Workbench correction prompt, built-in image-generation tool: “Turn only the central gray tool/display area into a biology workbench with a pale blue flask containing green liquid, a tiny potted sprout and a cream seed sample tray. Preserve the exact vanilla wooden frame, tabletop border, side strips, drawer, legs and shadow. All contents fit inside the frame. Same native 16×32 grid, warm palette and transparency.” The previously approved, larger biology details were retained inside the restored vanilla border after comparing the generated variant.

Border cleanup (built-in image-generation edit): “Replace stray brown soil pixels in the vertical dome rim with adjacent blue-gray glass-border shading, preserving all other pixels.” Only the corrected border detail was exported and palette-matched into the native sheet: six pixels across both frames, with the original alpha unchanged. The implemented pot keeps these corrected dome pixels.
