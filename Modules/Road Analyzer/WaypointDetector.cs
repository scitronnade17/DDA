using UnityEngine;
using System.Linq;

public class WaypointDetector
{
    private RoadAnalyzer analyzer;

    public WaypointDetector(RoadAnalyzer analyzer)
    {
        this.analyzer = analyzer;
    }

    public void DetectWaypoints()
    {
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag(analyzer.waypointTag);

        if (waypointObjects.Length == 0)
        {
            Debug.LogWarning($"Not found any objects with tag '{analyzer.waypointTag}'");
            return;
        }

        waypointObjects = waypointObjects
            .OrderBy(wp => ExtractNumberFromName(wp.name))
            .ThenBy(wp => wp.name)
            .ToArray();

        foreach (var wp in waypointObjects)
        {
            WaypointSegment segment = new WaypointSegment
            {
                waypoint = wp.transform,
                roadWidth = analyzer.defaultRoadWidth,
                segmentLength = analyzer.defaultSegmentLength,
                frictionCoefficient = analyzer.defaultFriction
            };
            analyzer.waypoints.Add(segment);
        }
    }

    private int ExtractNumberFromName(string name)
    {
        string number = System.Text.RegularExpressions.Regex.Match(name, @"\d+").Value;

        if (int.TryParse(number, out int result))
        {
            return result;
        }

        return -1;
    }
}