import pandas as pd

# Read the test data (performance metrics only)
df = pd.read_csv("drone4_test_data.csv")

# Calculate summary statistics based on test metrics
summary = {
    "Total Reward (mean)": df["Reward"].mean(),
    "Episode Length (mean)": df["Environment/Episode Length"].mean(),
    "Targets Found (mean)": df["TargetsFound"].mean(),
    "Path Efficiency (mean)": df["PathEfficiency"].mean(),
    "Angle Stability (mean)": df["AngleStability"].mean(),
    "Collision Rate (%)": df["GroundCollision"].mean() * 100,
    "Cumulative Reward (mean)": df["Environment/Cumulative Reward"].mean()
}

print("=== Test Data Summary (Performance Metrics Only) ===")
for k, v in summary.items():
    print(f"{k}: {v:.3f}")

# Show performance progression over time
print("\n=== Performance Progression ===")
early_training = df[df['step'] <= 1000000]  # First 1M steps
mid_training = df[(df['step'] > 1000000) & (df['step'] <= 2500000)]  # Mid training
late_training = df[df['step'] > 2500000]  # Final training

print("Early Training (0-1M steps):")
print(f"  Reward: {early_training['Reward'].mean():.3f}")
print(f"  Targets Found: {early_training['TargetsFound'].mean():.3f}")
print(f"  Path Efficiency: {early_training['PathEfficiency'].mean():.3f}")

print("Mid Training (1M-2.5M steps):")
print(f"  Reward: {mid_training['Reward'].mean():.3f}")
print(f"  Targets Found: {mid_training['TargetsFound'].mean():.3f}")
print(f"  Path Efficiency: {mid_training['PathEfficiency'].mean():.3f}")

print("Late Training (2.5M+ steps):")
print(f"  Reward: {late_training['Reward'].mean():.3f}")
print(f"  Targets Found: {late_training['TargetsFound'].mean():.3f}")
print(f"  Path Efficiency: {late_training['PathEfficiency'].mean():.3f}")

# Best performance achieved
print("\n=== Best Performance Achieved ===")
best_reward_idx = df['Reward'].idxmax()
best_step = df.loc[best_reward_idx, 'step']
print(f"Best Reward: {df['Reward'].max():.3f} at step {best_step}")
print(f"Max Targets Found: {df['TargetsFound'].max():.3f}")
print(f"Best Path Efficiency: {df['PathEfficiency'].max():.3f}")
print(f"Best Angle Stability: {df['AngleStability'].max():.3f}")
print(f"Lowest Collision Rate: {df['GroundCollision'].min() * 100:.3f}%")
