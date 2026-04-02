# Post-Training Guide

## After drone6.10 (Single Agent) and drone7.2 (Multi Agent) are done

---

## Phase 1: Verify Training Results

### 1.1 Open TensorBoard

```powershell
tensorboard --logdir=results
```

Open browser: `http://localhost:6006`

### 1.2 Check These Curves for Both Models

| Metric              | Expected                           |
| ------------------- | ---------------------------------- |
| `Cumulative Reward` | Should be increasing and plateaued |
| `Episode Length`    | Should be decreasing over time     |
| `Policy Loss`       | Should be decreasing               |
| `Value Loss`        | Should be decreasing               |

> ✅ If reward plateaued and losses are low → model is converged → ready for testing

---

## Phase 2: Export Training Metrics (for Thesis Training Curves)

### 2.1 Fetch drone6.10 training data

```powershell
cd G:\unity_files\Unity_ML_Agent
python data_fetch\fetch_data.py
```

### 2.2 Check CSVs are saved in `data_fetch/`

- `drone6_training_data.csv` → single agent
- `drone7_training_data.csv` → multi agent

### 2.3 Note down from TensorBoard (manually)

- Final average reward (last 100 episodes)
- Total training steps
- Best reward achieved

---

## Phase 3: Run Inference / Test Phase

> Load trained `.onnx` into Unity → build → run without training flag

### 3.1 Assign ONNX Models in Unity Inspector

- Open `thesis-1` project in Unity
- Select DroneAgent GameObject → Behavior Parameters → Model → assign `drone6.10/DroneAgent.onnx`
- Build the project: `File → Build Settings → Build`

- Open `Multi_Drone2D` project in Unity
- Select each drone GameObject → assign `drone7.2/DroneAgent.onnx`
- Build the project

### 3.2 Run Inference (No Training)

**Single Agent:**

```powershell
mlagents-learn config2D\single_occ.yaml --run-id=drone6.10_test --env=G:\unity_files\thesis-1\build\thesis-1.exe --inference
```

**Multi Agent:**

```powershell
mlagents-learn config2D\multi_gps.yaml --run-id=drone7.2_test --env=G:\unity_files\Multi_Drone2D\Build\Multi_Drone2D.exe --inference
```

### 3.3 Run for 100 Episodes Each

Let it run until 100 episodes complete, then stop with `Ctrl+C`.

### 3.4 Record These Metrics Per Model

| Metric                    | How to Get                                                                   |
| ------------------------- | ---------------------------------------------------------------------------- |
| Victims found per episode | TensorBoard: `Agent/VictimsFound` or `Team/TotalVictims`                     |
| Completion rate (%)       | Count episodes where all victims found / 100                                 |
| Average steps to complete | TensorBoard: `Episode Length`                                                |
| Crash rate                | Count `drone_destroyed` / `hard_crash_destroyed` episodes                    |
| Area coverage             | TensorBoard: `Team/ExploredCells` (multi) or `personalVisitedCells` (single) |

---

## Phase 4: Implement Scripted Baselines

> Add these scripts to **both** Unity projects for comparison

### 4.1 Random Walk Agent

Create `Assets/scripts/RandomAgent.cs` in `thesis-1` project:

```csharp
using UnityEngine;

public class RandomAgent : MonoBehaviour
{
    Rigidbody2D rb;
    float changeTimer = 0f;
    Vector2 currentForce;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PickNewDirection();
    }

    void FixedUpdate()
    {
        changeTimer -= Time.fixedDeltaTime;
        if (changeTimer <= 0f) PickNewDirection();

        float gravComp = rb.mass * Mathf.Abs(Physics2D.gravity.y);
        rb.AddForce(Vector2.up * gravComp, ForceMode2D.Force);
        rb.AddForce(currentForce * 5f, ForceMode2D.Force);
    }

    void PickNewDirection()
    {
        currentForce = Random.insideUnitCircle.normalized;
        changeTimer = Random.Range(0.5f, 2f);
    }
}
```

### 4.2 Greedy Nearest Victim Agent

Create `Assets/scripts/GreedyAgent.cs` in `thesis-1` project:

