from tensorboard.backend.event_processing import event_accumulator
import pandas as pd
import os

# ---- CHANGE THIS ----
run_id = "drone4"   # your run-id folder name
results_dir = "results"
metric_type = "performance"  # Options: "performance", "training", "policy"
# ----------------------

path = f"{results_dir}/{run_id}/MyAgent"

ea = event_accumulator.EventAccumulator(path)
ea.Reload()

# Define different metric categories
metric_categories = {
    "performance": [
        'Reward',
        'Environment/Cumulative Reward', 
        'Environment/Episode Length',
        'EpisodeLength',
        'TargetsFound',
        'PathEfficiency',
        'AngleStability',
        'GroundCollision'
    ],
    "training": [
        'Losses/Policy Loss',
        'Losses/Value Loss', 
        'Losses/Pretraining Loss'
    ],
    "policy": [
        'Policy/Entropy',
        'Policy/Extrinsic Value Estimate',
        'Policy/Extrinsic Reward',
        'Policy/Learning Rate',
        'Policy/Epsilon',
        'Policy/Beta'
    ]
}

# Select metrics based on category
selected_metrics = metric_categories.get(metric_type, [])
available_metrics = [tag for tag in ea.Tags()['scalars'] if tag in selected_metrics]

print(f"Extracting {metric_type} metrics:", available_metrics)

data = {}
all_steps = set()

# Collect all steps
for tag in available_metrics:
    events = ea.Scalars(tag)
    steps = [e.step for e in events]
    all_steps.update(steps)

all_steps = sorted(list(all_steps))
data['step'] = all_steps

# Align metrics
for tag in available_metrics:
    events = ea.Scalars(tag)
    step_to_value = {e.step: e.value for e in events}
    
    aligned_values = []
    for step in all_steps:
        aligned_values.append(step_to_value.get(step, None))
    
    data[tag] = aligned_values

df = pd.DataFrame(data)
df = df.ffill()
df = df.sort_values("step")

output_file = f"{run_id}_{metric_type}_data.csv"
df.to_csv(output_file, index=False)
print(f"Saved {metric_type} data to: {output_file}")
