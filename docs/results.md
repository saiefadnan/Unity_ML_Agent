# Results and Analysis

_A quantitative analysis of the performance of the single-agent (S1) and multi-agent (S2) systems based on 100 evaluation episodes._

The following results were extracted from the test evaluation logs located at `data/test/Agent2D_Test_Results.csv` and `data/test/MultiAgent2D_Test_Results.csv`.

## Single-Agent (S1) Test Results

This setup involved one agent tasked with finding 6 victims.

| Metric                    | Average Value | Notes                                                  |
| ------------------------- | ------------- | ------------------------------------------------------ |
| **Success Rate**          | **18.0%**     | 18 out of 100 episodes completed successfully.         |
| **Avg Victims Rescued**   | 3.89 / 6      | On average, the agent found ~4 victims before failing. |
| **Avg Steps Taken**       | 642           | Average for _successful_ episodes only.                |
| **Completion Time**       | 12.84 s       | `Avg Steps * 0.02s/step`.                              |
| **Avg Drone HP**          | -2.94         | Average HP across all episodes (including crashes).    |
| **Crash Rate**            | **74.0%**     | 74 episodes ended due to destruction.                  |
| **Path Efficiency**       | 0.248         | Ratio of ideal path to actual distance traveled.       |
| **Avg Distance Traveled** | 139.16 m      |                                                        |
| **Avg Explored Cells**    | 12.5          |                                                        |

### S1 Episode Outcome Breakdown

- `drone_destroyed`: 55%
- `grinding_obstacle_destroyed`: 19%
- `Completion`: 18%
- `out_of_bounds`: 5%
- `idle_hovering`: 2%
- `timeout`: 1%

## Multi-Agent (S2) Test Results

This setup involved a team of three agents tasked with finding 5 victims.

| Metric                         | Average Value      | Notes                                              |
| ------------------------------ | ------------------ | -------------------------------------------------- |
| **Success Rate**               | **99.0%**          | 99 out of 100 episodes completed successfully.     |
| **Avg Team Victims Rescued**   | 4.99 / 5           | The team almost always found all victims.          |
| **Avg Steps Taken**            | 263                | Average for _successful_ episodes only.            |
| **Completion Time**            | 5.26 s             | `Avg Steps * 0.02s/step`.                          |
| **Avg Drone HP (per agent)**   | 91.2 / 92.5 / 94.3 | High survival rate across the team.                |
| **Crash Rate**                 | **1.0%**           | Only 1 episode ended in failure (`out_of_bounds`). |
| **Avg Path Efficiency (Team)** | 1.764              | Average of all agents' efficiencies.               |
| **Avg Team Explored Cells**    | 23.4               |                                                    |

### S2 Per-Agent Contribution (Averages)

| Agent   | Avg Goals Found | Avg HP | Avg Efficiency |
| ------- | --------------- | ------ | -------------- |
| Agent 0 | 2.10            | 91.2   | 1.683          |
| Agent 1 | 1.53            | 92.5   | 1.845          |
| Agent 2 | 1.36            | 94.3   | 1.764          |

## S1 vs. S2 Performance Comparison

| Metric              | Single-Agent (S1) | Multi-Agent (S2) | Improvement/Change                          |
| ------------------- | ----------------- | ---------------- | ------------------------------------------- |
| **Success Rate**    | 18.0%             | **99.0%**        | **+450%** (Drastic increase in reliability) |
| **Completion Time** | 12.84 s           | **5.26 s**       | **-59%** (Significantly faster)             |
| **Crash Rate**      | 74.0%             | **1.0%**         | **-98.6%** (Vastly improved safety)         |
| **Exploration**     | 12.5 cells        | **23.4 cells**   | **+87%** (More area covered)                |
| **Path Efficiency** | 0.248             | 1.764            | Higher value indicates more direct paths.   |

## Interpretation and Conclusion

The results clearly demonstrate the profound superiority of the cooperative multi-agent system (S2) over the single-agent system (S1) for this SAR task.

1.  **Reliability and Safety**: The most striking result is the jump in success rate from a dismal 18% to a near-perfect 99%. The S2 system's ability to coordinate and cover more ground in parallel makes it far more robust. The crash rate plummeted from 74% to just 1%, indicating that the shared policy and teammate awareness led to much safer emergent behavior.

2.  **Efficiency**: The S2 team completed the mission in less than half the time of a successful S1 agent. This is a direct result of parallel search: three agents can explore different parts of the map simultaneously. The higher path efficiency in S2 suggests that the victim claiming mechanism and team exploration rewards successfully discouraged redundant work and encouraged more direct routes.

3.  **Applicability for SAR**: A single drone system with an 18% success rate would be entirely unsuitable for real-world search and rescue, where reliability is paramount. A system with a 99% success rate, however, becomes a viable candidate. The speed and safety of the multi-agent approach make it a far more practical solution for time-critical disaster response scenarios. The cooperative model proves to be not just an incremental improvement, but a transformative one.

## Related Docs

- [Reward Signal Design](./reward_design.md)
- [Project Overview](./overview.md)
