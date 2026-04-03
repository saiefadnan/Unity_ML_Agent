# Test Metrics Logging Implementation

## Overview

CSV-based evaluation metrics logging for 2D single-agent and multi-agent drone RL controllers. Collects performance data during inference for thesis evaluation.

## Files Modified

1. `single_agent_scripts/Agent2D.cs`
2. `multi_agent_scripts/MultiAgent2D.cs`
3. `thesis-1/Assets/scripts/Agent2D.cs`

## Quick Start

### 1. Inspector Setup

- Select Agent2D or MultiAgent2D in scene
- Set `recordEvaluationMetrics = true`
- Set `testEpisodeLimit = 100`

### 2. Run Inference

```powershell
# Single-agent
mlagents-learn config2D/single_occ.yaml --run-id=test --inference --num-envs=1

# Multi-agent
mlagents-learn config2D/multi_gps.yaml --run-id=test --inference --num-envs=1
```

### 3. Get Results

Console output: `[Test Complete] Ran 100 episodes. Results saved to: ...`

CSV files:

- `Agent2D_Test_Results.csv`
- `MultiAgent2D_Test_Results.csv`

## CSV Format

**Single-Agent:**

```
Episode,VictimsRescued,TotalVictims,StepsTaken,PathEfficiency,DistanceTraveled,EndReason,DroneHP,ExploredCells
1,3,4,245,0.847,156.32,success,85.5,142
```

**Multi-Agent:**

```
Episode,AgentGoals,TotalVictims,StepsTaken,PathEfficiency,DistanceTraveled,EndReason,AgentHPs,ExploredCells
1,2,1,0,4,245,0.562,287.45,timeout,85.5,92.3,78.2,312
```

## Code Changes

### Fields Added

**Agent2D.cs:**

```csharp
[Header("Test Mode Metrics")]
public bool recordEvaluationMetrics = false;
public int testEpisodeLimit = 100;
private string logFilePath = "";
private int episodeCount = 0;
private int testEpisodesRun = 0;
```

**MultiAgent2D.cs:**

```csharp
[Header("Test Mode Metrics")]
public bool recordEvaluationMetrics = false;
public int testEpisodeLimit = 100;
private static string logFilePath = "";
private static int episodeCount = 0;
private static int testEpisodesRun = 0;
```

### Methods Added

**Initialize()** - Creates CSV header on first initialization

**LogTestRun()** - Logs episode data

- Agent2D: Logs individual metrics
- MultiAgent2D: Collects stats from all agents (Agent 0 only)

**OnEpisodeBegin()** - Calls LogTestRun(), increments counters, auto-stops at limit

## Data Columns

| Column                      | Description                                        |
| --------------------------- | -------------------------------------------------- |
| Episode                     | Episode number                                     |
| VictimsRescued / AgentGoals | Victims found (comma-separated for multi-agent)    |
| TotalVictims                | Victims in environment                             |
| StepsTaken                  | Actions performed                                  |
| PathEfficiency              | Optimal distance / Actual distance (0-1)           |
| DistanceTraveled            | Total distance traveled                            |
| EndReason                   | Episode termination reason                         |
| DroneHP                     | Drone health at episode end (0-100)                |
| ExploredCells               | Unique grid cells visited                          |
| AgentHPs                    | Per-agent health for multi-agent (comma-separated) |

## How It Works

**Single-Agent:**

1. OnEpisodeBegin → LogTestRun (logs previous episode)
2. Increment episode counter
3. Check if limit reached → auto-exit if yes
4. Reset for new episode

**Multi-Agent:**

1. All agents call OnEpisodeBegin
2. Only Agent 0 calls LogTestRun
3. Agent 0 collects stats from all agents
4. Agent 0 increments counters and checks limit

## Error Handling

Null safety checks prevent crashes on first episode:

```csharp
// Agent2D
if (goals == null || goals.Length == 0) return;

// MultiAgent2D
if (envManager == null || envManager.activeVictims == null) return;
```

## Usage for Thesis

1. Build both projects
2. Open scene with trained agent
3. Set `recordEvaluationMetrics = true` in Inspector
4. Run inference for 100 episodes (~3-5 minutes)
5. Collect CSV results
6. Repeat 3 times for each (single & multi) and average
7. Create thesis comparison table

## Technical Details

- CSV Location: Project root (`Application.dataPath/../filename.csv`)
- Logging Active: Only when `recordEvaluationMetrics = true`
- Limit Active: Only when `recordEvaluationMetrics = true`
- Multi-Agent: Only Agent 0 writes to CSV
- File Handling: Creates header if doesn't exist, appends rows
- Error Handling: Try-catch around file I/O

## Related Files

- Config: `config2D/single_occ.yaml`, `config2D/multi_gps.yaml`
- Docs: `docs/AGENT_IMPROVEMENTS.md`, `docs/DRONE3D_CURRICULUM_GUIDE.md`

---

**Status:** ✅ Complete  
**Date:** April 3, 2026
