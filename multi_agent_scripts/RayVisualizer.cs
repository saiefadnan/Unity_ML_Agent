using UnityEngine;
using Unity.MLAgents.Sensors;

public class RayVisualizer : MonoBehaviour
{
    public RayPerceptionSensorComponent2D[] sensors;
    private static Material lineMat;

    void CreateMat()
    {
        if (!lineMat)
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            lineMat = new Material(shader);
        }
    }

    void OnPostRender()
    {
        if (sensors == null || sensors.Length == 0) return;

        CreateMat();
        lineMat.SetPass(0);

        foreach (var sensor in sensors)
        {
            DrawRays(sensor);
        }
    }

    void DrawRays(RayPerceptionSensorComponent2D sensor)
    {
        float rayLength = sensor.RayLength;
        int raysPerSide = sensor.RaysPerDirection;
        float maxAngle = sensor.MaxRayDegrees;
        int totalRays = raysPerSide * 2 + 1;

        for (int i = 0; i < totalRays; i++)
        {
            float angle = Mathf.Lerp(-maxAngle, maxAngle, (float)i / (totalRays - 1));
            Vector3 dir = Quaternion.Euler(0, 0, angle) * sensor.transform.up;
            Vector3 origin = sensor.transform.position;

            LayerMask mask = sensor.RayLayerMask;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, rayLength, mask);

            Vector3 endPos = hit ? (Vector3)hit.point : origin + dir * rayLength;

            // Colors
            Color rayColor = Color.red;
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Victim")) rayColor = Color.green;
                else if (hit.collider.CompareTag("Ground")) rayColor = Color.yellow;
                else if(hit.collider.CompareTag("Obstacle")) rayColor = Color.blue;
                else if(hit.collider.CompareTag("Agent")) rayColor = Color.magenta;
            }

            GL.Begin(GL.LINES);
            GL.Color(rayColor);
            GL.Vertex(origin);   // world coords
            GL.Vertex(endPos);   // world coords
            GL.End();
        }
    }
}
