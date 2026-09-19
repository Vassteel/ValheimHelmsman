"""Export the original fishing dock with the shared authored-mesh format."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
source=(root/'tools/WorkshopModels/export.py').read_text().replace("ROOT=R/'design/workshop-supplies-v1'", "ROOT=R/'design/pelican-station-v1'")
exec(compile(source,str(root/'tools/WorkshopModels/export.py'),'exec'),{'__file__':__file__})
