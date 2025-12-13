using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SkillMetrics
{
    public float overallSkill = 0.5f;
    public float[] segmentSkills;
    public int totalCollisions = 0;
    public int opponentCollisions = 0;
    public int evaluatedSegments = 0;
    public float totalSegmentTime = 0f;
}

public class SkillMetricsController : MonoBehaviour
{
    private float collisionWeight = 0.45f;
    private float speedWeight = 0.2f;
    private float timeWeight = 0.35f;

    [Header("Collision settings")]
    public LayerMask opponentLayers;
    public LayerMask obstacleLayers;
    public LayerMask wallLayers;
    private int maxAllowedCollisions = 1;
    private float opponentMultiplier = 1.5f;

    [HideInInspector] public float speedTolerance = 0.15f;

    private SkillMetrics metrics = new SkillMetrics();
    private RoadAnalyzer roadAnalyzer;

    public void Initialize(RoadAnalyzer analyzer)
    {
        roadAnalyzer = analyzer;

        if (roadAnalyzer.waypoints != null)
        {
            metrics.segmentSkills = new float[roadAnalyzer.waypoints.Count];
            ResetSegmentSkills();
        }
    }

    void ResetSegmentSkills()
    {
        for (int i = 0; i < metrics.segmentSkills.Length; i++)
        {
            metrics.segmentSkills[i] = 0.5f;
        }
    }

    public void RegisterCollision(GameObject collidedObject)
    {
        if (collidedObject == null) return;

        int objectLayer = collidedObject.layer;
        bool isOpponent = ((1 << objectLayer) & opponentLayers.value) != 0;
        bool isObstacle = ((1 << objectLayer) & obstacleLayers.value) != 0;
        bool isWall = ((1 << objectLayer) & wallLayers.value) != 0;

        if (isOpponent || isObstacle || isWall)
        {
            metrics.totalCollisions++;

            if (isOpponent)
            {
                metrics.opponentCollisions++;
            }
        }
    }

    public void AddSegmentTime(float elapsedTime, float recommendedTime)
    {
        if (recommendedTime <= 0) return;

        metrics.totalSegmentTime += elapsedTime;
    }


    public void EvaluateSegment(int segmentIndex, float speedScore, float timeScore)
    {
        if (segmentIndex < 0 || segmentIndex >= metrics.segmentSkills.Length)
            return;

        float collisionScore = CalculateCollisionScore();

        float segmentSkill = (collisionScore * collisionWeight) +
                           (speedScore * speedWeight) +
                           (timeScore * timeWeight);

        if (metrics.totalCollisions > 0 && timeScore < 0.5f)
        {
            segmentSkill *= 0.5f;
        }

        metrics.segmentSkills[segmentIndex] = Mathf.Clamp01(segmentSkill);
        metrics.evaluatedSegments++;

        //Debug.Log($"Segment {segmentIndex}: C={collisionScore:F2}*{collisionWeight:F2}={collisionScore * collisionWeight:F2}, " +
        //  $"S={speedScore:F2}*{speedWeight:F2}={speedScore * speedWeight:F2}, " +
        //  $"T={timeScore:F2}*{timeWeight:F2}={timeScore * timeWeight:F2}, " +
        //  $"Total={segmentSkill:F2}, Clamped={metrics.segmentSkills[segmentIndex]:F2}");

        UpdateOverallSkill();

        metrics.totalCollisions = 0;
        metrics.opponentCollisions = 0;
    }

    float CalculateCollisionScore()
    {
        if (maxAllowedCollisions <= 0) return 1f;
        if (metrics.totalCollisions >= maxAllowedCollisions) return 0f;
        float effectiveCollisions = metrics.totalCollisions +
                                  (metrics.opponentCollisions * (opponentMultiplier - 1f));

        return Mathf.Clamp01(1f - (effectiveCollisions / maxAllowedCollisions));
    }

    void UpdateOverallSkill()
    {
        if (metrics.evaluatedSegments == 0) return;

        float totalSkill = 0f;
        int count = Mathf.Min(metrics.evaluatedSegments, metrics.segmentSkills.Length);

        float penaltyFactor = 0.3f;

        for (int i = 0; i < count; i++)
        {
            float segmentScore = metrics.segmentSkills[i];

            if (segmentScore < 0.5f)
            {
                totalSkill += segmentScore * penaltyFactor;
            }
            else
            {
                totalSkill += segmentScore;
            }
        }

        metrics.overallSkill = totalSkill / count;
    }

    public SkillMetrics GetMetrics() { return metrics; }
    public float GetOverallSkill() { return metrics.overallSkill; }

    public void ResetAllMetrics()
    {
        metrics = new SkillMetrics();
        if (roadAnalyzer.waypoints != null)
        {
            metrics.segmentSkills = new float[roadAnalyzer.waypoints.Count];
            ResetSegmentSkills();
        }
    }
}