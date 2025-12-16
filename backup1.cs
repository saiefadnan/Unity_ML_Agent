using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Agent2D : Agent
{
    [Header("Camera Configuration")]
    public Camera agentCamera; // Assign the camera in Unity Inspector
    public bool logCameraInfo = true; // Toggle to enable/disable camera logging
    private int logCounter = 0;
    
    [Header("Agent Configuration")]
    public Transform[] goals;
    public AudioSource targetReached;
    public AudioSource droneHum;
    Vector2 lastPos;
    float shortestPath = 0.0f;
    float distanceTraveled = 0.0f;
    int goalsReached = 0;
    bool groundCollision = false;
    int targetIndex = 0;
    int StepCount = 0;
    Rigidbody2D rb;
    float deltaX= 0.0f;
    float targetZ = 0.0f;
    float rotationPerAction = 10.0f;
    float movePerAction = 3.0f;
    float targetY = 0.0f;
    float P = 0f;
    float D = 0f;
    //bool isJumping = false;
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Log camera setup information
        if (logCameraInfo && agentCamera != null)
        {
            Debug.Log($"[CAMERA] Camera initialized - Position: {agentCamera.transform.position}, Rotation: {agentCamera.transform.eulerAngles}");
            Debug.Log($"[CAMERA] Camera FOV: {agentCamera.fieldOfView}, Near: {agentCamera.nearClipPlane}, Far: {agentCamera.farClipPlane}");
            Debug.Log($"[CAMERA] Camera resolution: {agentCamera.pixelWidth}x{agentCamera.pixelHeight}");
        }
        else if (logCameraInfo)
        {
            Debug.LogWarning("[CAMERA] Camera logging enabled but no camera assigned!");
        }
    }
    
    float DistanceToTarget(){
        return Vector2.Distance(goals[targetIndex].localPosition, transform.localPosition);
    }
    void calculateShortestPath(){
       shortestPath+=deltaX;
       int k = targetIndex;
       Dictionary<int, bool> visited = new Dictionary<int, bool>();
       visited[k] = true;
       while(visited.Count < goals.Length){
           float nearestDist = Mathf.Infinity;
           int nearestIndex = -1;
           for(int i=0; i<goals.Length; i++){
               if(!visited.ContainsKey(i) && i!= k){
                   float dist = Vector2.Distance(goals[i].localPosition, goals[k].localPosition);
                   if(dist < nearestDist){
                       nearestDist = dist;
                       nearestIndex = i;
                   }
               }
           }
           if(nearestIndex != -1){
               shortestPath += nearestDist;
               visited[nearestIndex] = true;
               k = nearestIndex;
           }
       }
    }
    
    float GetNearestDistance(Vector3 source){
        float newDeltaX = Mathf.Infinity;
        foreach (Transform goal in goals)
        {
            float dist = Vector2.Distance(goal.localPosition, source);
            if (goal.gameObject.activeSelf && dist < newDeltaX){
                newDeltaX = dist;
                targetIndex = System.Array.IndexOf(goals, goal);
            }
        }
        return newDeltaX;
    }
    public override void OnEpisodeBegin()
    {
        
        if (StepCount > 0)
        {
            var recorder = Academy.Instance.StatsRecorder;
            recorder.Add("EpisodeLength", StepCount);
            recorder.Add("TargetsFound", goalsReached);
            recorder.Add("PathEfficiency", shortestPath / Mathf.Max(distanceTraveled, 0.001f));
        }
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        targetY = transform.localPosition.y;
        P=5.0f;
        D=30.0f;
        shortestPath = 0.0f;
        lastPos = transform.localPosition;
        distanceTraveled = 0.0f;
        StepCount = 0;
        groundCollision = false;
        if (goalsReached != 0)
        {
            goalsReached = 0;
            foreach (Transform goal in goals)
            {
                goal.gameObject.SetActive(true);
            }
        }
        transform.localPosition = new Vector2(Random.Range(-7f, 7f), -1.93f);
        rb.SetRotation(0f);
        rb.Sleep();
        rb.WakeUp();
        for (int i = 0; i < goals.Length; i++)
        {
            goals[i].localPosition = new Vector2(Random.Range(-7f, 7f), Random.Range(-3f, 7f));
        }

        //Debug.Log("Agent Pos: " + transform.localPosition + " Goal Pos: " + goals[0].localPosition);
        deltaX = GetNearestDistance(transform.localPosition);
        calculateShortestPath();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Only agent state - NO victim coordinates!
        sensor.AddObservation(rb.linearVelocity); // 2 values
        sensor.AddObservation(rb.angularVelocity); // 1 value
        
        // Add agent orientation for stability
        float normalizedRotation = transform.eulerAngles.z;
        if (normalizedRotation > 180) normalizedRotation -= 360;
        sensor.AddObservation(normalizedRotation / 180f); // 1 value, normalized to [-1,1]
        
        // Total: 4 observations (camera will provide the visual input)
    }

    void Update()
    {

        float speed = rb.linearVelocity.magnitude;
        droneHum.pitch = Mathf.Lerp(1.0f, 1.5f, speed / 10f);
        // Check if agent fell off
        if (transform.localPosition.y < -6f || transform.localPosition.y > 8f || transform.localPosition.x < -16f || transform.localPosition.x > 16f)
        { // pick a suitable value below the floor
            SetReward(-1f); // optional: give negative reward
            EndEpisode();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        //print("taking actions..");
        StepCount++;
        int action = actions.DiscreteActions[0];
        Vector2 move = Vector2.zero;
        if(action == 0) move = Vector2.zero;
        else if (action == 1) move = Vector2.up;
        else if (action == 2) move = Vector2.down;
        else if (action == 3) move = Vector2.left;
        else if (action == 4) move = Vector2.right;

        int rotationAction = actions.DiscreteActions[1];
        
        float angleZ = transform.eulerAngles.z;
        if (rotationAction == 1) {
            targetZ +=rotationPerAction;
        } else if (rotationAction==2) {
            targetZ -=rotationPerAction;
        }
        float torqueAmount = Mathf.DeltaAngle(angleZ, targetZ)*0.5f - rb.angularVelocity * 0.1f;
        rb.AddTorque(torqueAmount);

        if (action == 1)
        {   
            D = 2.5f;
            P= 5.0f;
            targetY = transform.localPosition.y + movePerAction;
        }
        if(action == 2)
        {
            D = 10.0f;
            P= 5.0f;
            targetY = transform.localPosition.y - movePerAction;
        }
        if (action > 1)
        {
            rb.AddForce(move * 5f, ForceMode2D.Force);
        }

        float upwardForce = (targetY - transform.localPosition.y)* P - rb.linearVelocity.y * D;
        rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Force);

        //stability reward
        
        float normalizedZRotation = angleZ > 180 ? angleZ - 360 : angleZ;
        float uprightReward =  1.0f - (Mathf.Abs(normalizedZRotation) / 180.0f);

        float tiltThreshold = 0.85f;
        if(uprightReward < tiltThreshold)AddReward(Mathf.Pow((tiltThreshold-uprightReward)/tiltThreshold,2) * (-0.05f));

        //path efficiency
        distanceTraveled += Vector2.Distance(transform.localPosition, lastPos);
        lastPos = transform.localPosition;

        // PURE VISION: Only basic rewards, let camera vision handle victim detection
        // Small negative reward to encourage efficiency and exploration
        AddReward(-0.001f);

        //collecting data
        RecordStats(uprightReward);
       
    }

    void RecordStats(float uprightReward)
    {
        var recorder = Academy.Instance.StatsRecorder;
        recorder.Add("AngleStability", uprightReward);
        recorder.Add("GroundCollision", groundCollision ? 1f : 0f);
        recorder.Add("Reward", GetCumulativeReward());
        recorder.Add("TargetsFound", goalsReached); // Simple metric
        
        // Basic camera logging (no raycasting)
        if (logCameraInfo && agentCamera != null)
        {
            logCounter++;
            if (logCounter >= 500)
            {
                Debug.Log($"[PURE VISION] Step {StepCount} - Learning from camera pixels only");
                Debug.Log($"[CAMERA] Position: {agentCamera.transform.position:F2}, Active: {agentCamera.enabled}");
                logCounter = 0;
            }
        }
    }
    
    // Method to manually check camera status (can be called from inspector during play mode)
    [ContextMenu("Check Camera Status")]
    public void CheckCameraStatus()
    {
        if (agentCamera != null)
        {
            Debug.Log($"[CAMERA CHECK] Camera enabled: {agentCamera.enabled}");
            Debug.Log($"[CAMERA CHECK] Camera position: {agentCamera.transform.position}");
            Debug.Log($"[CAMERA CHECK] Camera looking at: {agentCamera.transform.forward}");
            Debug.Log($"[CAMERA CHECK] Agent position: {transform.position}");
            Debug.Log($"[CAMERA CHECK] Current step: {StepCount}, Episode: {Academy.Instance.EpisodeCount}");
        }
        else
        {
            Debug.LogError("[CAMERA CHECK] No camera assigned!");
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //print("taking heuristic actions..");

        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Keyboard.current.upArrowKey.isPressed) discreteActionsOut[0] = 1;
        else if (Keyboard.current.downArrowKey.isPressed) discreteActionsOut[0] = 2;
        else if (Keyboard.current.leftArrowKey.isPressed) discreteActionsOut[0] = 3;
        else if (Keyboard.current.rightArrowKey.isPressed) discreteActionsOut[0] = 4;

        if (Keyboard.current.aKey.isPressed) discreteActionsOut[1] = 1; // rotate CW
        else if (Keyboard.current.dKey.isPressed) discreteActionsOut[1] = 2; // rotate CCW
        else discreteActionsOut[1] = 0; // no rotation
    }

    void OnCollisionEnter2D(Collision2D other){
        if (other.gameObject.CompareTag("Ground"))
        {
            //isJumping = false;
            AddReward(-0.01f);
            groundCollision = true;
            //Debug.Log("Landed on the ground.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Victim")){
            //print("Collided..");
            goalsReached++;
            targetReached.Play();
            if (System.Array.IndexOf(goals, other.transform) == targetIndex)
            {
                AddReward(2.0f);
            }
            else
            {
                AddReward(1.0f);
            }
            other.gameObject.SetActive(false);
            // EndEpisode();
        }
        if (goalsReached == 5){
            EndEpisode();
        }
        else{
            deltaX = GetNearestDistance(transform.localPosition);
        }
    }
}
