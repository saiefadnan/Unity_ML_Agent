# Reward Signal Design

_A detailed breakdown of the reward components and curriculum learning stages that shape the agent's behavior._

The agent's learning is guided by a complex reward function composed of multiple positive and negative incentives. This function is designed to encourage efficient and safe Search and Rescue behavior.

## Reward Component Breakdown

The following table details every reward and penalty term implemented in `Agent2D.cs` and `MultiAgent2D.cs`.

| Component Name                 | Value / Expression                                         | Purpose                                                               | Type            |
| ------------------------------ | ---------------------------------------------------------- | --------------------------------------------------------------------- | --------------- | ----------------------------------------------------------------------- | ------ |
| **Task Completion**            |                                                            |                                                                       |                 |
| Find Victim                    | `+5.0` (base) + `+1.0` (time bonus) + `+2.0` (diff. bonus) | Primary objective: strongly incentivizes finding victims.             | Reward          |
| Find Unclaimed Victim (S2)     | `+4.0` (base) + bonuses                                    | Slightly lower reward for finding a victim claimed by another agent.  | Reward          |
| Mission Complete               | `+10.0` + `efficiency * 5.0`                               | Large terminal reward for finding all victims, plus efficiency bonus. | Reward          |
| **Safety & Stability**         |                                                            |                                                                       |                 |
| Hard Crash (Ground/Obstacle)   | `-damage / MAX_HP * 3.0` (max ~-3.0)                       | Severe penalty for high-speed collisions leading to destruction.      | Penalty         |
| Rough Landing                  | `-0.5`                                                     | Penalty for landing too fast but not crashing.                        | Penalty         |
| Grinding Obstacle              | `-0.02` (per frame)                                        | Continuous penalty for staying in contact with an obstacle.           | Penalty         |
| Obstacle Proximity             | `-(2 - dist) / 2 * 0.05`                                   | Graduated penalty for getting closer than 2 units to an obstacle.     | Penalty         |
| High Speed Near Obstacle       | `-(speed - 3) / 10 * 0.03`                                 | Penalizes moving too fast when near obstacles to encourage braking.   | Penalty         |
| Unstable Angle (> 72°)         | `-0.2`                                                     | Strong penalty for extreme tilt to prevent flipping over.             | Penalty         |
| Spinning (> 100 ang. vel.)     | `-0.1`                                                     | Penalty for high angular velocity.                                    | Penalty         |
| Fast Descent Near Ground       | `-0.05` to `-0.2`                                          | Strong penalty for high vertical speed when close to the ground.      | Penalty         |
| Out of Bounds                  | `-10.0` (terminal)                                         | Severe penalty for leaving the designated operational area.           | Penalty         |
| Gentle/Soft Landing            | `+0.5` / `+0.2`                                            | Rewards controlled, slow contact with the ground.                     | Reward          |
| Being Upright                  | `(1 -                                                      | angle                                                                 | /180) \* 0.005` | Small, continuous reward for maintaining a stable, upright orientation. | Reward |
| **Efficiency & Exploration**   |                                                            |                                                                       |                 |
| Progress Towards Target        | `improvement * 3.0` (capped at `+0.08`)                    | Rewards moving closer to the target victim.                           | Reward          |
| Moving Away from Target        | `-0.015`                                                   | Penalizes backtracking or moving away from the objective.             | Penalty         |
| Explore New Grid Cell (S1)     | `0.05 / (1 + count * 0.05)`                                | Decaying reward for visiting a new cell in the exploration grid.      | Reward          |
| Explore New Personal Cell (S2) | `0.03 / (1 + count * 0.05)`                                | Reward for personal exploration in the multi-agent setup.             | Reward          |
| Explore New Team Cell (S2)     | `+0.02`                                                    | Reward for expanding the team's total explored area.                  | Reward          |
| Teammate Separation (S2)       | `-(3 - dist) / 3 * 0.005`                                  | Small penalty for being too close (< 3 units) to a teammate.          | Penalty         |
| Idle Hovering                  | `-0.1` to `-1.0` (terminal)                                | Penalizes staying stationary for too long to prevent non-action.      | Penalty         |
| Time/Energy Cost               | `-0.003` (per step)                                        | Small, constant penalty to encourage finishing the episode quickly.   | Penalty         |

## Curriculum Learning

A curriculum is used to gradually increase the task difficulty, allowing the agent to master basic skills before tackling the full problem. The curriculum is defined in `config2D/single_occ.yaml` and `config2D/multi_gps.yaml` and progresses based on the agent's measured reward.

| Stage Name        | `target_distance` | `obstacle_count` | Completion Threshold (Reward) | Purpose                                                               |
| ----------------- | ----------------- | ---------------- | ----------------------------- | --------------------------------------------------------------------- |
| `hover_training`  | 0.0               | 0                | 0.5                           | Teach the agent to stay airborne at a stable altitude.                |
| `easy_distance`   | 8.0               | 0                | 5.0                           | Introduce the concept of finding a nearby victim with no obstacles.   |
| `medium_distance` | 12.0              | 2                | 8.0                           | Find victims further away while navigating a few obstacles.           |
| `far_distance`    | 20.0              | 4                | 8.0                           | Increase navigation distance and obstacle density.                    |
| `max_obstacles`   | 20.0              | 6                | -                             | Final stage with the maximum number of obstacles and large distances. |

## Design Rationale

- **Dense vs. Sparse Rewards**: The function is dense, providing feedback at almost every step. This is crucial for learning complex motor control and navigation. Sparse rewards (e.g., only upon finding a victim) would make learning prohibitively slow.
- **Safety First**: Significant penalties for crashes, instability, and obstacle proximity teach the agent to prioritize survival and careful maneuvering over speed.
- **Efficiency Incentives**: Rewards for progress, path efficiency, and exploration, combined with a time-step penalty, push the agent to complete the mission efficiently rather than wandering aimlessly.
- **Cooperation (S2)**: In the multi-agent setup, rewards for team exploration and penalties for crowding encourage agents to spread out and cover the area collaboratively.

## Related Docs

- [Reinforcement Learning Algorithm](./rl_algorithm.md)
- [Results and Analysis](./results.md)
