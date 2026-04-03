using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class Agent2D : Agent
{
    [Header("Agent Configuration")]
    public bool isManualControl = false;
    public RayPerceptionSensorComponent2D raySensor;
    public AudioSource targetReached;
    public AudioSource droneHum;
    public int maxStepCount = 2000;
    public EnvManager envManager;

    private Transform[] goals;

    // Episode tracking
    Vector2 lastPos;
    float shortestPath = 0f;
    float distanceTraveled = 0f;
    int goalsReached = 0;
    int groundCollision = 0;
    int targetIndex = 0;
    int StepCnt = 0;

    // Physics
    Rigidbody2D rb;
    float deltaX = 0.0f;
    float targetY = 0.0f;
    float softLandingThreshold = 1.5f;
    float hardLandingThreshold = 4f;

    // Reward shaping
    float previousDistance = Mathf.Infinity;
    private float closestDistanceEver = Mathf.Infinity; // Anti-oscillation
    private int backtrackCounter = 0;

    // Exploration system
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private int hoverCounter = 0;
    private Vector2 lastHoverCheck;
    private const float GRID_CELL_SIZE = 2.5f;
    private string endReason = "start";

    // Landing assistance
    private float groundDistance = 10f;
    private bool isNearGround = false;

    // Drone HP system
    private float droneHP = 100f;
    private const float MAX_HP = 100f;

    // Evaluation metrics logging
    [Header("Test Mode Metrics")]
    public bool recordEvaluationMetrics = false;
    public int testEpisodeLimit = 100;
    private string logFilePath = "";
    private int episodeCount = 0;
    private int testEpisodesRun = 0;
    private float episodeStartTime = 0f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();

        // Initialize test logging
        if (recordEvaluationMetrics)
        {
            logFilePath = System.IO.Path.Combine(Application.dataPath, "..", "Agent2D_Test_Results.csv");
            if (!System.IO.File.Exists(logFilePath))
            {
                try
                {
                    System.IO.File.WriteAllText(logFilePath, 
                        "Episode,VictimsRescued,TotalVictims,StepsTaken,PathEfficiency,DistanceTraveled,EndReason,DroneHP,ExploredCells\n");
                }
                catch (System.Exception e) { Debug.LogError("CSV Init Error: " + e.Message); }
            }
            Debug.Log($"[Test Mode] Single Agent logging to: {logFilePath}");
        }
    }


    float DistanceToTarget()
    {
        if (goals == null || goals.Length == 0 || targetIndex < 0 || targetIndex >= goals.Length)
            return 0f;
        if (!goals[targetIndex].gameObject.activeSelf) return 0f;
        return Vector2.Distance(goals[targetIndex].localPosition, transform.localPosition);
    }

    void calculateShortestPath()
    {
        shortestPath += deltaX;
        int k = targetIndex;
        Dictionary<int, bool> visited = new Dictionary<int, bool>();
        visited[k] = true;

        while (visited.Count < goals.Length)
        {
            float nearestDist = Mathf.Infinity;
            int nearestIndex = -1;

            for (int i = 0; i < goals.Length; i++)
            {
                if (!visited.ContainsKey(i) && i != k)
                {
                    float dist = Vector2.Distance(goals[i].localPosition, goals[k].localPosition);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestIndex = i;
                    }
                }
            }

            if (nearestIndex != -1)
            {
                shortestPath += nearestDist;
                visited[nearestIndex] = true;
                k = nearestIndex;
            }
        }
    }

    float GetNearestDistance(Vector3 source)
    {
        float newDeltaX = Mathf.Infinity;
        foreach (Transform goal in goals)
        {
            float dist = Vector2.Distance(goal.localPosition, source);
            if (goal.gameObject.activeSelf && dist < newDeltaX)
            {
                newDeltaX = dist;
                targetIndex = System.Array.IndexOf(goals, goal);
            }
        }
        return newDeltaX;
    }

    private void LogTestRun()
    {
        if (!recordEvaluationMetrics || string.IsNullOrEmpty(logFilePath) || episodeCount == 0)
            return;

        try
        {
            // Skip logging if goals not yet initialized (first episode)
            if (goals == null || goals.Length == 0)
                return;

            // Calculate path efficiency (ideal / actual)
            float efficiency = shortestPath > 0.001f ? shortestPath / Mathf.Max(distanceTraveled, 0.001f) : 0f;
            
            string logData = $"{episodeCount},{goalsReached},{goals.Length},{StepCnt},{efficiency:F3},{distanceTraveled:F3},{endReason},{droneHP:F1},{visitedCells.Count}\n";
            System.IO.File.AppendAllText(logFilePath, logData);
        }
        catch (System.Exception e) { Debug.LogError("CSV Log Error: " + e.Message); }
    }

    public override void OnEpisodeBegin()
    {
        // Log previous episode metrics if in test mode
        LogTestRun();
        
        // Increment episode counter at start of new episode
        episodeCount++;
        testEpisodesRun++;
        episodeStartTime = Time.time;

        // Stop after N episodes if in test mode
        if (recordEvaluationMetrics && testEpisodesRun > testEpisodeLimit)
        {
            Debug.Log($"[Test Complete] Ran {testEpisodesRun-1} episodes. Results saved to: {logFilePath}");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                UnityEngine.Application.Quit();
            #endif
            return;
        }

        Debug.Log(endReason);
        envManager.ResetEnvironment();

        // Reset tracking variables
        targetY = transform.localPosition.y;
        shortestPath = 0f;
        lastPos = transform.localPosition;
        distanceTraveled = 0f;
        StepCnt = 0;
        groundCollision = 0;

        // Reset exploration systems
        visitedCells.Clear();
        hoverCounter = 0;
        lastHoverCheck = Vector2.zero;
        closestDistanceEver = Mathf.Infinity;
        backtrackCounter = 0;
        goalsReached = 0;
        droneHP = MAX_HP;

    }

    public void PostEnvironmentReset()
    {
        goals = envManager.activeVictims
            .ConvertAll(v => v.transform)
            .ToArray();

        deltaX = GetNearestDistance(transform.localPosition);
        calculateShortestPath();
        previousDistance = DistanceToTarget();
        closestDistanceEver = previousDistance;
        lastHoverCheck = transform.localPosition;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (envManager.isInitializing)
        {
            // MUST send the same number of observations every call (12 total)
            sensor.AddObservation(Vector2.zero);   // velocity (2)
            sensor.AddObservation(0f);             // angular velocity (1)
            sensor.AddObservation(Vector2.zero);   // position (2)
            sensor.AddObservation(0f);             // rotation (1)
            sensor.AddObservation(0f);             // mission progress (1)
            sensor.AddObservation(0f);             // direction x (1)
            sensor.AddObservation(0f);             // direction y (1)
            sensor.AddObservation(0f);             // distance (1)
            sensor.AddObservation(0f);             // remaining victims (1)
            sensor.AddObservation(0f);             // ground distance (1)
            sensor.AddObservation(0f);             // closest distance ever (1)
            return;
        }

        // Agent state (6 values)
        sensor.AddObservation(rb.linearVelocity / 10f); // 2 values (includes vertical velocity!)
        sensor.AddObservation(rb.angularVelocity / 180f); // 1 value
        sensor.AddObservation(transform.localPosition / 16f); // 2 values

        // Agent orientation
        float normalizedRotation = transform.eulerAngles.z;
        if (normalizedRotation > 180) normalizedRotation -= 360;
        sensor.AddObservation(normalizedRotation / 180f); // 1 value

        // Task context for LSTM (1 value)
        float missionProgress = (goals != null && goals.Length > 0) ? goalsReached / (float)goals.Length : 0f;
        sensor.AddObservation(missionProgress);

        // Direction and distance to nearest victim (3 values) - helps navigate around obstacles
        if (goals != null && goals.Length > 0 && targetIndex >= 0 && targetIndex < goals.Length
            && goals[targetIndex].gameObject.activeSelf)
        {
            Vector2 dirToTarget = (goals[targetIndex].localPosition - transform.localPosition);
            float distToTarget = dirToTarget.magnitude;
            sensor.AddObservation(dirToTarget.normalized); // 2 values: direction x, y
            sensor.AddObservation(Mathf.Clamp01(distToTarget / 20f)); // 1 value: normalized distance
        }
        else
        {
            sensor.AddObservation(0f); // no direction x
            sensor.AddObservation(0f); // no direction y
            sensor.AddObservation(0f); // no distance
        }

        // Remaining victims count (1 value)
        int remainingVictims = 0;
        if (goals != null)
        {
            foreach (var g in goals)
                if (g != null && g.gameObject.activeSelf) remainingVictims++;
        }
        sensor.AddObservation(remainingVictims / 5f); // Normalized by max victims

        // CRITICAL: Ground proximity awareness (1 value)
        RaycastHit2D groundHit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            10f,
            LayerMask.GetMask("Ground")
        );
        groundDistance = groundHit.collider != null ? groundHit.distance : 10f;
        isNearGround = groundDistance < 2f;
        sensor.AddObservation(groundDistance / 10f); // Normalized distance to ground below
    }

    void Update()
    {
        // Check if agent fell off bounds
        if (transform.localPosition.y > 8f ||
            transform.localPosition.x < -16f || transform.localPosition.x > 16f)
        {
            AddReward(-10f);
            endReason = "out_of_bounds";
            EndEpisode();
        }
        // Draw ray sensor debug visualization
        if (raySensor != null)
        {
            DrawRaySensorDebug();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (envManager.isInitializing) return; // Skip actions during initialization
        // Debug.Log($"Step: {StepCnt}, Action: [{actions.ContinuousActions[0]:F2}, {actions.ContinuousActions[1]:F2}, {actions.ContinuousActions[2]:F2}]");
        StepCnt++;
        if (StepCnt > maxStepCount)
        {
            AddReward(-2f); // Penalty for timeout
            endReason = "timeout";
            EndEpisode();
            return;
        }

        // Penalty for drifting away from center (x=0)
        float centerPenalty = Mathf.Abs(transform.localPosition.x) / 16f; // Normalize to scene bounds
        AddReward(-centerPenalty * 0.01f); // Small penalty for drifting from center
        // Apply drone control forces
        float forceX = actions.ContinuousActions[0];
        float forceY = actions.ContinuousActions[1];
        float torque = actions.ContinuousActions[2];

        // CRITICAL: Auto-compensate gravity so agent starts from hover baseline
        float gravityCompensation = rb.mass * Mathf.Abs(Physics2D.gravity.y);
        rb.AddForce(Vector2.up * gravityCompensation, ForceMode2D.Force);

        rb.AddForce(new Vector2(forceX, forceY) * 10f, ForceMode2D.Force);
        rb.AddTorque(torque * 0.5f);

        if (isManualControl)
        {
            previousDistance = DistanceToTarget();
            lastPos = transform.localPosition;
            return;
        }

        // ========== HOVER REWARD SHAPING (for hover_training curriculum) ========== 
        if (envManager.hoverMode)
        {
            float altitudeError = Mathf.Abs(transform.localPosition.y - envManager.hoverHeight);
            if (altitudeError < 0.5f) AddReward(0.15f);      // Big reward for precise hover
            else if (altitudeError < 1.0f) AddReward(0.05f);  // Medium reward for close hover
            else if (altitudeError < 2.0f) AddReward(0.01f);  // Small reward for rough hover
            else AddReward(-0.02f);                            // Penalty for far from hover height
        }

        // Reward for being airborne at good altitude (all stages)
        // Keep small - must not outweigh navigation incentive
        float altitude = transform.localPosition.y;
        if (altitude > 0f && altitude < 7f)
        {
            AddReward(0.005f); // Tiny reward - just enough to prefer airborne over ground
        }
        else if (altitude <= -4f)
        {
            AddReward(-0.08f); // Strong penalty for being very near ground
        }
        float angleZ = transform.eulerAngles.z;
        float normalizedZRotation = angleZ > 180 ? angleZ - 360 : angleZ;
        float uprightReward = 1.0f - (Mathf.Abs(normalizedZRotation) / 180.0f);

        // Stronger penalty for unstable angle
        if (Mathf.Abs(normalizedZRotation) > 72f)
        {
            AddReward(-0.2f); // Much stronger penalty for unstable angle
        }
        // Reward for being upright (continuous, capped)
        AddReward(Mathf.Clamp(uprightReward, 0f, 1f) * 0.005f); // Tiny - prevent idle farming

        // Continuous tilt penalty - discourages persistent lean in any direction
        if (Mathf.Abs(normalizedZRotation) > 10f)
        {
            float tiltSeverity = Mathf.Abs(normalizedZRotation) / 180f; // 0..1
            AddReward(-0.005f * tiltSeverity); // Gentle but continuous
        }

        // Penalty for high angular velocity (spinning)
        if (Mathf.Abs(rb.angularVelocity) > 100f)
        {
            AddReward(-0.1f); // Penalty for spinning
        }

        //to reduce vertical velocity
        if (rb.linearVelocity.y < -2.5f)
        {
            AddReward(-0.01f);
        }

        // CRITICAL: Prevent hard crashes - penalize high downward speed near ground
        if (isNearGround && rb.linearVelocity.y < -1.5f)
        {
            AddReward(-0.05f); // Strong penalty for fast descent near ground
            if (rb.linearVelocity.y < -3f)
            {
                AddReward(-0.2f); // Extreme penalty - crash imminent!
            }
        }

        // Reward controlled descent near ground
        if (isNearGround && rb.linearVelocity.y > -1.0f && rb.linearVelocity.y < 0.5f)
        {
            AddReward(0.005f); // Small reward for safe descent speed
        }

        // too close to ground
        if (transform.localPosition.y < -4.85f)
        {
            AddReward(-0.01f);
        }

        // ========== OBSTACLE AWARENESS SYSTEM ==========
        // Proximity penalty + detect if obstacle is between agent and target
        bool obstacleInPath = false;
        if (!envManager.hoverMode && envManager.obstacles != null)
        {
            float closestObsDist = Mathf.Infinity;
            Vector2 agentPos = transform.position;
            Vector2 targetDir = Vector2.zero;
            float targetDist = 0f;

            // Direction to current target (nearest victim)
            if (envManager.activeVictims != null && envManager.activeVictims.Count > 0)
            {
                float nearestVictDist = Mathf.Infinity;
                Vector2 nearestVictPos = agentPos;
                foreach (GameObject v in envManager.activeVictims)
                {
                    if (v != null && v.activeSelf)
                    {
                        float d = Vector2.Distance(agentPos, v.transform.position);
                        if (d < nearestVictDist) { nearestVictDist = d; nearestVictPos = v.transform.position; }
                    }
                }
                targetDir = (nearestVictPos - agentPos).normalized;
                targetDist = nearestVictDist;
            }

            foreach (Transform obs in envManager.obstacles)
            {
                if (obs != null && obs.gameObject.activeSelf)
                {
                    float dist = Vector2.Distance(agentPos, obs.position);
                    if (dist < closestObsDist) closestObsDist = dist;

                    // Check if this obstacle is roughly between agent and target
                    if (targetDir != Vector2.zero && dist < targetDist)
                    {
                        Vector2 obsDir = ((Vector2)obs.position - agentPos).normalized;
                        float dot = Vector2.Dot(targetDir, obsDir);
                        if (dot > 0.7f) // Obstacle within ~45° cone toward target
                        {
                            obstacleInPath = true;
                        }
                    }
                }
            }

            // Graduated proximity penalty
            if (closestObsDist < 2f)
            {
                float proximityPenalty = (2f - closestObsDist) / 2f * 0.05f;
                AddReward(-proximityPenalty);

                // Penalize high speed near obstacles — teaches braking before impact
                float speed = rb.linearVelocity.magnitude;
                if (speed > 3f)
                {
                    float speedPenalty = (speed - 3f) / 10f * 0.03f; // Scales with excess speed
                    AddReward(-speedPenalty);
                }
            }
        }

        // ========== OBSTACLE-AWARE PROGRESS REWARD ==========
        float currentDistance = DistanceToTarget();

        if (currentDistance > 0f)
        {
            // Gate progress reward when obstacle is blocking the direct path
            float progressMultiplier = obstacleInPath ? 0.3f : 1.0f;

            // Only reward when beating personal record distance
            if (currentDistance < closestDistanceEver - 0.05f)
            {
                float improvement = closestDistanceEver - currentDistance;
                AddReward(Mathf.Clamp(improvement * 3f, 0f, 0.08f) * progressMultiplier);
                closestDistanceEver = currentDistance;
                backtrackCounter = 0;
            }
            // Penalize moving away from target — but relax near obstacles
            else if (currentDistance > previousDistance + 0.1f)
            {
                if (obstacleInPath)
                {
                    // Near obstacle: allow detours, just decay counter faster
                    backtrackCounter = Mathf.Max(0, backtrackCounter - 2);
                }
                else
                {
                    AddReward(-0.015f);
                    backtrackCounter++;

                    // Strong penalty for persistent oscillation
                    if (backtrackCounter > 20)
                    {
                        AddReward(-0.05f);
                    }
                }
            }
            else
            {
                // Gradually decay backtrack counter
                backtrackCounter = Mathf.Max(0, backtrackCounter - 1);
            }

            previousDistance = currentDistance;
        }

        // ========== EXPLORATION SYSTEM ==========
        // Grid-based exploration reward
        Vector2Int currentCell = new Vector2Int(
            Mathf.RoundToInt(transform.localPosition.x / GRID_CELL_SIZE),
            Mathf.RoundToInt(transform.localPosition.y / GRID_CELL_SIZE)
        );

        if (!visitedCells.Contains(currentCell))
        {
            visitedCells.Add(currentCell);
            // Decaying exploration reward - high early, fades as agent explores more
            float explorationReward = 0.05f / (1f + visitedCells.Count * 0.05f);
            AddReward(explorationReward);
        }

        // Anti-hovering system - only during navigation, NOT hover training
        if (!envManager.hoverMode && StepCnt % 10 == 0)
        {
            float movement = Vector2.Distance(transform.localPosition, lastHoverCheck);

            if (movement < 0.1f)
            { // Barely moved in 10 steps
                hoverCounter++;
                if (hoverCounter > 3)
                { // 30 steps of hovering - kick in faster
                    AddReward(-0.1f);
                }
                if (hoverCounter > 8)
                { // 80 steps of hovering - end it, not productive
                    AddReward(-1f);
                    endReason = "idle_hovering";
                    EndEpisode();
                    return;
                }
            }
            else
            {
                hoverCounter = 0;
            }

            lastHoverCheck = transform.localPosition;
        }

        // ========== MOVEMENT TRACKING ==========
        float newDist = Vector2.Distance(transform.localPosition, lastPos);
        if (newDist < 0.001f)
        {
            AddReward(-0.01f); // Penalty for not moving
        }
        distanceTraveled += newDist;
        lastPos = transform.localPosition;

        // Time/energy cost - must exceed passive rewards to prevent idle farming
        AddReward(-0.003f);

        // ========== STATS RECORDING ==========
        var recorder = Academy.Instance.StatsRecorder;
        recorder.Add("AngleStability", uprightReward);
        recorder.Add("ExploredCells", visitedCells.Count);
        recorder.Add("ClosestEver", closestDistanceEver);
        recorder.Add("BacktrackCount", backtrackCounter);
        recorder.Add("GroundDistance", groundDistance);
        recorder.Add("VerticalVelocity", rb.linearVelocity.y);
        recorder.Add("DroneHP", droneHP);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var act = actionsOut.ContinuousActions;

        // Horizontal control
        float horiz = 0f;
        if (Keyboard.current.aKey.isPressed) horiz = -1f;
        else if (Keyboard.current.dKey.isPressed) horiz = 1f;

        // Vertical control
        float vert = 0f;
        if (Keyboard.current.wKey.isPressed) vert = 1f;
        else if (Keyboard.current.sKey.isPressed) vert = -1f;

        // Rotation control
        float torque = 0f;
        if (Keyboard.current.qKey.isPressed) torque = 1f;
        else if (Keyboard.current.eKey.isPressed) torque = -1f;

        act[0] = horiz;
        act[1] = vert;
        act[2] = torque;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            groundCollision++;
            float impactSpeed = other.relativeVelocity.magnitude;

            // During hover training, ANY ground contact is bad
            if (envManager.hoverMode)
            {
                AddReward(-3f);
                endReason = "ground_contact_hover";
                EndEpisode();
                return;
            }

            if (impactSpeed >= hardLandingThreshold)
            {
                float damage = impactSpeed * impactSpeed * 1.0f; // Ground is slightly more forgiving than obstacles
                droneHP -= damage;
                AddReward(-damage / MAX_HP * 2f);
                Debug.Log($"Hard crash! Impact: {impactSpeed:F2}, Damage: {damage:F1}, HP: {droneHP:F1}");

                if (droneHP <= 0f)
                {
                    AddReward(-2f);
                    endReason = "hard_crash_destroyed";
                    EndEpisode();
                    return;
                }
            }
            else if (impactSpeed < 1f)
            {
                AddReward(0.5f);
                Debug.Log($"Gentle touchdown! Impact: {impactSpeed:F2}");
            }
            else if (impactSpeed < softLandingThreshold)
            {
                AddReward(0.2f);
                Debug.Log($"Soft landing! Impact: {impactSpeed:F2}");
            }
            else
            {
                // Rough landing — some HP damage
                float damage = impactSpeed * 2f; // Linear, lighter than obstacle hits
                droneHP -= damage;
                AddReward(-0.5f);
                Debug.Log($"Rough landing. Impact: {impactSpeed:F2}, Damage: {damage:F1}, HP: {droneHP:F1}");

                if (droneHP <= 0f)
                {
                    AddReward(-1f);
                    endReason = "rough_landing_destroyed";
                    EndEpisode();
                    return;
                }
            }
        }

        if (other.gameObject.CompareTag("Obstacle"))
        {
            float impactSpeed = other.relativeVelocity.magnitude;

            // Damage scales with impact speed: light brush = small scratch, full slam = massive damage
            // impactSpeed ~1 = 5 HP, ~3 = 15 HP, ~6+ = 60 HP (likely fatal)
            float damage = impactSpeed * impactSpeed * 1.5f; // Quadratic scaling
            droneHP -= damage;

            // Reward proportional to damage taken
            float damagePenalty = -damage / MAX_HP * 3f; // Max ~-3 for lethal hit
            AddReward(damagePenalty);

            Debug.Log($"Obstacle hit! Speed: {impactSpeed:F2}, Damage: {damage:F1}, HP: {droneHP:F1}");

            if (droneHP <= 0f)
            {
                AddReward(-2f); // Extra penalty for destruction
                endReason = "drone_destroyed";
                EndEpisode();
                return;
            }
        }
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            // Continuous contact damage — teaches agent to actively fly away
            float stayDamage = 0.5f; // Per physics frame
            droneHP -= stayDamage;
            AddReward(-0.02f);

            if (droneHP <= 0f)
            {
                AddReward(-2f);
                endReason = "grinding_obstacle_destroyed";
                EndEpisode();
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Victim"))
        {
            goalsReached++;

            // Calculate rewards - consistent base, small time bonus to reduce variance
            float baseReward = 5.0f;
            float timeBonus = Mathf.Max(0f, (maxStepCount - StepCnt) / (float)maxStepCount) * 1f;

            // Bonus for finding victims when obstacles are present (harder = more reward)
            float difficultyBonus = envManager.obstacleCount > 0 ? 2.0f : 0f;

            if (System.Array.IndexOf(goals, other.transform) == targetIndex)
            {
                AddReward(baseReward + timeBonus + difficultyBonus);
                Debug.Log($"Found target victim! Reward: {baseReward + timeBonus + difficultyBonus:F2}");
            }
            else
            {
                AddReward(baseReward * 0.8f + timeBonus + difficultyBonus);
                Debug.Log($"Found victim! Reward: {(baseReward * 0.8f + timeBonus + difficultyBonus):F2}");
            }

            targetReached?.Play();
            other.gameObject.SetActive(false);
        }

        // Check if all victims found
        if (goalsReached == goals.Length)
        {
            float efficiency = shortestPath / Mathf.Max(distanceTraveled, 0.001f);
            float completionBonus = 10f;
            float efficiencyBonus = efficiency * 5f;

            AddReward(completionBonus + efficiencyBonus);

            var recorder = Academy.Instance.StatsRecorder;
            recorder.Add("Efficiency", efficiency);
            recorder.Add("CompletionTime", StepCnt);

            Debug.Log($"Mission complete! Total reward: {GetCumulativeReward():F2}");
            endReason = "Completion";
            EndEpisode();
        }
        else
        {
            // Update to next nearest target
            deltaX = GetNearestDistance(transform.localPosition);
            previousDistance = DistanceToTarget();
            closestDistanceEver = previousDistance; // Reset for new target
            backtrackCounter = 0;
        }
    }

    void DrawRaySensorDebug()
    {
        if (raySensor == null) return;

        float rayLength = raySensor.RayLength;
        int raysPerSide = raySensor.RaysPerDirection;
        float maxAngle = raySensor.MaxRayDegrees;
        int totalRays = raysPerSide * 2 + 1;

        for (int i = 0; i < totalRays; i++)
        {
            float angle = (i - raysPerSide) * (maxAngle / raysPerSide);
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * raySensor.transform.up;
            Vector3 startPos = raySensor.transform.position;

            LayerMask layerMask = raySensor.GetComponent<RayPerceptionSensorComponent2D>().RayLayerMask;
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, rayLength, layerMask);

            Color rayColor = Color.red;

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                string[] detectableTags = raySensor.DetectableTags.ToArray();

                if (System.Array.IndexOf(detectableTags, hit.collider.tag) >= 0)
                {
                    if (hit.collider.CompareTag("Victim"))
                    {
                        rayColor = Color.green;
                    }
                    else if (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Obstacle"))
                    {
                        rayColor = Color.yellow;
                    }
                    else
                    {
                        rayColor = Color.blue;
                    }
                }
            }
            Debug.DrawLine(startPos, startPos + dir * rayLength, rayColor);
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(15, 25, 300, 30),
                $"Reward: {GetCumulativeReward():F2}", style);
        GUI.Label(new Rect(15, 60, 300, 30),
                $"Dist: {(envManager.isInitializing ? 0 : DistanceToTarget()):F1} | Record: {closestDistanceEver:F1}", style);
        GUI.Label(new Rect(15, 95, 300, 30),
                $"Explored: {visitedCells.Count} | Hover: {hoverCounter}", style);
        GUI.Label(new Rect(15, 130, 300, 30),
                $"HP: {droneHP:F1}", style);
    }

    void RecordStats()
    {
        var recorder = Academy.Instance.StatsRecorder;
        recorder.Add("EpisodeLength", StepCnt);
        recorder.Add("TargetsFound", goalsReached);
        recorder.Add("GroundCollision", groundCollision);
        recorder.Add("PathEfficiency", shortestPath / Mathf.Max(distanceTraveled, 0.001f));
        recorder.Add("FinalReward", GetCumulativeReward());
    }

    // Optional: Visualize explored grid cells in Scene view
    void OnDrawGizmos()
    {
        if (visitedCells == null || visitedCells.Count == 0) return;

        // Draw visited cells
        foreach (Vector2Int cell in visitedCells)
        {
            Vector3 worldPos = new Vector3(
                cell.x * GRID_CELL_SIZE,
                cell.y * GRID_CELL_SIZE,
                0
            );
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawCube(worldPos, Vector3.one * GRID_CELL_SIZE);
        }

        // Draw current cell
        if (Application.isPlaying)
        {
            Vector2Int current = new Vector2Int(
                Mathf.RoundToInt(transform.localPosition.x / GRID_CELL_SIZE),
                Mathf.RoundToInt(transform.localPosition.y / GRID_CELL_SIZE)
            );
            Vector3 currentWorldPos = new Vector3(
                current.x * GRID_CELL_SIZE,
                current.y * GRID_CELL_SIZE,
                0
            );
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(currentWorldPos, Vector3.one * GRID_CELL_SIZE);
        }
    }
}