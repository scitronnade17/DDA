using System.Collections.Generic;
using UnityEngine;

public class RoadAnalyzer : MonoBehaviour
{
    [Header("Main settings")]
    public float gravity = 9.81f;

    [Header("Waypoints settings")]
    public List<WaypointSegment> waypoints = new List<WaypointSegment>();
    public bool autoDetectWaypoints = true;
    public string waypointTag = "Waypoint";

    [Header("Friction settings")]
    public float defaultFriction = 0.8f;


    [Header("Detect settings")]
    public LayerMask roadLayer = -1;
    public float raycastHeight = 10f;
    public float raycastDistance = 20f;

    private WaypointDetector waypointDetector;
    private GeometryAnalyzer geometryAnalyzer;
    private SpeedCalculator speedCalculator;
    private FrictionAnalyzer frictionAnalyzer;
    private SpeedOptimizer speedOptimizer;

    [HideInInspector] public float defaultSegmentLength = 20f;
    [HideInInspector] public float defaultRoadWidth = 8f;

    [HideInInspector] public float minCurveRadius = 1f;
    [HideInInspector] public float maxCurveRadius = 1000f;

    void Start()
    {
        InitializeComponents();

        if (autoDetectWaypoints)
        {
            waypointDetector.DetectWaypoints();
        }

        AnalyzeRoad();
    }

    void InitializeComponents()
    {
        waypointDetector = new WaypointDetector(this);
        geometryAnalyzer = new GeometryAnalyzer(this);
        speedCalculator = new SpeedCalculator(this);
        frictionAnalyzer = new FrictionAnalyzer(this);
        speedOptimizer = new SpeedOptimizer(this);
    }

    void AnalyzeRoad()
    {
        geometryAnalyzer.AnalyzeWaypointsGeometry();
        speedCalculator.CalculateAllSafeSpeeds();
        speedOptimizer.OptimizeAllSegments();
    }

    public WaypointSegment GetSegmentAtPosition(Vector3 position)
    {
        return geometryAnalyzer.GetSegmentAtPosition(position);
    }

}