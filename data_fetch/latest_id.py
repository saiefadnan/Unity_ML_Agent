import os
from pathlib import Path
from datetime import datetime

results_path = Path("results")

print("=" * 60)
print("ALL TRAINING RUNS")
print("=" * 60)

runs = []
for run_dir in results_path.iterdir():
    if run_dir.is_dir():
        mtime = run_dir.stat().st_mtime
        onnx_count = len(list(run_dir.glob("**/*.onnx")))
        runs.append({
            'name': run_dir.name,
            'modified': datetime.fromtimestamp(mtime),
            'onnx_models': onnx_count
        })

# Sort by modification time (newest first)
runs.sort(key=lambda x: x['modified'], reverse=True)

for i, run in enumerate(runs, 1):
    print(f"\n{i}. Run ID: {run['name']}")
    print(f"   Last Modified: {run['modified']}")
    print(f"   ONNX Models: {run['onnx_models']}")
    if i == 1:
        print("   ⭐ LATEST RUN ⭐")