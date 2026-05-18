using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class RayVisualizer : MonoBehaviour
{
    public RayPerceptionSensorComponent2D[] sensors;

    [Header("Optimization")]
    [Range(1, 10)]
    public int drawEveryNthRay = 2;

    [Range(0.05f, 1f)]
    public float missRayMultiplier = 0.3f;

    [Range(1, 10)]
    public int updateEveryNFrames = 2;

    public bool drawOnlyHits = false;

    [Header("Visual")]
    [Range(0.05f, 1f)]
    public float alpha = 0.4f;

    private static Material lineMat;

    private struct RayData
    {
        public Vector3 start;
        public Vector3 end;
        public Color color;
    }

    private readonly List<RayData> rayCache = new();

    void CreateMat()
    {
        if (lineMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");

            lineMat = new Material(shader);

            lineMat.hideFlags = HideFlags.HideAndDontSave;

            lineMat.SetInt("_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

            lineMat.SetInt("_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            lineMat.SetInt("_Cull",
                (int)UnityEngine.Rendering.CullMode.Off);

            lineMat.SetInt("_ZWrite", 0);
        }
    }

    void Update()
    {
        if (sensors == null || sensors.Length == 0)
            return;

        // Reduces flicker + improves recording quality
        if (Time.frameCount % updateEveryNFrames != 0)
            return;

        CacheRays();
    }

    void CacheRays()
    {
        rayCache.Clear();

        foreach (var sensor in sensors)
        {
            if (sensor == null)
                continue;

            float rayLength = sensor.RayLength;
            int raysPerSide = sensor.RaysPerDirection;
            float maxAngle = sensor.MaxRayDegrees;

            int totalRays = raysPerSide * 2 + 1;

            Transform tf = sensor.transform;

            for (int i = 0; i < totalRays; i++)
            {
                // Skip some rays for cleaner visuals
                if (i % drawEveryNthRay != 0)
                    continue;

                float angle = totalRays > 1
                    ? Mathf.Lerp(
                        -maxAngle,
                        maxAngle,
                        (float)i / (totalRays - 1))
                    : 0f;

                Vector3 dir =
                    Quaternion.Euler(0, 0, angle) *
                    tf.up;

                Vector3 origin = tf.position;

                RaycastHit2D hit =
                    Physics2D.Raycast(
                        origin,
                        dir,
                        rayLength,
                        sensor.RayLayerMask
                    );

                bool didHit = hit.collider != null;

                // Optional: only draw successful hits
                if (!didHit && drawOnlyHits)
                    continue;

                Vector3 endPos = didHit
                    ? (Vector3)hit.point
                    : origin + dir * rayLength * missRayMultiplier;

                // Softer transparent colors compress better in video
                Color rayColor =
                    new Color(1f, 0f, 0f, alpha);

                if (didHit)
                {
                    if (hit.collider.CompareTag("Victim"))
                    {
                        rayColor =
                            new Color(0f, 1f, 0f, alpha);
                    }
                    else if (hit.collider.CompareTag("Ground"))
                    {
                        rayColor =
                            new Color(1f, 1f, 0f, alpha);
                    }
                    else if (hit.collider.CompareTag("Obstacle"))
                    {
                        // softer blue
                        rayColor =
                            new Color(0.2f, 0.6f, 1f, alpha);
                    }
                    else if (hit.collider.CompareTag("Agent"))
                    {
                        // softer magenta
                        rayColor =
                            new Color(1f, 0.3f, 1f, alpha);
                    }
                    else
                    {
                        rayColor =
                            new Color(0f, 1f, 1f, alpha);
                    }
                }

                rayCache.Add(new RayData
                {
                    start = origin,
                    end = endPos,
                    color = rayColor
                });
            }
        }
    }

    void OnPostRender()
    {
        if (rayCache.Count == 0)
            return;

        CreateMat();

        lineMat.SetPass(0);

        GL.PushMatrix();

        GL.Begin(GL.LINES);

        foreach (var ray in rayCache)
        {
            GL.Color(ray.color);

            GL.Vertex(ray.start);
            GL.Vertex(ray.end);
        }

        GL.End();

        GL.PopMatrix();
    }
}