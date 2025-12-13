using UnityEngine;

public class SpeedCalculator
{
    private RoadAnalyzer analyzer;
    
    
    private float minRecommendedTime = 0.1f; 
    private float maxTimeMultiplier = 2.0f; 
    private float baseComplexityMultiplier = 1.0f;

    public SpeedCalculator(RoadAnalyzer analyzer)
    {
        this.analyzer = analyzer;
    }

    public void CalculateAllSafeSpeeds()
    {
        foreach (var segment in analyzer.waypoints)
        {
            segment.maxSafeSpeed = CalculateMaxSafeSpeed(segment);
            
            
            segment.recommendedTime = CalculateRecommendedTime(segment);
        }
    }

    public float CalculateMaxSafeSpeed(WaypointSegment segment)
    {
        float alpha = segment.inclineAngle;
        float mu = segment.frictionCoefficient;
        float r = segment.curveRadius;
        float w_obs = segment.obstacleWidth;
        float w = segment.roadWidth;

        if (r < 0.1f) r = 0.1f;
        if (w < 0.1f) w = 0.1f;
        if (mu < 0.01f) mu = 0.01f;

        float tanAlpha = Mathf.Tan(alpha);
        float numerator = tanAlpha + mu;
        float denominator = 1 - (mu * tanAlpha);

        if (denominator <= 0.001f || float.IsInfinity(denominator) || float.IsNaN(denominator))
        {
            denominator = 0.001f;
        }

        float baseSpeed = Mathf.Sqrt((numerator / denominator) * r * analyzer.gravity);
        float obstacleFactor = 1 - (w_obs / w);
        obstacleFactor = Mathf.Clamp(obstacleFactor, 0f, 1f);
        float finalSpeed = baseSpeed * obstacleFactor * 3.6f;
        float maxSpeed = 250f;

        return Mathf.Clamp(finalSpeed, 1f, maxSpeed);
    }
    
    
    public float CalculateRecommendedTime(WaypointSegment segment)
    {
        float baseTime = segment.maxSafeSpeed > 0 ? segment.segmentLength / (segment.maxSafeSpeed / 3.6f) : 0f;
        
        float complexityMultiplier = CalculateComplexityMultiplier(segment);
        float recommendedTime = baseTime * complexityMultiplier;
        
        return Mathf.Max(recommendedTime, minRecommendedTime);
    }
    
    
    private float CalculateComplexityMultiplier(WaypointSegment segment)
    {
        float multiplier = baseComplexityMultiplier;

        if (segment.curveRadius < 50f)
            multiplier *= 1.8f; 
        else if (segment.curveRadius < 100f)
            multiplier *= 1.5f; 
        else if (segment.curveRadius < 200f)
            multiplier *= 1.2f; 
        
        
        float inclineEffect = Mathf.Abs(segment.inclineAngle) / 45f; 
        multiplier *= 1.0f + inclineEffect * 0.4f; 
        
        
        float frictionEffect = Mathf.Clamp01(0.8f - segment.frictionCoefficient); 
        multiplier *= 1.0f + frictionEffect * 0.6f; 
        
        multiplier *= 1.0f + segment.obstaclePercent / 100f * 0.4f; 
           
        return Mathf.Clamp(multiplier, 0.8f, maxTimeMultiplier);
    }
}