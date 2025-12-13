using UnityEngine;

public class GeometryAnalyzer
{
    private RoadAnalyzer analyzer;
    private ObstacleDetector obstacleDetector;
    private FrictionAnalyzer frictionAnalyzer;

    public GeometryAnalyzer(RoadAnalyzer analyzer)
    {
        this.analyzer = analyzer;
        this.obstacleDetector = new ObstacleDetector();
        this.frictionAnalyzer = new FrictionAnalyzer(analyzer);
    }

    public void AnalyzeWaypointsGeometry()
    {
        if (analyzer.waypoints.Count < 2)
        {
            Debug.LogWarning("Needed 2 or more segments!");
            return;
        }

        for (int i = 0; i < analyzer.waypoints.Count; i++)
        {
            WaypointSegment segment = analyzer.waypoints[i];
            CalculateSegmentBounds(segment, i);
            CalculateSegmentDirection(segment);
            CalculateCurvature(segment, i);
            AnalyzeSurfaceProperties(segment);
            frictionAnalyzer.DetermineFrictionFromPhysicMaterial(segment);
            obstacleDetector.DetectObstacles(segment);
        }
    }

    private void CalculateSegmentBounds(WaypointSegment segment, int index)
    {
        if (index == analyzer.waypoints.Count - 1)
        {
            segment.segmentStart = segment.waypoint.position;
            segment.segmentEnd = analyzer.waypoints[0].waypoint.position;
        }
        else
        {
            segment.segmentStart = segment.waypoint.position;
            segment.segmentEnd = analyzer.waypoints[index + 1].waypoint.position;
        }
    }

    private void CalculateSegmentDirection(WaypointSegment segment)
    {
        Vector3 segmentVector = segment.segmentEnd - segment.segmentStart;
        segment.segmentLength = segmentVector.magnitude;
        segment.segmentDirection = segmentVector.normalized;
    }

    private void CalculateCurvature(WaypointSegment segment, int index)
    {
        if (analyzer.waypoints.Count < 3)
        {
            segment.curveAngle = 0f;
            segment.curveRadius = analyzer.maxCurveRadius;
            return;
        }

        Vector3 prevPoint;
        Vector3 nextPoint;

        if (index == 0)
        {
            prevPoint = analyzer.waypoints[analyzer.waypoints.Count - 1].waypoint.position;
            nextPoint = analyzer.waypoints[index + 1].waypoint.position;
        }
        else if (index == analyzer.waypoints.Count - 1)
        {
            prevPoint = analyzer.waypoints[index - 1].waypoint.position;
            nextPoint = analyzer.waypoints[0].waypoint.position;
        }
        else
        {
            prevPoint = analyzer.waypoints[index - 1].waypoint.position;
            nextPoint = analyzer.waypoints[index + 1].waypoint.position;
        }

        Vector3 currentPoint = segment.waypoint.position;

        Vector3 inVec = currentPoint - prevPoint;
        Vector3 outVec = nextPoint - currentPoint;

        float inLength = inVec.magnitude;
        float outLength = outVec.magnitude;

        Vector3 inDirection = inVec.normalized;
        Vector3 outDirection = outVec.normalized;

        segment.curveAngle = Vector3.Angle(inDirection, outDirection);

        if (segment.curveAngle > 0.1f && segment.curveAngle < 175f)
        {
            float a = Vector3.Distance(prevPoint, currentPoint);
            float b = Vector3.Distance(currentPoint, nextPoint);
            float c = Vector3.Distance(prevPoint, nextPoint);

            float s = (a + b + c) * 0.5f;

            float area = Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));

            if (area > 0.0001f)
            {
                segment.curveRadius = (a * b * c) / (4f * area);
            }
            else
            {
                segment.curveRadius = analyzer.maxCurveRadius;
            }

            segment.curveRadius = Mathf.Clamp(segment.curveRadius, analyzer.minCurveRadius, analyzer.maxCurveRadius);
        }
        else
        {
            segment.curveAngle = 0f;
            segment.curveRadius = analyzer.maxCurveRadius;
        }
    }

    private void AnalyzeSurfaceProperties(WaypointSegment segment)
    {
        float heightDifference = segment.segmentEnd.y - segment.segmentStart.y;

        if (segment.segmentLength > 0.001f)
        {
            segment.inclineAngle = Mathf.Asin(heightDifference / segment.segmentLength) * Mathf.Rad2Deg;
        }
        else
        {
            segment.inclineAngle = 0f;
        }
    }

    public WaypointSegment GetSegmentAtPosition(Vector3 position)
    {
        if (analyzer.waypoints.Count == 0) return null;

        WaypointSegment closestSegment = null;
        float closestDistance = float.MaxValue;

        foreach (var segment in analyzer.waypoints)
        {
            Vector3 closestPoint = GetClosestPointOnSegment(position, segment.segmentStart, segment.segmentEnd);
            float distance = Vector3.Distance(position, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSegment = segment;
            }
        }

        return closestSegment;
    }

    private Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        direction.Normalize();

        float projection = Vector3.Dot(point - start, direction);
        projection = Mathf.Clamp(projection, 0f, length);

        return start + direction * projection;
    }
}