using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;

public class MultiAgent2D : Agent
{
    [Header("Agent Configuration")]
    public bool isManualControl = false;
    public RayPerceptionSensorComponent2D raySensor;
    public AudioSource targetReached;
    public int maxStepCount = 2000;
    public MultiEnvManager envManager;

    [HideInInspector] public int agentIndex = 0;

    // Victim tracking
    private int targetIndex = -1;

    // Episode tracking
    Vector2 lastPos;
    public float distanceTraveled = 0f;   // made public for LogTestRun access
    public int goalsReached = 0;
    int groundCollision = 0;
    public int StepCnt = 0;               // made public for LogTestRun access
    public float shortestPath = 0f;       // ← ADDED

    // Physics
    Rigidbody2D rb;
    float softLandingThreshold = 1.5f;
    float hardLandingThreshold = 4f;

    // Reward shaping
    float previousDistance = Mathf.Infinity;
    private float closestDistanceEver = Mathf.Infinity;
    private int backtrackCounter = 0;

    // Exploration
    public HashSet<Vector2Int> personalVisitedCells = new HashSet<Vector2Int>(); // made public
    private int hoverCounter = 0;
    private Vector2 lastHoverCheck;
    private string endReason = "start";

    // Ground awareness
    private float groundDistance = 10f;
    private bool isNearGround = false;

    // Drone HP system
    public float droneHP = 100f;
    public const float MAX_HP = 100f;

    // Evaluation metrics logging
    [Header("Test Mode Metrics")]
    public bool recordEvaluationMetrics = false;
    public int testEpisodeLimit = 100;
    private static string logFilePath = "";
    private static int episodeCount = 0;
    private static bool logInitialized = false;
    private static int testEpisodesRun = 0;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();

