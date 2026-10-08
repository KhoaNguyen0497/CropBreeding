# Crop Breeding artwork

Machine assets are native RGBA PNGs. The breeding station preserves the supplied vanilla workbench's outer wooden border, base, feet and silhouette. The research machine preserves its corrected blue-gray glass dome above a custom terracotta pot, with a fitted rim and connected tapered outline. Both use normal big-craftable rendering. Enlarged previews use nearest-neighbor scaling.

| Asset | Native size | Preview |
|---|---|---|
| `../src/assets/breeding-machine.png` | 16×32 | `breeding-machine-preview.png`, 10× |
| `../src/assets/research-machine.png` | 32×32 sheet; two 16×32 frames | `research-machine-preview.png`, 10× |

Biology details were created with the built-in image-generation tool, exported to native grids using nearest-neighbor sampling and a limited palette, then confined to the interior of the user's supplied vanilla workbench. The vanilla border/base pixels are retained directly to avoid clipping the sides or changing the edge shading.

Machine prompt: “A Stardew Valley crop-breeding biology station based on the vanilla wooden workbench. Keep its narrow amber wooden base, drawer and feet. Add a pale blue flask with green liquid, a tiny potted seedling and cream seed specimen tray. Frontal slightly top-down pixel art, limited palette, dark outline, transparent background, no animation, glow or ground shadow.”


The trait badge reuses the vanilla Qi Gem item sprite, `(O)858`, via ItemRegistry. Its native 16×16 sprite is drawn at exactly 2× for a 32×32 badge on a normal 64-pixel inventory/crop icon. No resampled or generated gem artwork is shipped. Inventory badges mean inherited item traits; Crop Harvest Bubbles badges mean a saved successful mutation waiting to be harvested.

## Research Machine

The sheet contains two aligned 16×32 frames: soil-only idle/ready at index 0 and a sprout rooted in the same soil at index 1. Vanilla machine settings select the working frame and wobble during processing. Timing and rendering code are unchanged.

The approved pot concept replaces the entire wooden base with clean terracotta, a fitted top lip in slightly top-down perspective and a tapered body. The final native-grid pass removes the side knob and evens the connected outline. No wooden stand, metallic parts or added cast-shadow ellipse are used. The same native pot pixels and silhouette are shared across both states.

The built-in image-generation tool supplied the pot artwork. Export used nearest-neighbor sampling, a limited palette and binary alpha at the native grid. The fitted pot preserves the corrected dome silhouette and neutral border without soil-color bleed. A later sprout/glass pass updates only the interior above it. The new pot occupies rows 19–31. The preview is exactly 10× nearest-neighbor scaling.

Pot edit prompt (built-in image-generation tool): “Use the existing two-frame sheet for exact layout and the approved larger terracotta pot concept for the lower body. Replace the wooden base with a clean terracotta pot directly supporting the dome, with a thick oval top lip in Stardew’s slightly top-down perspective, tapered body, small integrated foot and tiny side knob. No wooden stand or platform, metal, moss or extra shadow. Preserve the blue-gray glass border and keep soil inside it. Idle has soil only; active has the sprout. Same 16×32 native pixel grid per frame, limited flat palette, transparent background; frames identical outside the sprout.”

Workbench correction prompt, built-in image-generation tool: “Turn only the central gray tool/display area into a biology workbench with a pale blue flask containing green liquid, a tiny potted sprout and a cream seed sample tray. Preserve the exact vanilla wooden frame, tabletop border, side strips, drawer, legs and shadow. All contents fit inside the frame. Same native 16×32 grid, warm palette and transparency.” The previously approved, larger biology details were retained inside the restored vanilla border after comparing the generated variant.

Border cleanup (built-in image-generation edit): “Replace stray brown soil pixels in the vertical dome rim with adjacent blue-gray glass-border shading, preserving all other pixels.” Only the corrected border detail was exported and palette-matched into the native sheet: six pixels across both frames, with the original alpha unchanged. The implemented pot keeps these corrected dome pixels.

Sprout and reflection review (built-in image-generation edit): “Repair the chopped sprout into two complete green leaves joined to a short stem rooted in soil. Add a subtle curved pale-cyan highlight to both glass domes. Preserve the clean glass borders, soil and terracotta pot; idle stays soil-only.” Export uses nearest-neighbor native sampling and the existing palette. Both frames share identical glass, soil and pot pixels; only the active seedling differs. The pot and outer side borders are unchanged from the approved base revision.

### Native sprout rendering review

The follow-up pass replaces the flattened sprout with diagonally raised, tapered leaves and a visible stem. Only the generated plant is exported: existing idle, glass reflections, soil and pot are retained. The native seedling spans local x4–11 / y10–17, is one 4-connected shape, and leaves clearance from both glass borders. Its upper tip is one pixel wide; the stem continues through y15–17 into the soil. The export resets PNG page metadata to the full 32×32 sheet.

`research-machine-render-preview.png` shows both 16×32 frames at the ordinary 4× world scale. `research-machine-render-preview.gif` simulates the vanilla processing wobble using the inspected `Object.draw` destination rectangle: width `64 + 4w`, height `128 + 2w`, position offset `(-2w, -2w)`, with `w` ranging from 0 to 5. All phases draw the entire source frame. These are source-based render simulations, not screenshots from a running game. The mod uses sprite index 0, the next index while working, and no custom draw patch. Live in-game validation remains pending.

Follow-up built-in image edit prompt: “Replace only the cropped-looking rectangular sprout with a taller complete seedling: diagonally raised pointed leaves with single-pixel tapered tips and a continuous visible stem above the soil. Keep coherent native-grid shapes; preserve the existing dome, reflections, soil, pot and idle frame.” The generated sprout is palette-matched and aligned one native row upward to root at the soil surface.

### Native pot perimeter correction

Per the user’s request, this revision edits the original 32×32 sheet directly. No generated or enlarged artwork is downsampled. The lower 13 rows are replaced with a matching terracotta base in each 16×32 frame, with mirrored silhouette geometry and one connected dark perimeter around the rim, stepped sides and bottom. The right-side handle is removed completely. Rows 0–18, including glass, soil and the full sprout, remain byte-for-byte unchanged. All pot silhouette edges use the same outline color. Enlarged and 4×/wobble previews are generated only after the native asset is complete.
