# Reinforcement Learning Algorithm

_Details on the Proximal Policy Optimization (PPO) algorithm, hyperparameters, and recurrent network configuration used for training._

The project employs Proximal Policy Optimization (PPO), a state-of-the-art reinforcement learning algorithm known for its stability and sample efficiency. It is combined with a recurrent neural network (RNN) to handle the partially observable nature of the task.

## Proximal Policy Optimization (PPO)

PPO is a policy gradient method that optimizes a "clipped" surrogate objective function. This approach constrains the policy update at each step, preventing destructively large updates and improving training stability.

The core objective function for the policy network is:

$$
L^{CLIP}(\theta) = \hat{\mathbb{E}}_t \left[ \min \left( r_t(\theta) \hat{A}_t, \text{clip}(r_t(\theta), 1 - \epsilon, 1 + \epsilon) \hat{A}_t \right) \right]
$$

where:

- $r_t(\theta) = \frac{\pi_\theta(a_t | s_t)}{\pi_{\theta_{old}}(a_t | s_t)}$ is the probability ratio of the new policy to the old policy.
- $\hat{A}_t$ is the estimated advantage function at timestep $t$.
- $\epsilon$ is the clipping hyperparameter, which defines the range `[1-ε, 1+ε]` for the probability ratio.

The final loss function combines the policy surrogate, a value function loss (for the critic), and an entropy bonus to encourage exploration.

$$
L_t(\theta) = L^{CLIP}(\theta) - c_1 L^{VF}_t(\theta) + c_2 S[\pi_\theta](s_t)
$$

- $L^{VF}_t$ is the squared-error loss for the value function: $(V_\theta(s_t) - V_t^{targ})^2$.
- $S[\pi_\theta](s_t)$ is the entropy bonus.

### Generalized Advantage Estimation (GAE)

To reduce the variance of the advantage function estimates, Generalized Advantage Estimation (GAE) is used. It computes an exponentially-weighted average of TD-errors for different time steps.

$$
\hat{A}_t = \sum_{l=0}^{\infty} (\gamma \lambda)^l \delta_{t+l}
$$

where $\delta_{t+l} = r_{t+l} + \gamma V(s_{t+l+1}) - V(s_{t+l})$ is the TD-error, and `λ` is the GAE hyperparameter.

## Hyperparameters

The following hyperparameters were used for the single-agent (S1) and multi-agent (S2) training runs, as defined in `config2D/single_occ.yaml` and `config2D/multi_gps.yaml`.

| Hyperparameter           | S1 (Single-Agent) Value | S2 (Multi-Agent) Value | Description                                                     |
| ------------------------ | ----------------------- | ---------------------- | --------------------------------------------------------------- |
| `trainer_type`           | `ppo`                   | `ppo`                  | The learning algorithm used.                                    |
| `max_steps`              | `10,005,000`            | `10,005,000`           | Total steps for the training session.                           |
| `time_horizon`           | `128`                   | `128`                  | Steps collected before adding to the experience buffer.         |
| `batch_size`             | `512`                   | `1024`                 | Minibatch size for each epoch of gradient ascent.               |
| `buffer_size`            | `5120`                  | `10240`                | Total experiences stored before sampling for updates.           |
| `learning_rate`          | `2.0e-4`                | `2.0e-4`               | Initial learning rate for the Adam optimizer.                   |
| `learning_rate_schedule` | `linear`                | `linear`               | How the learning rate changes over time.                        |
| `beta`                   | `0.01`                  | `0.01`                 | Strength of the entropy regularization term.                    |
| `epsilon`                | `0.2`                   | `0.2`                  | The clipping parameter for the PPO surrogate objective.         |
| `lambd` (GAE λ)          | `0.95`                  | `0.95`                 | The lambda parameter for Generalized Advantage Estimation.      |
| `num_epoch`              | `3`                     | `3`                    | Number of passes over the experience buffer during each update. |
| `gamma` (Extrinsic)      | `0.995`                 | `0.995`                | Discount factor for future extrinsic rewards.                   |
| `strength` (Curiosity)   | `0.05`                  | `0.05`                 | Contribution of the curiosity-driven intrinsic reward signal.   |
| `gamma` (Curiosity)      | `0.99`                  | `0.99`                 | Discount factor for the intrinsic reward signal.                |

## Recurrent Neural Network (LSTM)

To address the partial observability of the environment (e.g., remembering victim locations or explored areas), a Long Short-Term Memory (LSTM) unit is integrated into the network architecture.

- **Presence**: Confirmed in both `single_occ.yaml` and `multi_gps.yaml`.
- **Hidden Units (`memory_size`)**: `128`
- **Sequence Length (`sequence_length`)**: `64`

The LSTM processes a sequence of 64 consecutive observations, allowing the agent to maintain a memory of recent events and make decisions based on temporal context, which is crucial for efficient exploration and navigation.

## Related Docs

- [Network Architecture](./network_architecture.md)
- [Environment and Task Definition](./environment_and_task.md)
