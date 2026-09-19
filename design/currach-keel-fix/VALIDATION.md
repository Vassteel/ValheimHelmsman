# Currach keel seam repair

The original skin stopped 12 mm short of the centreline on each side, leaving a 24 mm slot. A fitted tarred-skin bridge closes it over the full keel profile, with outward-facing top, bottom and end surfaces. It adds 516 triangles to the existing hull material batch (no extra renderer).

The packed Currach is now 29,108 triangles. Original vertices/indices remain identical prefixes; only the skin strip was appended. All material bytes, gameplay metadata, water mask, collision definitions, seats, boarding points and animated parts are unchanged.

The regression ray check fails on the old asset and passes at 189 locations from both inside and below. Preview images use the exact packed geometry. Final game appearance still needs a local check.

Source: tools/FinalFleet/keel_skin.py, called by the Currach finish step. The saved final Blender model and GLB include the patch. The optimization baseline and staged binary are also patched, so subsequent reductions retain the protected hull seam. Old files remain in .build/currach-keel-before/ for rollback.
