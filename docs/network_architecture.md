# Network Architecture

_An overview of the neural network structure, including layer sizes, activations, and the integration of the LSTM module._

The agent's decision-making is powered by a deep neural network that functions as both the policy (actor) and value (critic) function. The architecture is defined in the `network_settings` section of the `.yaml` configuration files.

## Policy and Value Network

The network is a standard feed-forward multi-layer perceptron (MLP) with the following specifications from `config2D/single_occ.yaml` and `config2D/multi_gps.yaml`:

- **Number of Layers (`num_layers`)**: 2
- **Hidden Units per Layer (`hidden_units`)**: 256
- **Activation Function**: ReLU (default for ML-Agents)
- **Input Normalization (`normalize`)**: `true`. The input observations are normalized to have a mean of 0 and a variance of 1. This stabilizes the training process.

The network takes the observation vector as input and splits into two heads:

1.  **Policy Head**: Outputs the parameters for the action distribution (mean and standard deviation for the continuous actions).
2.  **Value Head**: Outputs a single scalar value, representing the predicted return (cumulative discounted reward) from the current state.

## LSTM Integration

An LSTM is integrated to provide the agent with memory.

- **Placement**: The LSTM processes the encoded observation vector before it is passed to the policy and value heads.
- **Memory Size (`memory_size`)**: 128. This is the size of the hidden state and cell state vectors within the LSTM.
- **Sequence Length (`sequence_length`)**: 64. The network is unrolled for 64 steps, and gradients are back-propagated through this sequence.

## Input/Output Tensor Shapes

The shape of the input tensor depends on the observation space of the specific setup.

- **Single-Agent (S1) Input Shape**:
  - Vector Observation: `(batch_size, 13)`
  - Raycast Observations are processed separately and concatenated.
  - The total input dimension is 13 + (number of raycast observations).

- **Multi-Agent (S2) Input Shape**:
  - Vector Observation: `(batch_size, 23)`
  - The total input dimension is 23 + (number of raycast observations).

- **Output Shapes**:
  - **Policy (Actor)**: `(batch_size, 6)` — 3 for the mean and 3 for the log standard deviation of the continuous actions.
  - **Value (Critic)**: `(batch_size, 1)` — A single value estimate per state.

## Network Forward Pass Diagram

This diagram illustrates the flow of data through the network architecture.

```mermaid
graph TD
    subgraph Inputs
        A[Vector Observations]
        B[Raycast Observations]
    end

    subgraph Preprocessing
        C[Input Normalization]
    end

    subgraph Encoding
        D[Encoder MLP]
        E[LSTM]
    end

    subgraph Heads
        F[Policy Head (Actor)]
        G[Value Head (Critic)]
    end

    subgraph Outputs
        H[Action Distribution (Mean, StdDev)]
        I[State-Value Estimate]
    end

    A --> C
    B --> C
    C --> D
    D -- Encoded State --> E
    E -- Memory-Enhanced State --> F
    E -- Memory-Enhanced State --> G
    F --> H
    G --> I
```

## Related Docs

- [Reinforcement Learning Algorithm](./rl_algorithm.md)
