using UnityEngine;
using System.Collections.Generic;

public class RoadSkillAssessor
{
    private Dictionary<string, float> parameterWeights = new Dictionary<string, float>()
    {
        { "frictionCoefficient", 0.25f },
        { "curveRadius", 0.25f },
        { "roadWidth", 0.15f },
        { "inclineAngle", 0.1f },
        { "obstaclePercent", 0.25f }
    };

    public float AssessDifficultyLevel(WaypointSegment currentSegment, List<WaypointSegment> allSegments)
    {
        if (allSegments.Count == 0) return 0.5f;

        float totalSkill = 0f;

        foreach (var param in parameterWeights)
        {
            float normalizedValue = NormalizeParameter(param.Key, currentSegment, allSegments);
            totalSkill += normalizedValue * param.Value;
        }
        //Debug.Log($"{currentSegment.waypoint.name}: " + $"Skill needed = {totalSkill:F2}");
        return totalSkill;
    }

    private float NormalizeParameter(string parameterName, WaypointSegment segment, List<WaypointSegment> allSegments)
    {
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        foreach (var seg in allSegments)
        {
            float value = GetParameterValue(seg, parameterName);
            minValue = Mathf.Min(minValue, value);
            maxValue = Mathf.Max(maxValue, value);
        }

        if (Mathf.Approximately(maxValue, minValue)) return 0.5f;

        float currentValue = GetParameterValue(segment, parameterName);
        return Mathf.Clamp01((currentValue - minValue) / (maxValue - minValue));
    }

    private float GetParameterValue(WaypointSegment segment, string parameterName)
    {
        switch (parameterName)
        {
            case "frictionCoefficient":
                return 1 - segment.frictionCoefficient;
            case "curveRadius":
                return 1 / segment.curveRadius;
            case "roadWidth":
                return 1 / segment.roadWidth;
            case "inclineAngle":
                return Mathf.Abs(segment.inclineAngle);
            case "obstaclePercent":
                return segment.obstaclePercent;
            default:
                return 0f;
        }
    }
}