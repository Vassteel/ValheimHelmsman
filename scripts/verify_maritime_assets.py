#!/usr/bin/env python3
"""Verify the retired imported bundle cannot enter the runtime build."""
import runpy
from pathlib import Path
runpy.run_path(str(Path(__file__).resolve().parents[1]/'tests/PaddleCraft/assets.py'),run_name='__main__')
