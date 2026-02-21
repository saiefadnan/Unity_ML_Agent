from tensorboard.backend.event_processing import event_accumulator
import pandas as pd
import os

# ---- CHANGE THIS ----
run_id = "drone4"   # your run-id folder name
results_dir = "results"
# ----------------------

path = f"{results_dir}/{run_id}/MyAgent"  # tfevents files are in MyAgent subdirectory

# Use the entire directory path for EventAccumulator
ea = event_accumulator.EventAccumulator(path)
ea.Reload()

print("Available scalar tags:", ea.Tags()['scalars'])

data = {}
all_steps = set()

# First pass: collect all unique steps
for tag in ea.Tags()['scalars']:
    events = ea.Scalars(tag)
    steps = [e.step for e in events]
    all_steps.update(steps)

# Sort all steps
all_steps = sorted(list(all_steps))
data['step'] = all_steps

# Second pass: align all metrics to the same step indices
for tag in ea.Tags()['scalars']:
    events = ea.Scalars(tag)
    step_to_value = {e.step: e.value for e in events}
    
    # Fill in values for all steps, using None for missing steps
    aligned_values = []
    for step in all_steps:
        if step in step_to_value:
            aligned_values.append(step_to_value[step])
        else:
            aligned_values.append(None)
    
    data[tag] = aligned_values

# Debug print
print("Available columns:", list(data.keys()))
print(f"Data length: {len(all_steps)} steps")

df = pd.DataFrame(data)
# Forward fill missing values
df = df.ffill()
df = df.sort_values("step")
df.to_csv(f"{run_id}_training_data.csv", index=False)

print(f"Saved CSV to: {run_id}_training_data.csv")
