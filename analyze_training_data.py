import pandas as pd

# Read the training data
df = pd.read_csv("drone4_training_data.csv")

# Calculate summary statistics based on available columns
summary = {
    "Total Reward (mean)": df["Reward"].mean(),
    "Episode Length (mean)": df["Environment/Episode Length"].mean(),
    "Targets Found (mean)": df["TargetsFound"].mean(),
    "Path Efficiency (mean)": df["PathEfficiency"].mean(),
    "Angle Stability (mean)": df["AngleStability"].mean(),
    "Collision Rate (%)": df["GroundCollision"].mean() * 100,
    "Cumulative Reward (mean)": df["Environment/Cumulative Reward"].mean(),
    "Policy Entropy (mean)": df["Policy/Entropy"].mean()
}

print("=== Training Data Summary ===")
for k, v in summary.items():
    print(f"{k}: {v:.3f}")

# Additional analysis - show data ranges
print("\n=== Data Ranges ===")
print(f"Reward range: {df['Reward'].min():.3f} to {df['Reward'].max():.3f}")
print(f"Episode Length range: {df['Environment/Episode Length'].min():.1f} to {df['Environment/Episode Length'].max():.1f}")
print(f"Targets Found range: {df['TargetsFound'].min():.3f} to {df['TargetsFound'].max():.3f}")
print(f"Path Efficiency range: {df['PathEfficiency'].min():.3f} to {df['PathEfficiency'].max():.3f}")
print(f"Training steps: {df['step'].min()} to {df['step'].max()}")

# Show final performance (last 10% of training)
final_10_percent = df[df['step'] >= df['step'].max() * 0.9]
print("\n=== Final 10% Performance ===")
final_summary = {
    "Final Reward (mean)": final_10_percent["Reward"].mean(),
    "Final Episode Length (mean)": final_10_percent["Environment/Episode Length"].mean(),
    "Final Targets Found (mean)": final_10_percent["TargetsFound"].mean(),
    "Final Path Efficiency (mean)": final_10_percent["PathEfficiency"].mean(),
    "Final Angle Stability (mean)": final_10_percent["AngleStability"].mean(),
    "Final Collision Rate (%)": final_10_percent["GroundCollision"].mean() * 100
}

for k, v in final_summary.items():
    print(f"{k}: {v:.3f}")