```csharp
using UnityEngine;

public class GreedyAgent : MonoBehaviour
{
    Rigidbody2D rb;
    public EnvManager envManager;

    void Start() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        float gravComp = rb.mass * Mathf.Abs(Physics2D.gravity.y);
        rb.AddForce(Vector2.up * gravComp, ForceMode2D.Force);

        GameObject nearest = FindNearestVictim();
        if (nearest != null)
        {
            Vector2 dir = (nearest.transform.position - transform.position).normalized;
            rb.AddForce(dir * 8f, ForceMode2D.Force);
        }
    }

    GameObject FindNearestVictim()
    {
        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        foreach (var v in envManager.activeVictims)
        {
            if (v == null || !v.activeSelf) continue;
            float d = Vector2.Distance(transform.position, v.transform.position);
            if (d < minDist) { minDist = d; nearest = v; }
        }
        return nearest;
    }
}
```

### 4.3 Run Each Baseline for 100 Episodes

Record the same metrics as Phase 3.

---

## Phase 5: Build Comparison Table

Fill this in after running all 4 methods for 100 episodes each:

| Method                   | Victims/Ep | Completion% | Avg Steps | Crash Rate | Coverage |
| ------------------------ | ---------- | ----------- | --------- | ---------- | -------- |
| Random Walk              |            |             |           |            |          |
| Greedy Nearest           |            |             |           |            |          |
| PPO Single (`drone6.10`) |            |             |           |            |          |
| PPO Multi (`drone7.2`)   |            |             |           |            |          |

---

## Phase 6: Plot Results (Python)

### 6.1 Training Curves

```python
# Run from G:\unity_files\Unity_ML_Agent
python data_fetch\analyze_training_data.py
```

### 6.2 Manual Comparison Plot

Create `plot_comparison.py`:

```python
import matplotlib.pyplot as plt
import numpy as np

methods = ['Random', 'Greedy', 'Single PPO', 'Multi PPO']
victims = [0.5, 1.8, 0, 0]        # fill in your values
completion = [5, 15, 0, 0]         # fill in your values
steps = [2000, 1200, 0, 0]         # fill in your values

fig, axes = plt.subplots(1, 3, figsize=(15, 5))

axes[0].bar(methods, victims, color=['gray','orange','blue','green'])
axes[0].set_title('Victims Found per Episode')
axes[0].set_ylabel('Count')

axes[1].bar(methods, completion, color=['gray','orange','blue','green'])
axes[1].set_title('Completion Rate (%)')
axes[1].set_ylabel('%')

axes[2].bar(methods, steps, color=['gray','orange','blue','green'])
axes[2].set_title('Avg Steps to Complete')
axes[2].set_ylabel('Steps')

plt.tight_layout()
plt.savefig('results_comparison.png', dpi=150)
plt.show()
```

---

## Phase 7: Ablation Studies (Optional — if time allows)

Run these only if you have time. Each needs ~500k steps of training.

| Ablation            | Change to Make                       | New Run ID                    |
| ------------------- | ------------------------------------ | ----------------------------- |
| No Curriculum       | Remove `curriculum:` block from YAML | `drone6.ablation_nocurr`      |
| No LSTM             | Remove `memory:` block from YAML     | `drone6.ablation_nolstm`      |
| No Curiosity        | Remove `curiosity:` from YAML        | `drone6.ablation_nocuriosity` |
| No Obstacle Penalty | Comment out obstacle reward code     | `drone6.ablation_noobs`       |

Compare final reward of each ablation vs `drone6.10` baseline.

---

## Summary Checklist

```
[ ] drone6.10 training complete
[ ] drone7.2 training complete
[ ] Training curves exported (TensorBoard / CSV)
[ ] drone6.10 inference run (100 episodes, metrics recorded)
[ ] drone7.2 inference run (100 episodes, metrics recorded)
[ ] Random Walk baseline run (100 episodes)
[ ] Greedy Nearest baseline run (100 episodes)
[ ] Comparison table filled in
[ ] Plots generated
[ ] Ablation studies (optional)
[ ] Thesis results section written
```

---

## Files to Include in Thesis

| File                             | What it shows                       |
| -------------------------------- | ----------------------------------- |
| `results/drone6.10/` TensorBoard | Single agent learning curve         |
| `results/drone7.2/` TensorBoard  | Multi agent learning curve          |
| `results_comparison.png`         | Bar chart comparing all methods     |
| Comparison table (Phase 5)       | Quantitative performance comparison |
