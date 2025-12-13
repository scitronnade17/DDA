using UnityEngine;
using System.Collections.Generic;

public class SpeedOptimizer
{
    private RoadAnalyzer analyzer;
    private Dictionary<int, float> segmentSpeeds;

    private float maxDeceleration = 4.0f;
    private float inclineAccelEffect = 0.3f;

    public SpeedOptimizer(RoadAnalyzer analyzer)
    {
        this.analyzer = analyzer;
        segmentSpeeds = new Dictionary<int, float>();
    }

    public void OptimizeAllSegments()
    {
        if (analyzer.waypoints == null || analyzer.waypoints.Count < 2)
            return;

        segmentSpeeds.Clear();

        for (int i = 0; i < analyzer.waypoints.Count; i++)
        {
            float originalSpeed = analyzer.waypoints[i].maxSafeSpeed / 3.6f;
            segmentSpeeds[i] = originalSpeed;
        }

        Optimize();

        ApplyOptimizedSpeeds();
    }

    private void Optimize()
    {
        int segmentCount = analyzer.waypoints.Count;

        for (int i = 0; i < segmentCount; i++)
        {
            int nextIndex = (i == segmentCount - 1) ? 0 : i + 1;
            WaypointSegment currentSeg = analyzer.waypoints[i];

            float currentSpeed = segmentSpeeds[i];
            float nextTargetSpeed = segmentSpeeds[nextIndex];
            float segmentLength = currentSeg.segmentLength;

            if (nextTargetSpeed < currentSpeed)
            {
                float minRequiredSpeed = CalculateRequiredInitialSpeed(
                    nextTargetSpeed,
                    segmentLength,
                    i
                );

                if (currentSpeed > minRequiredSpeed)
                {
                    segmentSpeeds[i] = Mathf.Max(minRequiredSpeed, nextTargetSpeed);
                }
            }
        }

        for (int iteration = 0; iteration < 3; iteration++)
        {
            bool changed = false;

            for (int i = 0; i < segmentCount; i++)
            {
                int prevIndex = (i == 0) ? segmentCount - 1 : i - 1;

                WaypointSegment prevSeg = analyzer.waypoints[prevIndex];
                WaypointSegment currentSeg = analyzer.waypoints[i];
               
                float safeSpeedLimit = currentSeg.maxSafeSpeed / 3.6f;
                if (segmentSpeeds[i] > safeSpeedLimit)
                {
                    segmentSpeeds[i] = safeSpeedLimit;
                    changed = true;
                }
            }

            if (!changed) break; 
        }
    }


    private float CalculateRequiredInitialSpeed(float requiredSpeed, float segmentLength, int segmentIndex)
    {
        if (segmentLength <= 0.1f)
            return requiredSpeed;

        WaypointSegment segment = analyzer.waypoints[segmentIndex];

        float deceleration = maxDeceleration;
        float inclineRad = segment.inclineAngle * Mathf.Deg2Rad;

        if (segment.inclineAngle < 0) 
            deceleration -= analyzer.gravity * Mathf.Sin(-inclineRad) * inclineAccelEffect;
        else if (segment.inclineAngle > 0) 
            deceleration += analyzer.gravity * Mathf.Sin(inclineRad) * inclineAccelEffect;

        deceleration = Mathf.Max(deceleration, 0.1f);

        float minSpeed = Mathf.Sqrt(
            Mathf.Max(0, requiredSpeed * requiredSpeed + 2 * deceleration * segmentLength)
        );

        float safeLimit = segment.maxSafeSpeed / 3.6f;
        return Mathf.Min(minSpeed, safeLimit);
    }

    private void ApplyOptimizedSpeeds()
    {
        for (int i = 0; i < analyzer.waypoints.Count; i++)
        {
            if (segmentSpeeds.ContainsKey(i))
            {
                float optimizedSpeed = segmentSpeeds[i] * 3.6f;
                float safeSpeed = analyzer.waypoints[i].maxSafeSpeed;

                analyzer.waypoints[i].optimizedSafeSpeed = Mathf.Min(optimizedSpeed, safeSpeed);

                if (analyzer.waypoints[i].optimizedSafeSpeed > 0)
                {
                    analyzer.waypoints[i].recommendedTime =
                        analyzer.waypoints[i].segmentLength / (analyzer.waypoints[i].optimizedSafeSpeed / 3.6f);
                }
            }
        }
    }

    public float GetOptimizedSpeedAtPosition(Vector3 position)
    {
        WaypointSegment segment = analyzer.GetSegmentAtPosition(position);
        if (segment != null && segment.optimizedSafeSpeed > 0)
        {
            return segment.optimizedSafeSpeed;
        }

        return segment?.maxSafeSpeed ?? 0f;
    }
}