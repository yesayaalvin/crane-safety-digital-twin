using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class CraneTelemetryLogger : MonoBehaviour
{
    [Header("Sampling Settings")]
    public float samplingInterval = 0.1f; // 10 Hz (every 0.1 second)
    public bool isLoggingActive = true;

    [Header("Site & Safety Parameters")]
    public float sitePadSize = 26.0f;     // Square pad (26m x 26m)
    public float loadDangerRadius = 3.0f; // Drop-Zone Radius (3m)

    [Header("Transforms (World Space)")]
    public Transform craneMastCenter;     // Base of Crane Mast (0,0,0)
    public Transform craneDriverCabin;    // Cabin (for Slew Angle Y)
    public Transform craneTrolley;        // CraneRopeBase (Trolley)
    public Transform craneLoad;           // Precast Concrete Slab
    public Transform buildingObstacle;    // Building (Cube)
    public List<Transform> allWorkers;    // The 3 Workers (Capsule, Capsule (1), Capsule (2))

    [Header("Sensors & Cameras")]
    public Camera driverCamera;           // Driver POV Camera
    public Camera hookCamera;             // Hook Cam

    [Header("Layers")]
    public LayerMask obstacleLayer;       // Obstacle Layer (Building)
    public LayerMask loadLayer;           // Load Layer

    private Rigidbody craneLoadRb;
    private Collider buildingCollider;
    private string logFolderPath;
    private string currentFilePath;
    private StreamWriter csvWriter;
    private float timer = 0f;
    private float simTimeElapsed = 0f;

    void Awake()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        if (craneLoad != null)
        {
            craneLoadRb = craneLoad.GetComponent<Rigidbody>();
        }

        if (buildingObstacle != null)
        {
            buildingCollider = buildingObstacle.GetComponent<Collider>();
        }

        InitializeTelemetryFile();
    }

    void InitializeTelemetryFile()
    {
        try
        {
            logFolderPath = Path.Combine(Application.dataPath, "TelemetryLogs");

            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"crane_telemetry_log_{timestamp}.csv";
            currentFilePath = Path.Combine(logFolderPath, fileName);

            csvWriter = new StreamWriter(currentFilePath, false, Encoding.UTF8, 65536);

            // 22 Accurate World-Space Variables
            string header = "timestamp_iso," +
                            "sim_time_elapsed_sec," +
                            "crane_slew_angle_deg," +
                            "crane_mast_x," +
                            "crane_mast_z," +
                            "trolley_pos_x," +
                            "trolley_pos_z," +
                            "load_pos_x," +
                            "load_pos_y," +
                            "load_pos_z," +
                            "load_velocity_mps," +
                            "worker_id," +
                            "worker_pos_x," +
                            "worker_pos_z," +
                            "worker_to_load_dist_2d," +
                            "is_in_drop_zone," +
                            "is_in_driver_frustum," +
                            "is_driver_los_blocked," +
                            "bldg_world_center_x," +
                            "bldg_world_center_z," +
                            "bldg_world_size_x," +
                            "bldg_world_size_z," +
                            "load_danger_radius," +
                            "site_pad_size," +
                            "safety_status_label";

            csvWriter.WriteLine(header);
            csvWriter.Flush();

            Debug.Log($"<color=green>[Telemetry Logger] Real-time World-Space logger ready: {currentFilePath}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Telemetry Logger] Initialization Error: {ex.Message}");
        }
    }

    void Update()
    {
        if (!isLoggingActive || csvWriter == null) return;

        simTimeElapsed += Time.deltaTime;
        timer += Time.deltaTime;

        if (timer >= samplingInterval)
        {
            LogTelemetryFrame();
            timer = 0f;
        }
    }

    void LogTelemetryFrame()
    {
        if (craneLoad == null || craneDriverCabin == null || craneTrolley == null) return;

        string isoTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        float slewAngle = craneDriverCabin.eulerAngles.y;

        // 1. Strict World Space Coordinates (Meters)
        Vector3 mastPos = (craneMastCenter != null) ? craneMastCenter.position : Vector3.zero;
        Vector3 trolleyPos = craneTrolley.position;
        Vector3 loadPos = craneLoad.position;
        float loadVelocity = (craneLoadRb != null) ? craneLoadRb.velocity.magnitude : 0f;

        // 2. Exact Building World Bounds directly from Physics Collider
        float bldgX = 4.07f, bldgZ = 6.29f, bldgSizeX = 3.0f, bldgSizeZ = 3.0f;
        if (buildingCollider != null)
        {
            bldgX = buildingCollider.bounds.center.x;
            bldgZ = buildingCollider.bounds.center.z;
            bldgSizeX = buildingCollider.bounds.size.x;
            bldgSizeZ = buildingCollider.bounds.size.z;
        }
        else if (buildingObstacle != null)
        {
            bldgX = buildingObstacle.position.x;
            bldgZ = buildingObstacle.position.z;
            bldgSizeX = buildingObstacle.lossyScale.x;
            bldgSizeZ = buildingObstacle.lossyScale.z;
        }

        // 3. Structural Proximity Check (Is Load within 3m of Building?)
        Collider[] nearbyObstacles = Physics.OverlapSphere(loadPos, loadDangerRadius, obstacleLayer);
        bool isBuildingNear = false;

        foreach (Collider col in nearbyObstacles)
        {
            if (col.transform != craneLoad && !col.transform.IsChildOf(craneLoad))
            {
                isBuildingNear = true;
                break;
            }
        }

        // 4. Evaluate Each Active Worker in World Space
        foreach (Transform worker in allWorkers)
        {
            if (worker == null || worker.gameObject.layer == LayerMask.NameToLayer("Obstacle")) continue;

            string workerId = worker.name;
            Vector3 workerPos = worker.position;
            Vector3 targetChest = workerPos + Vector3.up * 1.0f;

            // Pure 2D Horizontal Distance (Meters)
            float dist2D = Vector2.Distance(new Vector2(workerPos.x, workerPos.z), new Vector2(loadPos.x, loadPos.z));
            int isInDropZone = (dist2D <= loadDangerRadius) ? 1 : 0;

            int inDriverFrustum = 0;
            int isDriverLosBlocked = 0;

            if (driverCamera != null)
            {
                Vector3 vp = driverCamera.WorldToViewportPoint(targetChest);
                bool inFrustum = (vp.x >= 0.02f && vp.x <= 0.98f && vp.y >= 0.02f && vp.y <= 0.98f && vp.z > 0.1f);
                inDriverFrustum = inFrustum ? 1 : 0;

                Vector3 dOrigin = driverCamera.transform.position;
                Vector3 dDir = (targetChest - dOrigin).normalized;
                float dDist = Vector3.Distance(dOrigin, targetChest);

                if (Physics.Raycast(dOrigin, dDir, out RaycastHit dHit, dDist, obstacleLayer))
                {
                    isDriverLosBlocked = 1;
                }
            }

            // Safety Status Determination
            string safetyLabel = "SAFE";

            if (isInDropZone == 1)
            {
                if (inDriverFrustum == 0 || isDriverLosBlocked == 1)
                {
                    safetyLabel = "CRITICAL_BLINDSPOT";
                }
                else
                {
                    safetyLabel = "DANGER_ZONE";
                }
            }
            else if (isBuildingNear)
            {
                safetyLabel = "STRUCTURAL_HAZARD";
            }

            // Write Row to CSV
            string csvRow = $"{isoTimestamp}," +
                           $"{simTimeElapsed:F2}," +
                           $"{slewAngle:F2}," +
                           $"{mastPos.x:F3}," +
                           $"{mastPos.z:F3}," +
                           $"{trolleyPos.x:F3}," +
                           $"{trolleyPos.z:F3}," +
                           $"{loadPos.x:F3}," +
                           $"{loadPos.y:F3}," +
                           $"{loadPos.z:F3}," +
                           $"{loadVelocity:F3}," +
                           $"{workerId}," +
                           $"{workerPos.x:F3}," +
                           $"{workerPos.z:F3}," +
                           $"{dist2D:F3}," +
                           $"{isInDropZone}," +
                           $"{inDriverFrustum}," +
                           $"{isDriverLosBlocked}," +
                           $"{bldgX:F3}," +
                           $"{bldgZ:F3}," +
                           $"{bldgSizeX:F3}," +
                           $"{bldgSizeZ:F3}," +
                           $"{loadDangerRadius:F1}," +
                           $"{sitePadSize:F1}," +
                           $"{safetyLabel}";

            csvWriter.WriteLine(csvRow);
        }
    }

    void OnApplicationQuit()
    {
        if (csvWriter != null) { csvWriter.Flush(); csvWriter.Close(); csvWriter = null; }
    }

    void OnDisable()
    {
        if (csvWriter != null) { csvWriter.Flush(); csvWriter.Close(); csvWriter = null; }
    }
}