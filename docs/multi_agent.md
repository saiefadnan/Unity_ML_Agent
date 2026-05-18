# Multi-Agent System (S2)

_A comparison of the single-agent (S1) and cooperative multi-agent (S2) systems, focusing on observation, communication, and reward structure differences._

The multi-agent setup (S2) extends the single-agent baseline by introducing cooperative behaviors through shared observations and team-based rewards. Three agents operate in the same environment, sharing a single trained policy but acting independently.

## S1 vs. S2: Key Differences

The primary distinction lies in the agent's awareness of its teammates and the environment's reward structure.

| Feature                   | Single-Agent (S1)                              | Multi-Agent (S2)                                                                    | Purpose of Change                                                    |
| ------------------------- | ---------------------------------------------- | ----------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| **Agent Count**           | 1                                              | 3                                                                                   | To enable parallel search and faster area coverage.                  |
| **Policy**                | Individual                                     | Shared Policy (all agents use the same trained model)                               | Promotes learning a general, reusable strategy.                      |
| **Observation Space**     | 13 vector dimensions                           | 23 vector dimensions                                                                | Adds awareness of teammates' state (position, velocity, HP).         |
| **Communication**         | None                                           | **GPS-based**: Each agent observes the relative position of its teammates.          | Allows for emergent coordination and deconfliction.                  |
| **Exploration Reward**    | Based on personal grid coverage.               | **Team-based**: Agents are rewarded for expanding the _team's_ total explored area. | Incentivizes spreading out to cover more ground.                     |
| **Victim Assignment**     | Agent pursues the nearest victim.              | **Victim Claiming**: Agents "claim" the nearest _unclaimed_ victim.                 | Prevents multiple agents from inefficiently chasing the same target. |
| **Termination Condition** | Episode ends when the agent finds all victims. | Episode ends when the _team_ collectively finds all victims.                        | Defines success at the team level.                                   |

## GPS-Based Communication

In the S2 setup, agents do not communicate directly but are aware of each other through their observation space. This simulates a simple GPS system.

- **Data Shared**: As detailed in `MultiAgent2D.cs`, for each of the other two teammates, an agent observes:
  1.  **Relative Position (2 dims)**: The `(x, y)` vector from itself to the teammate, normalized by the arena size.
  2.  **Velocity (2 dims)**: The teammate's `(x, y)` velocity, normalized.
  3.  **Health (1 dim)**: The teammate's current HP, normalized from 0 to 1.
- **Total Communication Overhead**: This adds `(2 + 2 + 1) * 2 = 10` dimensions to the observation vector compared to the S1 setup.
- **Effect**: This information allows the policy to learn behaviors like avoiding collisions with teammates, spreading out, or potentially moving to assist a damaged teammate (though the latter is an emergent, not explicitly rewarded, behavior).

## Cooperative vs. Individual Rewards

While most of the physics-based rewards (e.g., for stability, avoiding obstacles) remain individual, several key rewards are modified to promote teamwork.

- **Exploration**:
  - `personalVisitedCells` reward: A small reward for exploring a cell the individual agent has not seen.
  - `RegisterExploredCell` reward: A slightly larger reward if that cell is also new to the _entire team's_ explored map (`teamExploredCells`). This explicitly incentivizes expanding the team's knowledge.
- **Separation**: A small penalty is applied if an agent gets too close (`< 3.0` units) to a teammate, encouraging them to maintain a safe and efficient formation.
- **Completion**: The large terminal reward for mission success is given to all agents when the team finds the last victim, regardless of which agent made the final discovery.

## Configuration Differences

The trainer configurations in `multi_gps.yaml` are adjusted to handle the increased data flow from three agents.

| Hyperparameter | `single_occ.yaml` (S1) | `multi_gps.yaml` (S2) | Rationale                                                                |
| -------------- | ---------------------- | --------------------- | ------------------------------------------------------------------------ |
| `batch_size`   | 512                    | 1024                  | Larger batch to handle more experiences per update from multiple agents. |
| `buffer_size`  | 5120                   | 10240                 | Larger buffer to store a more diverse set of experiences.                |

## Related Docs

- [Environment and Task Definition](./environment_and_task.md)
- [Results and Analysis](./results.md)
