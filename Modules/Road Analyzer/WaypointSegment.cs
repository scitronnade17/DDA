using UnityEngine;

[System.Serializable]
public class WaypointSegment
{
    public Transform waypoint;
    [HideInInspector] public float segmentLength = 10f;
    [HideInInspector] public float roadWidth = 8f;
    [HideInInspector] public float obstacleWidth = 0f;
    [HideInInspector] public float obstacleLength = 0f;
    [HideInInspector] public float obstaclePercent = 0f;
    [HideInInspector] public float frictionCoefficient = 0.8f;

    [HideInInspector] public Vector3 segmentDirection;
    [HideInInspector] public float curveAngle = 0f;
    [HideInInspector] public float curveRadius = 1000f;
    [HideInInspector] public float inclineAngle = 0f;

    [HideInInspector] public float maxSafeSpeed = 0f;
    [HideInInspector] public float optimizedSafeSpeed = 0f;

    [HideInInspector] public PhysicMaterial detectedPhysicMaterial;

    [HideInInspector] public Vector3 segmentStart;
    [HideInInspector] public Vector3 segmentEnd;

    [HideInInspector] public float recommendedTime = 0f;
}