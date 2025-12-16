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

# Define test/evaluation metrics (exclude training losses and policy parameters)
test_metrics = [
    'Reward',
    'Environment/Cumulative Reward', 
    'Environment/Episode Length',
    'EpisodeLength',
    'TargetsFound',
    'PathEfficiency',
    'AngleStability',
    'GroundCollision'
]

# Filter available metrics to only include test metrics
available_test_metrics = [tag for tag in ea.Tags()['scalars'] if tag in test_metrics]
print("Available test metrics:", available_test_metrics)

data = {}
all_steps = set()

# First pass: collect all unique steps from test metrics only
for tag in available_test_metrics:
    events = ea.Scalars(tag)
    steps = [e.step for e in events]
    all_steps.update(steps)

# Sort all steps
all_steps = sorted(list(all_steps))
data['step'] = all_steps

# Second pass: align all test metrics to the same step indices
for tag in available_test_metrics:
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
print("Available test data columns:", list(data.keys()))
print(f"Test data length: {len(all_steps)} steps")

df = pd.DataFrame(data)
# Forward fill missing values
df = df.ffill()
df = df.sort_values("step")
df.to_csv(f"{run_id}_test_data.csv", index=False)

print(f"Saved test data CSV to: {run_id}_test_data.csv")