        if (recordEvaluationMetrics && agentIndex == 0 && !logInitialized)
        {
            logFilePath = Path.Combine(Application.dataPath, "..", "MultiAgent2D_Test_Results.csv");
            if (!File.Exists(logFilePath))
            {
                try
                {
                    File.WriteAllText(logFilePath,
                        "Episode,TeamVictimsRescued,TotalVictims,StepsTaken," +
                        "EndReason," +
                        "Agent0Goals,Agent1Goals,Agent2Goals," +
                        "Agent0HP,Agent1HP,Agent2HP," +
                        "Agent0Efficiency,Agent1Efficiency,Agent2Efficiency," +
                        "TeamExploredCells\n");
                    Debug.Log($"[Test Mode] Multi-Agent logging to: {logFilePath}");
                }
                catch (System.Exception e) { Debug.LogError("CSV Init Error: " + e.Message); }
            }
            logInitialized = true;
        }
    }

    // ─── DISTANCE HELPERS ────────────────────────────────────────────
    float DistanceToClaimedTarget()
    {
        if (targetIndex < 0 || targetIndex >= envManager.activeVictims.Count) return 0f;
        var victim = envManager.activeVictims[targetIndex];
        if (victim == null || !victim.activeSelf) return 0f;
        return Vector2.Distance(victim.transform.position, transform.localPosition);
    }

    Vector2 DirectionToClaimedTarget()
    {
        if (targetIndex < 0 || targetIndex >= envManager.activeVictims.Count) return Vector2.zero;
        var victim = envManager.activeVictims[targetIndex];
        if (victim == null || !victim.activeSelf) return Vector2.zero;
        return (victim.transform.position - transform.localPosition).normalized;
    }

    // ─── SHORTEST PATH CALCULATION ───────────────────────────────────  ← ADDED
    float CalculateShortestPath()
    {
        if (envManager == null || envManager.activeVictims == null) return 0f;

        List<GameObject> remaining = new List<GameObject>();
        foreach (var v in envManager.activeVictims)
            if (v != null && v.activeSelf) remaining.Add(v);

        if (remaining.Count == 0) return 0f;

        float total = 0f;
        Vector2 current = transform.localPosition;

        while (remaining.Count > 0)
        {
            float nearest = Mathf.Infinity;
            GameObject nearestVic = null;

            foreach (var v in remaining)
            {
                if (v == null) continue;
                float d = Vector2.Distance(current, v.transform.localPosition);
                if (d < nearest) { nearest = d; nearestVic = v; }
            }

            if (nearestVic == null) break;
            total += nearest;
            current = nearestVic.transform.localPosition;
            remaining.Remove(nearestVic);
        }

        return total;
    }

    // ─── EPISODE LIFECYCLE ───────────────────────────────────────────
    private void EndEpisodeClean(string reason)
    {
        endReason = reason;
        if (envManager != null) envManager.ReleaseClaim(agentIndex);
        EndEpisode();
    }

    private void LogTestRun()
    {
        if (!recordEvaluationMetrics || agentIndex != 0 ||
            string.IsNullOrEmpty(logFilePath) || episodeCount == 0)
            return;

        try
        {
            if (envManager == null || envManager.activeVictims == null ||
                envManager.activeVictims.Count == 0)
                return;

            // Collect per-agent data sorted by agentIndex
            MultiAgent2D[] allAgents = FindObjectsOfType<MultiAgent2D>();
            System.Array.Sort(allAgents, (a, b) => a.agentIndex.CompareTo(b.agentIndex));

            string agentGoalsStr      = "";
            string agentHPsStr        = "";
            string agentEfficiencyStr = "";   // ← ADDED per-agent efficiency
            int    totalSteps         = 0;

            foreach (var agent in allAgents)
            {
                if (agent.envManager != envManager) continue;

                // Per-agent path efficiency                              ← ADDED
                float eff = agent.shortestPath > 0
                    ? agent.shortestPath / Mathf.Max(agent.distanceTraveled, 0.001f)
                    : 0f;

                agentGoalsStr      += agent.goalsReached + ",";
                agentHPsStr        += agent.droneHP.ToString("F1") + ",";
                agentEfficiencyStr += eff.ToString("F3") + ",";
                totalSteps          = Mathf.Max(totalSteps, agent.StepCnt);
            }

            agentGoalsStr      = agentGoalsStr.TrimEnd(',');
            agentHPsStr        = agentHPsStr.TrimEnd(',');
            agentEfficiencyStr = agentEfficiencyStr.TrimEnd(',');

            int teamRescued  = envManager.TotalVictimsFound();
            int totalVictims = envManager.victimCount;
            int teamExplored = envManager.teamExploredCells.Count;

            // Columns: Episode, TeamVictimsRescued, TotalVictims, StepsTaken,
            //          EndReason,
            //          Agent0Goals, Agent1Goals, Agent2Goals,
            //          Agent0HP, Agent1HP, Agent2HP,
            //          Agent0Efficiency, Agent1Efficiency, Agent2Efficiency,
            //          TeamExploredCells
            string logData =
                $"{episodeCount},"       +
                $"{teamRescued},"        +
                $"{totalVictims},"       +
                $"{totalSteps},"         +
                $"{endReason},"          +
                $"{agentGoalsStr},"      +
                $"{agentHPsStr},"        +
                $"{agentEfficiencyStr}," +
                $"{teamExplored}\n";

            File.AppendAllText(logFilePath, logData);
        }
        catch (System.Exception e)
        {
            Debug.LogError("CSV Log Error: " + e.Message);
        }
    }

    public override void OnEpisodeBegin()
    {
        if (envManager == null)
        {
            Debug.LogWarning($"Agent {agentIndex}: Environment Manager is not assigned.");
            return;
        }

        if (agentIndex == 0)
        {
            LogTestRun();
            episodeCount++;
            testEpisodesRun++;

            if (recordEvaluationMetrics && testEpisodesRun > testEpisodeLimit)
            {
                Debug.Log($"[Test Complete] Ran {testEpisodesRun - 1} episodes. Results saved to: {logFilePath}");
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
                return;
            }
        }

        Debug.Log($"Agent {agentIndex}: {endReason}");
        envManager.RequestReset();

        lastPos          = transform.localPosition;
        distanceTraveled = 0f;
        shortestPath     = 0f;   // ← ADDED reset
        StepCnt          = 0;
        groundCollision  = 0;
        goalsReached     = 0;
        droneHP          = MAX_HP;

        personalVisitedCells.Clear();
        hoverCounter         = 0;
        lastHoverCheck       = Vector2.zero;
        closestDistanceEver  = Mathf.Infinity;
        backtrackCounter     = 0;
        targetIndex          = -1;
    }

    public void PostEnvironmentReset()
    {
        droneHP          = MAX_HP;
        goalsReached     = 0;
        groundCollision  = 0;
        StepCnt          = 0;
        distanceTraveled = 0f;
        shortestPath     = 0f;   // ← ADDED reset
        personalVisitedCells.Clear();
        hoverCounter        = 0;
        closestDistanceEver = Mathf.Infinity;
        backtrackCounter    = 0;
        targetIndex         = -1;

        targetIndex = envManager.ClaimNearestAvailableVictim(agentIndex, transform.localPosition);
        if (targetIndex >= 0)
        {
            previousDistance    = DistanceToClaimedTarget();
            closestDistanceEver = previousDistance;
        }

        shortestPath   = CalculateShortestPath();  // ← ADDED calculate at episode start
        lastHoverCheck = transform.localPosition;
        lastPos        = transform.localPosition;
    }

    // ─── OBSERVATIONS ────────────────────────────────────────────────
    int GetObservationSize()
    {
        int baseObs     = 13;
        int teammateObs = (envManager.AgentCount - 1) * 5;
        return baseObs + teammateObs;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (envManager == null || envManager.isInitializing)
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);

            int teammateCount = envManager != null ? envManager.AgentCount - 1 : 2;
            for (int t = 0; t < teammateCount; t++)
            {
                sensor.AddObservation(Vector2.zero);
                sensor.AddObservation(Vector2.zero);
                sensor.AddObservation(0f);
            }
            return;
        }

        int activeTeammateCount = envManager.AgentCount - 1;

        sensor.AddObservation(rb.linearVelocity / 10f);
        sensor.AddObservation(rb.angularVelocity / 180f);
        sensor.AddObservation(transform.localPosition / 16f);

        float normalizedRotation = transform.eulerAngles.z;
        if (normalizedRotation > 180) normalizedRotation -= 360;
        sensor.AddObservation(normalizedRotation / 180f);

        int totalVictims = envManager.victimCount;
        int teamFound    = envManager.TotalVictimsFound();
        sensor.AddObservation(totalVictims > 0 ? teamFound / (float)totalVictims : 0f);

        if (targetIndex >= 0 && targetIndex < envManager.activeVictims.Count)
        {
            var victim = envManager.activeVictims[targetIndex];
            if (victim != null && victim.activeSelf)
            {
                Vector2 dir  = (victim.transform.position - transform.localPosition);
                float   dist = dir.magnitude;
                sensor.AddObservation(dir.normalized);
                sensor.AddObservation(Mathf.Clamp01(dist / 20f));
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        int remaining = 0;
        foreach (var v in envManager.activeVictims)
            if (v != null && v.activeSelf) remaining++;
        sensor.AddObservation(remaining / (float)Mathf.Max(totalVictims, 1));

        RaycastHit2D groundHit = Physics2D.Raycast(
            transform.position, Vector2.down, 10f, LayerMask.GetMask("Ground"));
        groundDistance = groundHit.collider != null ? groundHit.distance : 10f;
        isNearGround   = groundDistance < 2f;
        sensor.AddObservation(groundDistance / 10f);

        sensor.AddObservation(droneHP / MAX_HP);

        for (int t = 0; t < activeTeammateCount; t++)
        {
            var info = envManager.GetTeammateInfo(agentIndex, t);
            if (info.isActive)
            {
                Vector2 relPos = info.position - (Vector2)transform.localPosition;
                sensor.AddObservation(relPos / 32f);
                sensor.AddObservation(info.velocity / 10f);
                sensor.AddObservation(info.hp / MAX_HP);
            }
            else
            {
                sensor.AddObservation(Vector2.zero);
                sensor.AddObservation(Vector2.zero);
                sensor.AddObservation(0f);
            }
        }
    }

    // ─── ACTIONS ─────────────────────────────────────────────────────
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (envManager == null || envManager.isInitializing) return;

        StepCnt++;
        if (StepCnt > maxStepCount)
        {
            AddReward(-2f);
            EndEpisodeClean("timeout");
            return;
        }

        float forceX = actions.ContinuousActions[0];
        float forceY = actions.ContinuousActions[1];
        float torque = actions.ContinuousActions[2];

        float gravityCompensation = rb.mass * Mathf.Abs(Physics2D.gravity.y);
        rb.AddForce(Vector2.up * gravityCompensation, ForceMode2D.Force);
        rb.AddForce(new Vector2(forceX, forceY) * 10f, ForceMode2D.Force);
        rb.AddTorque(torque * 0.5f);

        if (isManualControl)
        {
            lastPos = transform.localPosition;
            return;
        }

        // ── VALIDATE CLAIM ───────────────────────────────────────────
        if (targetIndex >= 0)
        {
            var claimed = targetIndex < envManager.activeVictims.Count
                ? envManager.activeVictims[targetIndex] : null;
            if (claimed == null || !claimed.activeSelf ||
                !envManager.IsVictimClaimed(targetIndex, agentIndex))
            {
                envManager.ReleaseClaim(agentIndex);
                targetIndex = envManager.ClaimNearestAvailableVictim(agentIndex, transform.localPosition);
                if (targetIndex >= 0)
                {
                    previousDistance    = DistanceToClaimedTarget();
                    closestDistanceEver = previousDistance;
                    backtrackCounter    = 0;
                }
            }
        }
        else
        {
            targetIndex = envManager.ClaimNearestAvailableVictim(agentIndex, transform.localPosition);
            if (targetIndex >= 0)
            {
                previousDistance    = DistanceToClaimedTarget();
                closestDistanceEver = previousDistance;
                backtrackCounter    = 0;
            }
        }

        // ── HOVER TRAINING ───────────────────────────────────────────
        if (envManager.hoverMode)
        {
            float altitudeError = Mathf.Abs(transform.localPosition.y - envManager.hoverHeight);
            if      (altitudeError < 0.5f) AddReward(0.15f);
            else if (altitudeError < 1.0f) AddReward(0.05f);
            else if (altitudeError < 2.0f) AddReward(0.01f);
            else                           AddReward(-0.02f);
        }

        // ── STABILITY ────────────────────────────────────────────────
        float altitude = transform.localPosition.y;
        if      (altitude > 0f && altitude < 7f) AddReward(0.005f);
        else if (altitude <= -4f)                AddReward(-0.08f);

        float angleZ             = transform.eulerAngles.z;
        float normalizedZRotation = angleZ > 180 ? angleZ - 360 : angleZ;
        float uprightReward      = 1.0f - (Mathf.Abs(normalizedZRotation) / 180.0f);

        if (Mathf.Abs(normalizedZRotation) > 72f) AddReward(-0.2f);
        AddReward(Mathf.Clamp(uprightReward, 0f, 1f) * 0.005f);

        if (Mathf.Abs(normalizedZRotation) > 10f)
            AddReward(-0.005f * (Mathf.Abs(normalizedZRotation) / 180f));

        if (Mathf.Abs(rb.angularVelocity) > 100f) AddReward(-0.1f);
        if (rb.linearVelocity.y < -2.5f)          AddReward(-0.01f);

        if (isNearGround && rb.linearVelocity.y < -1.5f)
        {
            AddReward(-0.05f);
            if (rb.linearVelocity.y < -3f) AddReward(-0.2f);
        }
        if (isNearGround && rb.linearVelocity.y > -1.0f && rb.linearVelocity.y < 0.5f)
            AddReward(0.005f);
        if (transform.localPosition.y < -4.85f) AddReward(-0.01f);

        // ── OBSTACLE AWARENESS ───────────────────────────────────────
        bool obstacleInPath = false;
        if (!envManager.hoverMode && envManager.obstacles != null)
        {
            float closestObsDist = Mathf.Infinity;
            Vector2 agentPos  = transform.position;
            Vector2 targetDir = DirectionToClaimedTarget();
            float   targetDist = DistanceToClaimedTarget();

            foreach (Transform obs in envManager.obstacles)
            {
                if (obs == null || !obs.gameObject.activeSelf) continue;
                float dist = Vector2.Distance(agentPos, obs.position);
                if (dist < closestObsDist) closestObsDist = dist;

                if (targetDir != Vector2.zero && dist < targetDist)
                {
                    Vector2 obsDir = ((Vector2)obs.position - agentPos).normalized;
                    if (Vector2.Dot(targetDir, obsDir) > 0.7f) obstacleInPath = true;
                }
            }

            if (closestObsDist < 2f)
            {
                AddReward(-(2f - closestObsDist) / 2f * 0.05f);
                float speed = rb.linearVelocity.magnitude;
                if (speed > 3f) AddReward(-(speed - 3f) / 10f * 0.03f);
            }
        }

        // ── PROGRESS REWARD ──────────────────────────────────────────
        float currentDistance = DistanceToClaimedTarget();
        if (currentDistance > 0f)
        {
            float progressMultiplier = obstacleInPath ? 0.3f : 1.0f;

            if (currentDistance < closestDistanceEver - 0.05f)
            {
                float improvement = closestDistanceEver - currentDistance;
                AddReward(Mathf.Clamp(improvement * 3f, 0f, 0.08f) * progressMultiplier);
                closestDistanceEver = currentDistance;
                backtrackCounter    = 0;
            }
            else if (currentDistance > previousDistance + 0.1f)
            {
                if (obstacleInPath) backtrackCounter = Mathf.Max(0, backtrackCounter - 2);
                else
                {
                    AddReward(-0.015f);
                    backtrackCounter++;
                    if (backtrackCounter > 20) AddReward(-0.05f);
                }
            }
            else
            {
                backtrackCounter = Mathf.Max(0, backtrackCounter - 1);
            }

            previousDistance = currentDistance;
        }

        // ── TEAM EXPLORATION ─────────────────────────────────────────
        Vector2Int currentCell = new Vector2Int(
            Mathf.RoundToInt(transform.localPosition.x / MultiEnvManager.GRID_CELL_SIZE),
            Mathf.RoundToInt(transform.localPosition.y / MultiEnvManager.GRID_CELL_SIZE)
        );

        if (!personalVisitedCells.Contains(currentCell))
        {
            personalVisitedCells.Add(currentCell);
            AddReward(0.03f / (1f + personalVisitedCells.Count * 0.05f));
        }

        if (envManager.RegisterExploredCell(currentCell))
            AddReward(0.02f);

        // ── TEAMMATE SEPARATION ──────────────────────────────────────
        for (int t = 0; t < envManager.AgentCount - 1; t++)
        {
            var info = envManager.GetTeammateInfo(agentIndex, t);
            if (info.isActive)
            {
                float teammateDist = Vector2.Distance(transform.localPosition, info.position);
                if (teammateDist < 3f)
                    AddReward(-0.005f * (3f - teammateDist) / 3f);
            }
        }

        // ── ANTI-HOVERING ────────────────────────────────────────────
        if (!envManager.hoverMode && StepCnt % 10 == 0)
        {
            float movement = Vector2.Distance(transform.localPosition, lastHoverCheck);
            if (movement < 0.1f)
            {
                hoverCounter++;
                if (hoverCounter > 3) AddReward(-0.1f);
                if (hoverCounter > 8)
                {
                    AddReward(-1f);
                    EndEpisodeClean("idle_hovering");
                    return;
                }
            }
            else hoverCounter = 0;
            lastHoverCheck = transform.localPosition;
        }

        // ── MOVEMENT TRACKING ────────────────────────────────────────
        float newDist = Vector2.Distance(transform.localPosition, lastPos);
        if (newDist < 0.001f) AddReward(-0.01f);
        distanceTraveled += newDist;
        lastPos = transform.localPosition;

        AddReward(-0.003f);

        // ── STATS ────────────────────────────────────────────────────
        var recorder = Academy.Instance.StatsRecorder;
        recorder.Add($"Agent{agentIndex}/Stability",    uprightReward);
        recorder.Add($"Agent{agentIndex}/HP",           droneHP);
        recorder.Add($"Agent{agentIndex}/VictimsFound", goalsReached);
        recorder.Add("Team/ExploredCells",  envManager.teamExploredCells.Count);
        recorder.Add("Team/TotalVictims",   envManager.TotalVictimsFound());
    }

    // ─── HEURISTIC ───────────────────────────────────────────────────
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var act = actionsOut.ContinuousActions;
        float horiz = 0f, vert = 0f, torq = 0f;

        if (Keyboard.current.aKey.isPressed)      horiz = -1f;
        else if (Keyboard.current.dKey.isPressed) horiz =  1f;
        if (Keyboard.current.wKey.isPressed)      vert  =  1f;
        else if (Keyboard.current.sKey.isPressed) vert  = -1f;
        if (Keyboard.current.qKey.isPressed)      torq  =  1f;
        else if (Keyboard.current.eKey.isPressed) torq  = -1f;

        act[0] = horiz;
        act[1] = vert;
        act[2] = torq;
    }

    // ─── COLLISIONS ──────────────────────────────────────────────────
    void Update()
    {
        if (envManager == null) return;
        if (transform.localPosition.y > 8f ||
            transform.localPosition.x < -16f || transform.localPosition.x > 16f)
        {
            AddReward(-10f);
            EndEpisodeClean("out_of_bounds");
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (envManager == null) return;

        if (other.gameObject.CompareTag("Ground"))
        {
            groundCollision++;
            float impactSpeed = other.relativeVelocity.magnitude;

            if (envManager.hoverMode)
            {
                AddReward(-3f);
                EndEpisodeClean("ground_contact_hover");
                return;
            }

            if (impactSpeed >= hardLandingThreshold)
            {
                float damage = impactSpeed * impactSpeed * 1.0f;
                droneHP -= damage;
                AddReward(-damage / MAX_HP * 2f);
                if (droneHP <= 0f) { AddReward(-2f); EndEpisodeClean("hard_crash_destroyed"); return; }
            }
            else if (impactSpeed < 1f)                  AddReward(0.5f);
            else if (impactSpeed < softLandingThreshold) AddReward(0.2f);
            else
            {
                float damage = impactSpeed * 2f;
                droneHP -= damage;
                AddReward(-0.5f);
                if (droneHP <= 0f) { AddReward(-1f); EndEpisodeClean("rough_landing_destroyed"); return; }
            }
        }

        if (other.gameObject.CompareTag("Obstacle"))
        {
            float impactSpeed = other.relativeVelocity.magnitude;
            float damage      = impactSpeed * impactSpeed * 1.5f;
            droneHP -= damage;
            AddReward(-damage / MAX_HP * 3f);
            if (droneHP <= 0f) { AddReward(-2f); EndEpisodeClean("drone_destroyed"); return; }
        }
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if (envManager == null) return;
        if (other.gameObject.CompareTag("Obstacle"))
        {
            droneHP -= 0.5f;
            AddReward(-0.02f);
            if (droneHP <= 0f) { AddReward(-2f); EndEpisodeClean("grinding_obstacle_destroyed"); return; }
        }
    }

    // ─── VICTIM COLLECTION ───────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (envManager == null) return;

        if (other.CompareTag("Victim"))
        {
            goalsReached++;

            float baseReward      = 5.0f;
            float timeBonus       = Mathf.Max(0f, (maxStepCount - StepCnt) / (float)maxStepCount) * 1f;
            float difficultyBonus = envManager.obstacleCount > 0 ? 2.0f : 0f;

            int  victimIndex = envManager.activeVictims.IndexOf(other.gameObject);
            bool wasClaimed  = victimIndex >= 0 && envManager.IsVictimClaimed(victimIndex, agentIndex);

            AddReward(wasClaimed
                ? baseReward + timeBonus + difficultyBonus
                : baseReward * 0.8f + timeBonus + difficultyBonus);

            targetReached?.Play();
            other.gameObject.SetActive(false);

            envManager.ReleaseClaim(agentIndex);
            targetIndex = envManager.ClaimNearestAvailableVictim(agentIndex, transform.localPosition);
            if (targetIndex >= 0)
            {
                previousDistance    = DistanceToClaimedTarget();
                closestDistanceEver = previousDistance;
                backtrackCounter    = 0;
            }

            if (envManager.AllVictimsFound())
            {
                AddReward(10f);
                var recorder = Academy.Instance.StatsRecorder;
                recorder.Add("Team/CompletionTime",           StepCnt);
                recorder.Add($"Agent{agentIndex}/PersonalFinds", goalsReached);
                endReason = "team_completion";
                EndEpisode();
            }
        }
    }

    // ─── DEBUG GUI ───────────────────────────────────────────────────
    void OnGUI()
    {
        if (agentIndex > 0 || envManager == null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(15, 25, 400, 30),
            $"Team Victims: {envManager.TotalVictimsFound()}/{envManager.victimCount}", style);
        GUI.Label(new Rect(15, 50, 400, 30),
            $"Team Explored: {envManager.teamExploredCells.Count}", style);

        for (int i = 0; i < envManager.AgentCount; i++)
        {
            var info = i == agentIndex
                ? new MultiEnvManager.TeammateInfo { hp = droneHP, isActive = true }
                : envManager.GetTeammateInfo(agentIndex, i > agentIndex ? i - 1 : i);

            Color c = info.hp > 50 ? Color.green : info.hp > 20 ? Color.yellow : Color.red;
            style.normal.textColor = c;
            GUI.Label(new Rect(15, 80 + i * 25, 400, 30),
                $"Drone {i}: HP {(i == agentIndex ? droneHP : info.hp):F0} | Found: {(i == agentIndex ? goalsReached : info.goalsReached)}", style);
        }
    }
}