# 🎓 3D Drone Curriculum Training Guide

## 📋 Overview

This curriculum teaches your drone in 8 progressive stages:
1. **Hover Training** - Learn to stay stable in air
2. **Close Target** - Reach nearby targets (5 units)
3. **Medium Target** - Reach medium distance (10 units)
4. **Static Obstacles** - Navigate around 3 obstacles
5. **Complex Obstacles** - Navigate around 6 obstacles
6. **Wind Challenge** - Fly with wind disturbance
7. **Long Distance** - Reach far targets (25 units)
8. **Expert Flight** - Master level with all challenges

---

## 🚀 Quick Start

### 1. Setup Unity Scene

```
Hierarchy:
├── Drone (with DroneAgent.cs, DroneController.cs)
├── Target (with visible mesh/sprite)
├── Environment (with DroneEnvironmentConfig.cs)
├── Ground
└── Arena Boundaries
```

### 2. Attach Scripts

- **Drone**: Add `DroneAgent.cs` and `DroneController.cs`
- **Environment Manager**: Create empty GameObject, add `DroneEnvironmentConfig.cs`
- Configure references in Inspector

### 3. Start Training

```bash
# From Unity_ML_Agent folder
mlagents-learn config/drone3d_curriculum.yaml --run-id=drone3d_v1 --train
```

Press Play in Unity when you see "Start training by pressing..."

---

## 📊 Curriculum Stages Breakdown

### Stage 1: Hover Training (0-500K steps)
**Goal**: Learn basic stability
- No target to reach
- No obstacles
- Short episodes (500 steps)
- Large success zone (2.0 units)

**Expected Behavior**:
- Drone learns to counteract gravity
- Maintains stable altitude
- Minimal drift

**Completion**: Average reward > 5.0

---

### Stage 2: Close Target (500K-1.5M steps)
**Goal**: Navigate to nearby target
- Target 5 units away
- No obstacles yet
- Medium episodes (1000 steps)
- Success radius 1.5 units

**Expected Behavior**:
- Drone flies toward green target
- Learns forward/backward movement
- Basic directional control

**Completion**: Average reward > 10.0

---

### Stage 3: Medium Target (1.5M-2.5M steps)
**Goal**: Reach farther targets
- Target 10 units away
- Still no obstacles
- Longer episodes (1500 steps)
- Tighter success zone (1.0 units)

**Expected Behavior**:
- Faster, more direct flight
- Better altitude control
- Smoother landing

**Completion**: Average reward > 15.0

---

### Stage 4: Static Obstacles (2.5M-4M steps)
**Goal**: Learn obstacle avoidance
- Target 10 units away
- 3 obstacles in path
- 2000 step episodes
- Same success radius (1.0)

**Expected Behavior**:
- Drone navigates around obstacles
- Plans path to avoid collisions
- May be hesitant at first

**Completion**: Average reward > 12.0

---

### Stage 5: Complex Obstacles (4M-5.5M steps)
**Goal**: Navigate crowded spaces
- Target 15 units away
- 6 obstacles
- 2500 step episodes

**Expected Behavior**:
- Confident maneuvering
- Efficient path planning
- Quick obstacle avoidance reactions

**Completion**: Average reward > 12.0

---

### Stage 6: Wind Challenge (5.5M-7M steps)
**Goal**: Fly with disturbances
- Target 15 units away
- 6 obstacles
- Wind force 2.0
- 3000 step episodes

**Expected Behavior**:
- Compensates for wind drift
- Maintains stable flight in turbulence
- Adjusts approach angles

**Completion**: Average reward > 10.0

---

### Stage 7: Long Distance (7M-8.5M steps)
**Goal**: Master long-range navigation
- Target 25 units away
- 8 obstacles
- Wind force 3.0
- 4000 step episodes
- Precise landing (0.8 radius)

**Expected Behavior**:
- Efficient long-range flight
- Smooth wind compensation
- Precise target approach

**Completion**: Average reward > 15.0

---

### Stage 8: Expert Flight (8.5M-10M steps)
**Goal**: Peak performance
- Target 30 units away
- 10 obstacles
- Strong wind (5.0)
- 5000 step episodes
- Very precise landing (0.5 radius)

**Expected Behavior**:
- Expert-level flight control
- Optimal path finding
- Consistent success rate

**Completion**: Average reward > 20.0

---

## 📈 Expected Training Timeline

| Stage | Steps | Real Time (16 envs) | Success Rate |
|-------|-------|---------------------|--------------|
| 1. Hover | 500K | ~30 min | 80%+ |
| 2. Close Target | 1M | ~1 hour | 70%+ |
| 3. Medium Target | 1M | ~1 hour | 65%+ |
| 4. Static Obstacles | 1.5M | ~1.5 hours | 60%+ |
| 5. Complex Obstacles | 1.5M | ~1.5 hours | 55%+ |
| 6. Wind Challenge | 1.5M | ~1.5 hours | 50%+ |
| 7. Long Distance | 1.5M | ~1.5 hours | 45%+ |
| 8. Expert Flight | 1.5M | ~1.5 hours | 40%+ |
| **Total** | **10M** | **~10 hours** | **Master** |

---

## 🎮 Testing Your Drone

### Manual Control (Heuristic Mode)

In DroneAgent.cs, the `Heuristic()` method lets you test manually:

```
W/S = Throttle (up/down)
A/D = Roll (left/right)
↑/↓ = Pitch (forward/backward)
Q/E = Yaw (rotate)
```

### Monitor Training Progress

```bash
# In new terminal
tensorboard --logdir results
```

Open http://localhost:6006 to see:
- Reward curves
- Episode length
- Success rate
- Curriculum progression

---

## 🔧 Troubleshooting

### Drone not learning to hover:
- Check `hoverForce = 9.81` in DroneController
- Verify Rigidbody has gravity enabled
- Increase reward for staying upright

### Stuck on a curriculum stage:
- Lower `threshold` in config
- Increase `min_lesson_length` for more practice
- Check if environment is too hard

### Drone spinning out of control:
- Reduce `torque` value (12 → 8)
- Increase `angularDrag` (3 → 5)
- Add penalty for excessive rotation

### Training too slow:
- Increase number of parallel environments (16 → 32)
- Reduce `time_horizon` (1000 → 500)
- Use GPU acceleration if available

---

## 📝 Reward Function Tips

### Good Reward Structure:

```csharp
// Distance to target (continuous shaping)
float distanceToTarget = Vector3.Distance(transform.position, target.position);
AddReward(-0.001f * distanceToTarget);

// Reached target (big bonus)
if (distanceToTarget < successRadius)
{
    AddReward(10.0f);
    EndEpisode();
}

// Time penalty (encourage efficiency)
AddReward(-0.001f);

// Upright bonus (encourage stability)
float uprightness = Vector3.Dot(transform.up, Vector3.up);
AddReward(0.001f * uprightness);

// Collision penalty
if (hitObstacle)
{
    AddReward(-5.0f);
    EndEpisode();
}
```

---

## 🎯 Next Steps

1. **Create 16 duplicate environments** for faster training
2. **Add Ray Perception Sensors** for obstacle detection
3. **Fine-tune hyperparameters** based on TensorBoard
4. **Export trained model** for deployment
5. **Test in real scenarios** (moving targets, dynamic obstacles)

---

## 📚 Additional Resources

- [ML-Agents Documentation](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Readme.md)
- [PPO Algorithm](https://openai.com/blog/openai-baselines-ppo/)
- [Curriculum Learning](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Training-Configuration-File.md#curriculum)

---

**Good luck training your drone! 🚁✨**
