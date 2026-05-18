# Autonomous Drone for Search and Rescue using Deep Reinforcement Learning

_A Unity ML-Agents project focused on training autonomous drones for Search and Rescue (SAR) tasks in complex 2D environments. This repository contains the source code, trained models, and complete documentation for both single-agent and cooperative multi-agent systems._

![Multi-Agent System in Action](./contents/multi2D.png)

---

## Key Features

- **Advanced RL Algorithm**: Utilizes Proximal Policy Optimization (PPO) with a Long Short-Term Memory (LSTM) network to enable sophisticated, memory-driven navigation.
- **Two Distinct Systems**:
  - **Single-Agent (S1)**: A baseline system with one drone navigating the environment.
  - **Multi-Agent (S2)**: A cooperative team of three drones using a shared policy and GPS-based communication to enhance exploration and efficiency.
- **Complex Reward Shaping**: A dense reward function guides the agent to learn safe and efficient flight, with specific incentives for progress, stability, and exploration, and penalties for crashes or inefficiency.
- **Curriculum Learning**: Agents are trained through a multi-stage curriculum that gradually increases task difficulty, from basic hovering to navigating cluttered environments.
- **Comprehensive Documentation**: Includes detailed documentation on the environment, RL algorithm, network architecture, reward design, and performance results.

## Technology Stack

- **Engine**: Unity 2022.3.15f1
- **RL Framework**: Unity ML-Agents Release 20
- **Programming Languages**: C# (for Unity environment/agent logic) and Python (for training)
- **Libraries**: PyTorch, NumPy, Pandas

## Getting Started

### Prerequisites

- Unity Editor (2022.3.15f1 or later)
- Python 3.8+
- Unity ML-Agents package installed in Unity.
- `ml-agents` Python package installed.

```bash
# Clone the repository
git clone <repository-url>
cd Unity_ML_Agent

# Set up Python virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
pip install -r requirements.txt # Assuming a requirements.txt exists
```

### Running Inference

To see the trained agents in action, use the `mlagents-learn` command with the `--inference` flag.

```powershell
# Run the Single-Agent (S1) model
mlagents-learn config2D/single_occ.yaml --run-id=drone6.1 --inference

# Run the Multi-Agent (S2) model
mlagents-learn config2D/multi_gps.yaml --run-id=drone7.2 --inference
```

### Training a New Model

To start a new training run from scratch:

```powershell
# Train a new single-agent model
mlagents-learn config2D/single_occ.yaml --run-id=NewSingleAgentRun

# Train a new multi-agent model
mlagents-learn config2D/multi_gps.yaml --run-id=NewMultiAgentRun
```

Training progress can be monitored via TensorBoard: `tensorboard --logdir results`.

## Repository Structure

```
/
├── 📄 README.md           # You are here
├── 📂 config2D/            # Trainer configuration (.yaml) files for PPO
├── 📂 contents/            # Demo videos and images
├── 📂 data/                # Raw test evaluation data (.csv)
├── 📂 docs/                # Detailed technical documentation
├── 📂 results/             # Trained models (.onnx) and TensorBoard logs
└── 📂 scripts/             # C# source code for the Unity environment
```

## Detailed Documentation

For a deep dive into the project's technical implementation, including environment design, algorithm hyperparameters, reward functions, and performance analysis, please refer to the complete documentation suite located in the `/docs` directory.

**[➡️ View Full Technical Documentation](./docs/overview.md)**
