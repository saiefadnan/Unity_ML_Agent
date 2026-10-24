# Results and Analysis

_A quantitative analysis of the performance of the single-agent (S1) and multi-agent (S2) systems based on 100 evaluation episodes._

The following results are drawn from the benchmark evaluation logs located at `data/test/Agent2D_Test_Results.csv` and `data/test/2DMultiAgent_Test_Results.csv` (formally summarized in Table V of the thesis).

## Single-Agent (S1) Test Results

This setup involved one agent tasked with finding 6 victims in the 2D obstacle arena.

| Metric | Average Value | Notes |
|---|---|---|
| **Mission Success Rate** | **65.0%** | Ratio of episodes where target objectives were completed. |
| **Avg Victims Rescued** | 3.90 / 6 | On average, the single drone rescued ~4 victims before termination. |
| **Avg Steps Taken** | 553 | Mean steps across test episodes. |
| **Completion Time** | 27.67 s | Simulated flight duration (`553 steps * 0.05s/step`). |
| **Avg Drone HP** | -2.94 | Residual HP across all episodes (reflecting severe collision damage). |
| **Path Efficiency** | 0.240 | Ratio of direct Euclidean distance to actual distance traveled. |
| **Avg Explored Cells** | 12.5 | Grid cells surveyed during the episode. |

### S1 Episode Termination Breakdown

- `drone_destroyed`: 55%
- `grinding_obstacle_destroyed`: 19%
- `Completion`: 18%
- `out_of_bounds`: 5%
- `idle_hovering`: 2%
- `timeout`: 1%

---

## Multi-Agent (S2) Test Results

This setup involved a cooperative team of three agents tasked with locating 6 victims using shared policies and inter-drone GPS communication.

| Metric | Average Value | Notes |
|---|---|---|
| **Mission Success Rate** | **100.0%** | 100 out of 100 benchmark episodes completed successfully. |
| **Avg Team Victims Rescued** | **6.00 / 6** | The team consistently rescued all 6 spawned victims. |
| **Avg Steps Taken** | 266 | Mean steps to clear the entire arena. |
| **Completion Time** | 13.28 s | Simulated flight duration (`266 steps * 0.05s/step`), >2× faster than S1. |
| **Avg Drone HP (Team Mean)** | **96.08** | High team survival (Agent 0: 95.2, Agent 1: 97.2, Agent 2: 95.8). |
| **Crash Rate** | **0.0%** | Flawless collision-free cooperative navigation. |
| **Avg Path Efficiency (Team)** | 1.726 | Median team path efficiency with coordinated trajectories. |
| **Avg Team Explored Cells** | 23.3 | Broad multi-quadrant coverage. |

### S2 Per-Agent Contribution (Averages)

| Agent | Avg Goals Rescued | Avg HP | Avg Path Efficiency |
|---|---|---|---|
| **Agent 0** | 2.50 | 95.2 | 1.810 |
| **Agent 1** | 1.60 | 97.2 | 1.987 |
| **Agent 2** | 2.00 | 95.8 | 1.777 |

---

## S1 vs. S2 Performance Comparison (Table V Benchmark)

| Metric | Single-Agent (S1) | Multi-Agent (S2) | Relative Improvement |
|---|---|---|---|
| **Mission Success Rate** | 65.0% | **100.0%** | **+53.8%** (+1900% vs. Random Baseline) |
| **Avg Victims Rescued** | 3.90 / 6 | **6.00 / 6** | **+53.8%** (100% arena clearance) |
| **Avg Steps to Complete** | 553 | **266** | **-51.9%** (2× faster completion) |
| **Completion Time (s)** | 27.67 s | **13.28 s** | **-52.0%** (Halved mission duration) |
| **Avg Drone HP Survival** | -2.94 | **96.08** | **+103%** (Near-zero crash rate) |
| **Path Efficiency** | 0.240 | **1.726** | **+619%** (Direct, coordinated routes) |
| **Explored Area (cells)** | 12.5 | **23.3** | **+86.4%** (Comprehensive team search) |

---

## Interpretation and Conclusion

The empirical benchmark results demonstrate the clear superiority of the cooperative multi-agent system (S2) over the single-agent baseline (S1):

1. **Reliability and Safety**: The multi-agent swarm achieved a flawless 100% mission clearance rate with zero crashes, preserving an average 96.08 HP out of 100, whereas the single drone frequently accumulated collision damage (-2.94 HP) and struggled to clear the full arena independently.
2. **Search Speed and Parallelism**: By dividing the search volume among three agents, S2 completed the rescue mission in **13.28 s** (266 steps) compared to **27.67 s** (553 steps) for S1—representing a **52% reduction in search time**.
3. **Coordinated Exploration**: Shared policy awareness and decentralized target claiming prevented duplicate searches, yielding an 86% expansion in explored cells (23.3 vs. 12.5 cells) and a more than 7-fold increase in trajectory path efficiency (1.726 vs. 0.240).

---

## Related Docs

- [Project Overview](./overview.md) — System architecture diagram and setup summary
- [Reward Signal Design](./reward_design.md) — Multi-component reward formulations
- [Multi-Agent System](./multi_agent.md) — GPS communication and cooperative mechanics
- [Thesis Roadmap (3D Extension)](https://github.com/saiefadnan/Unity_ML_3DAgent/blob/main/THESIS_ROADMAP.md) — Cross-environment 2D-to-3D thesis plan
