using UnityEngine;
using SpinMotion;

public class ComplexityCoordinator : MonoBehaviour
{
    [Header("Complexity Progression")]
    [Range(0f, 1f)] public float startComplexity = 0.3f;
    [Range(0f, 1f)] public float endComplexity = 0.7f;
    public float complexityGrowthRate = 0.001f;
    [Range(0.1f, 0.3f)] public float complexityRange = 0.2f;

    private float targetComplexity;
    private float currentTargetMinS;
    private float currentTargetMaxS;

    [Header("Adaptation Settings")]
    [Range(0f, 1f)] public float initialAlpha = 0.5f;
    public float adjustmentSpeed = 0.5f;

    private float alpha;
    private float trackComplexity = 0.5f;
    private float aiComplexity = 0.5f;
    private float overallComplexity = 0.5f;
    private float currentMinS;
    private float currentMaxS;

    private CarController playerCarController;
    private RoadAnalyzer roadAnalyzer;
    private ParameterAdjuster parameterAdjuster;
    private AIAdaptiveSystem aiAdaptiveSystem;
    private SkillMetricsController skillMetrics;

    void Start()
    {
        alpha = initialAlpha;
        targetComplexity = startComplexity;

        UpdateTargetThresholds();
        currentMinS = currentTargetMinS;
        currentMaxS = currentTargetMaxS;

        FindReferences();
    }

    void FindReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerCarController == null)
            playerCarController = player.GetComponent<CarController>();

        if (roadAnalyzer == null)
            roadAnalyzer = FindObjectOfType<RoadAnalyzer>();

        if (parameterAdjuster == null)
            parameterAdjuster = FindObjectOfType<ParameterAdjuster>();

        if (aiAdaptiveSystem == null)
            aiAdaptiveSystem = FindObjectOfType<AIAdaptiveSystem>();

        if (skillMetrics == null)
            skillMetrics = FindObjectOfType<SkillMetricsController>();
    }

    void Update()
    {
        if (playerCarController == null || roadAnalyzer == null)
        {
            FindReferences();
            return;
        }

        targetComplexity = Mathf.Min(endComplexity,
            targetComplexity + complexityGrowthRate * Time.deltaTime);

        UpdateTargetThresholds();

        currentMinS = Mathf.Lerp(currentMinS, currentTargetMinS, adjustmentSpeed * Time.deltaTime);
        currentMaxS = Mathf.Lerp(currentMaxS, currentTargetMaxS, adjustmentSpeed * Time.deltaTime);

        UpdateComplexityMetrics();
    }

    void UpdateTargetThresholds()
    {
        currentTargetMinS = Mathf.Clamp(targetComplexity - complexityRange / 2f, 0f, 1f);
        currentTargetMaxS = Mathf.Clamp(targetComplexity + complexityRange / 2f, 0f, 1f);
    }

    void UpdateComplexityMetrics()
    {
        if (parameterAdjuster == null || aiAdaptiveSystem == null ||
            skillMetrics == null || playerCarController == null ||
            roadAnalyzer == null)
            return;

        WaypointSegment currentSegment = GetCurrentSegment();
        if (currentSegment == null)
            return;

        float deltaP = CalculateDeltaP(currentSegment, playerCarController);
        trackComplexity = Mathf.Abs(deltaP);

        aiComplexity = CalculateAIComplexity();

        UpdateAlphaBasedOnSkill();

        overallComplexity = alpha * trackComplexity + (1 - alpha) * aiComplexity;

        AdjustComplexityToTarget();
    }

    private WaypointSegment GetCurrentSegment()
    {
        if (roadAnalyzer == null || playerCarController == null)
            return null;

        return roadAnalyzer.GetSegmentAtPosition(playerCarController.transform.position);
    }

    private float CalculateDeltaP(WaypointSegment segment, CarController playerCar)
    {
        if (segment == null || playerCar == null)
            return 0f;

        float k = skillMetrics.GetOverallSkill();
        float v = segment.optimizedSafeSpeed;
        float v_player = playerCar.CurrentSpeed;

        float deltaP = (1f - k) * ((v - v_player) / Mathf.Max(v, 0.1f));

        return Mathf.Clamp(deltaP, -1f, 1f);
    }

    private float CalculateAIComplexity()
    {
        if (aiAdaptiveSystem == null)
            return 0.5f;

        float losingChromosomeFrequency = aiAdaptiveSystem.GetLosingChromosomeFrequency();

        return Mathf.Lerp(0.3f, 0.7f, losingChromosomeFrequency);
    }

    private void UpdateAlphaBasedOnSkill()
    {
        float skill = skillMetrics.GetOverallSkill();

        float skillBasedAlpha = Mathf.Lerp(0.9f, 0.1f, skill);
        skillBasedAlpha = Mathf.Clamp(skillBasedAlpha, 0.1f, 0.9f);

        alpha = Mathf.Lerp(alpha, skillBasedAlpha, adjustmentSpeed * Time.deltaTime);
    }

    private void AdjustComplexityToTarget()
    {
        float complexityDifference = overallComplexity - targetComplexity;
        float absoluteDifference = Mathf.Abs(complexityDifference);

        if (overallComplexity < currentMinS)
        {
            IncreaseComplexity(absoluteDifference);
        }
        else if (overallComplexity > currentMaxS)
        {
            DecreaseComplexity(absoluteDifference);
        }
        else
        {
            MaintainOptimalComplexity();
        }
    }

    private void IncreaseComplexity(float difference)
    {
        float trackContribution = alpha * trackComplexity;
        float aiContribution = (1 - alpha) * aiComplexity;

        float adjustmentFactor = Mathf.Clamp(difference * 2f, 0.1f, 1f);

        if (trackContribution < aiContribution)
        {
            alpha = Mathf.Min(alpha + adjustmentSpeed * adjustmentFactor * Time.deltaTime, 0.9f);
        }
        else
        {
            alpha = Mathf.Max(alpha - adjustmentSpeed * adjustmentFactor * Time.deltaTime, 0.1f);
        }
    }

    private void DecreaseComplexity(float difference)
    {
        float trackContribution = alpha * trackComplexity;
        float aiContribution = (1 - alpha) * aiComplexity;

        float adjustmentFactor = Mathf.Clamp(difference * 2f, 0.1f, 1f);

        if (trackContribution > aiContribution)
        {

            alpha = Mathf.Max(alpha - adjustmentSpeed * adjustmentFactor * Time.deltaTime, 0.1f);
        }
        else
        {
            alpha = Mathf.Min(alpha + adjustmentSpeed * adjustmentFactor * Time.deltaTime, 0.9f);
        }
    }

    private void MaintainOptimalComplexity()
    {
        float randomVariation = Random.Range(-0.005f, 0.005f);
        alpha = Mathf.Clamp(alpha + randomVariation, 0.1f, 0.9f);
    }


    public float GetAlpha() { return alpha; }
    public float GetTrackComplexity() { return trackComplexity; }
    public float GetAIComplexity() { return aiComplexity; }
    public float GetOverallComplexity() { return overallComplexity; }
    public float GetMinThreshold() { return currentMinS; }
    public float GetMaxThreshold() { return currentMaxS; }
    public float GetTargetComplexity() { return targetComplexity; }
}