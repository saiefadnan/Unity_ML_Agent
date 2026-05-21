# Environment and Task Definition

_Describes the Search and Rescue (SAR) task, the 2D Unity environment, and the agent's perception and action capabilities._

The core task is for one or more drone agents to find and "rescue" all victims in a 2D environment as quickly and efficiently as possible while avoiding obstacles and self-destruction.

## Task Definition

- **Success Conditions**:
  - **Single-Agent (S1)**: The agent successfully navigates to and triggers the collider of every victim object in the scene. The episode ends with a large positive reward upon finding the last victim.
  - **Multi-Agent (S2)**: The team of agents collectively finds all victims. The episode ends when the last victim is found by any agent.

- **Failure Conditions**:
  - **Destruction**: The agent's HP drops to zero or below from colliding with obstacles or the ground at high velocity.
  - **Out of Bounds**: The agent flies outside the predefined arena boundaries (`y > 8`, `x < -16`, or `x > 16`).
  - **Timeout**: The episode exceeds the maximum step count:
    - **Single-Agent (S1)**: `maxStepCount = 4000`
    - **Multi-Agent (S2)**: `maxStepCount = 2000`
  - **Idle Hovering**: The agent remains nearly stationary for an extended period (over 80 steps), indicating non-productive behavior.

## 2D Unity Environment

The environment is a 2D physics-based world created in Unity.

- **Bounds**: The primary navigable area is a rectangle approximately 32 units wide (x from -16 to 16) and 12 units high (y from -4 to 8).
- **Agent Spawning**:
  - **S1**: A single agent spawns at `(x, 1.5)` where `x` is a random value between -7 and 7.
  - **S2**: Three agents spawn at `y = 1.5` with horizontal positions spread evenly across the arena to encourage initial area division.
- **Victim Placement**: Victims are spawned at random positions within the arena. The placement logic (`GetRandomTargetPosition`) ensures they are spread out, with vertical positions varying to appear at ground level or elevated, requiring the agent to fly over obstacles.
- **Obstacle Placement**: Obstacles are chosen from a predefined array and activated up to the `obstacle_count` specified by the curriculum. Their horizontal positions are randomized to create new layouts each episode.

## Observation Space

The agent perceives the environment through a combination of vector observations and raycasts.

### Single-Agent (S1) Observation Space (13 Dimensions + Rays)

_Source: `scripts/single_agent_scripts/Agent2D.cs`_

- **Agent State (6 dims)**:
  - `rb.linearVelocity / 10f` (2): Normalized X/Y velocity.
  - `rb.angularVelocity / 180f` (1): Normalized angular velocity.
  - `transform.localPosition / 16f` (2): Normalized X/Y position.
  - `normalizedRotation / 180f` (1): Z-axis rotation, normalized from -1 to 1.
- **Task Context (5 dims)**:
  - `missionProgress` (1): Ratio of victims found to total victims.
  - `dirToTarget.normalized` (2): X/Y direction vector to the nearest victim.
  - `distToTarget / 20f` (1): Normalized distance to the nearest victim.
  - `remainingVictims / 5f` (1): Normalized count of remaining victims.
- **Ground Proximity (2 dims)**:
  - `groundDistance / 10f` (1): Normalized distance to the ground directly below.
  - `droneHP / MAX_HP` (1): Current health points, normalized 0-1.
- **Ray Perception Sensor (2D)**:
  - The `RayPerceptionSensorComponent2D` provides additional observations by casting rays (total 25) to detect `Obstacle`, `Ground`, and `Victim` tags.

### Multi-Agent (S2) Observation Space (23 Dimensions + Rays)

_Source: `scripts/multi_agent_scripts/MultiAgent2D.cs`_

The S2 agent observes everything the S1 agent does, plus information about its teammates.

- **S1 Base Observations (13 dims)**: All observations from the single-agent setup are included.
- **Teammate GPS Communication (10 dims for 2 other agents)**: For each of the other 2 agents, the following is observed:
  - `relPos / 32f` (2): Relative X/Y position to the teammate, normalized.
  - `info.velocity / 10f` (2): Teammate's normalized X/Y velocity.
  - `info.hp / MAX_HP` (1): Teammate's normalized health.

## Action Space

The agent uses a **continuous action space** with 3 dimensions.

_Source: `OnActionReceived` method in `Agent2D.cs` and `MultiAgent2D.cs`_

- **Action 0**: `forceX` - Applies horizontal force.
- **Action 1**: `forceY` - Applies vertical force.
- **Action 2**: `torque` - Applies rotational force.

These values are mapped to `Rigidbody2D.AddForce()` and `Rigidbody2D.AddTorque()` to control the drone's movement.

## Related Docs

- [Reinforcement Learning Algorithm](./rl_algorithm.md)
- [Reward Signal Design](./reward_design.md)
