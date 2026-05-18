# Project Overview: Autonomous Drone SAR

_A reinforcement learning project for training single and multi-agent drones for Search and Rescue (SAR) tasks in 2D environments._

This project uses Unity ML-Agents to train autonomous drones to navigate complex 2D environments, avoid obstacles, and locate victims. The core of the project is a Proximal Policy Optimization (PPO) algorithm coupled with a Long Short-Term Memory (LSTM) network, enabling the agents to learn sophisticated navigation and exploration strategies from high-dimensional sensor inputs. Two primary setups are explored: a single-agent system (S1) and a cooperative multi-agent system (S2) that uses a shared policy and GPS-based communication to enhance team-based exploration and coverage.

## System Diagram

```mermaid
graph TD
    subgraph Unity Environment
        A[2D Physics World]
        B(Drone Agent) -- Receives Actions --> A
        A -- Emits Observations --> B
        C{Victims}
        D{Obstacles}
    end

    subgraph ML-Agents Brain
        E[PPO Algorithm]
        F[Policy & Value Network]
        G[LSTM Memory]
        E -- Updates --> F
        F -- Uses --> G
    end

    B -- Sends Observations --> F
    F -- Returns Actions --> B

    subgraph Observations
        O1[Agent State (Velocity, Position)]
        O2[Raycast Sensor (LiDAR)]
        O3[Task Info (Victims, Progress)]
        O4[GPS Comms (Multi-Agent)]
    end

    subgraph Actions
        Ac1[Continuous: Force X, Y]
        Ac2[Continuous: Torque]
    end

    F -- Processes --> O1
    F -- Processes --> O2
    F -- Processes --> O3
    F -- Processes --> O4

    Ac1 -- Applied to --> B
    Ac2 -- Applied to --> B
```

## Repository Structure

The project is organized into the following key directories:

- `/config2D/`: Contains the `.yaml` configuration files for the ML-Agents trainers, defining hyperparameters for both single-agent (`single_occ.yaml`) and multi-agent (`multi_gps.yaml`) setups.
- `/contents/`: Stores media files like videos and images showcasing the agent's behavior.
- `/data/`: Holds raw output data, including test results in CSV format.
- `/docs/`: Contains all project documentation files (like this one).
- `/results/`: Stores the trained model files (`.onnx`) and TensorBoard logs from various training runs.
- `/scripts/`: Contains the C# source code for the Unity environment, split into `single_agent_scripts` and `multi_agent_scripts`.
- `/venv/`: Python virtual environment for ML-Agents.
- `feature.txt`, `guides.txt`, `README.md`: Root files containing high-level feature descriptions, development guides, and project readme.

## Related Docs

- [Environment and Task Definition](./environment_and_task.md)
- [Reinforcement Learning Algorithm](./rl_algorithm.md)
- [Reward Signal Design](./reward_design.md)
- [Network Architecture](./network_architecture.md)
- [Multi-Agent System](./multi_agent.md)
- [Results and Analysis](./results.md)
