"""The imported boatyard overlay is retired; verify its removal instead."""
import runpy
from pathlib import Path
runpy.run_path(str(Path(__file__).resolve().parents[1]/'PaddleCraft/assets.py'),run_name='__main__')
