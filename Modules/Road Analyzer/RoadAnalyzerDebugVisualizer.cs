#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class DebugSettings
{
    public bool showWaypoints = true;
    public bool showSegments = true;
    public bool showSegmentWidth = true;

    [Range(0.1f, 5f)]
    public float segmentVisualLength = 1f;

    public Color segmentColor = Color.blue;
    public Color widthColor = Color.yellow;
}

public class RoadAnalyzerDebugVisualizer : MonoBehaviour
{
    public RoadAnalyzer roadAnalyzer;
    public DebugSettings debugSettings = new DebugSettings();

    void OnDrawGizmosSelected()
    {
        if (!debugSettings.showWaypoints || roadAnalyzer == null ||
            roadAnalyzer.waypoints == null || roadAnalyzer.waypoints.Count == 0)
            return;

        for (int i = 0; i < roadAnalyzer.waypoints.Count; i++)
        {
            var segment = roadAnalyzer.waypoints[i];
            if (segment.waypoint == null) continue;

            Vector3 pos = segment.waypoint.position;
            Vector3 start = segment.segmentStart;
            Vector3 end = segment.segmentEnd;

            DrawSegment(segment, start, end, pos, i);
            DrawRoadWidth(segment, pos);

            DrawSegmentInfo(segment, pos);
        }
    }

    private void DrawSegment(WaypointSegment segment, Vector3 start, Vector3 end, Vector3 pos, int index)
    {
        if (debugSettings.showSegments)
        {
            Gizmos.color = debugSettings.segmentColor;
            Gizmos.DrawLine(start, end);

            Vector3 midPoint = (start + end) / 2f;
            Vector3 direction = (end - start).normalized;
            Handles.ArrowHandleCap(0, midPoint,
                Quaternion.LookRotation(direction),
                debugSettings.segmentVisualLength, EventType.Repaint);
        }
    }

    private void DrawRoadWidth(WaypointSegment segment, Vector3 pos)
    {
        if (segment.roadWidth > 0 && debugSettings.showSegmentWidth)
        {
            Gizmos.color = debugSettings.widthColor;
            Vector3 right = Vector3.Cross(segment.segmentDirection, Vector3.up).normalized;
            Vector3 leftEdge = pos - right * (segment.roadWidth / 2f);
            Vector3 rightEdge = pos + right * (segment.roadWidth / 2f);

            Gizmos.DrawLine(leftEdge + Vector3.up * 0.1f, rightEdge + Vector3.up * 0.1f);
            Gizmos.DrawLine(leftEdge, leftEdge + Vector3.up * 0.5f);
            Gizmos.DrawLine(rightEdge, rightEdge + Vector3.up * 0.5f);
        }
    }

    private void DrawSegmentInfo(WaypointSegment segment, Vector3 pos)
    {
        string info = $"Segment {segment.waypoint.name}\n" +
                     $"Length: {segment.segmentLength:F1}m\n" +
                     $"Width: {segment.roadWidth:F1}m\n" +
                     $"Curve: {segment.curveAngle:F1}° (R={segment.curveRadius:F0}m)\n" +
                     $"Speed: {segment.maxSafeSpeed:F0} km/h\n" +
                     $"Opt Speed: {segment.optimizedSafeSpeed:F0} km/h\n" +
                     $"Friction: μ={segment.frictionCoefficient:F2}\n" +
                     $"Incline: {segment.inclineAngle:F1}°\n" +
                     $"Obstacles: {segment.obstaclePercent:F1}%\n" +
                     $"Time: {segment.recommendedTime:F2} s";

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 8;
        style.fontStyle = FontStyle.Bold;

        GUIStyle outlineStyle = new GUIStyle(style);
        outlineStyle.normal.textColor = Color.black;

        Vector3 labelPos = pos + Vector3.up * 3f;


        Handles.Label(labelPos + Vector3.right * 0.02f, info, outlineStyle);
        Handles.Label(labelPos + Vector3.left * 0.02f, info, outlineStyle);
        Handles.Label(labelPos + Vector3.up * 0.02f, info, outlineStyle);
        Handles.Label(labelPos + Vector3.down * 0.02f, info, outlineStyle);

        Handles.Label(labelPos, info, style);
    }
}
#endif