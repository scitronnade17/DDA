using SpinMotion;
using UnityEngine;

public class SegmentEvaluator : MonoBehaviour
{
    [Header("Time Evaluation Settings")]
    public float maxTimeFactor = 2.0f;

    private RoadAnalyzer roadAnalyzer;
    private CarController carController;
    private SkillMetricsController metricsController;

    private WaypointSegment currentSegment;
    private int currentSegmentIndex = -1;
    private bool isEvaluating = false;
    private float segmentMaxSpeed = 0f;
    private float segmentStartTime;
    private float segmentElapsedTime = 0f;

    public WaypointSegment CurrentSegment { get { return currentSegment; } }
    public int CurrentSegmentIndex { get { return currentSegmentIndex; } }
    public float SegmentElapsedTime { get { return segmentElapsedTime; } }

    public void Initialize(RoadAnalyzer analyzer, CarController car, SkillMetricsController metrics)
    {
        roadAnalyzer = analyzer;
        carController = car;
        metricsController = metrics;
    }

    public void UpdateEvaluation()
    {
        if (roadAnalyzer.waypoints == null) return;

        DetectCurrentSegment();

        if (isEvaluating && currentSegment != null)
        {
            segmentElapsedTime = Time.time - segmentStartTime;
            EvaluateCurrentSegment();
        }
    }

    void DetectCurrentSegment()
    {
        Vector3 position = transform.position;
        WaypointSegment nearestSegment = roadAnalyzer.GetSegmentAtPosition(position);

        if (nearestSegment != null)
        {
            int index = roadAnalyzer.waypoints.IndexOf(nearestSegment);

            if (index != currentSegmentIndex)
            {
                if (isEvaluating)
                {
                    CompleteSegmentEvaluation();
                }

                StartSegmentEvaluation(nearestSegment, index);
            }
        }
    }

    void StartSegmentEvaluation(WaypointSegment segment, int index)
    {
        currentSegment = segment;
        currentSegmentIndex = index;
        segmentMaxSpeed = 0f;
        segmentStartTime = Time.time;
        segmentElapsedTime = 0f;
        isEvaluating = true;
    }

    void EvaluateCurrentSegment()
    {
        float currentSpeed = carController.CurrentSpeed;
        if (currentSpeed > segmentMaxSpeed)
        {
            segmentMaxSpeed = currentSpeed;
        }
    }

    void CompleteSegmentEvaluation()
    {
        if (currentSegment == null || currentSegmentIndex < 0) return;

        float speedScore = CalculateSpeedScore();
        float timeScore = CalculateTimeScore();

        metricsController.EvaluateSegment(currentSegmentIndex, speedScore, timeScore);
        metricsController.AddSegmentTime(segmentElapsedTime, currentSegment.recommendedTime);

        ResetSegmentData();
    }

    float CalculateSpeedScore()
    {
        if (currentSegment.optimizedSafeSpeed <= 0) return 1f;

        float speedRatio = segmentMaxSpeed / currentSegment.optimizedSafeSpeed;

        float speedDifference = Mathf.Abs(1f - speedRatio);

        if (speedDifference <= metricsController.speedTolerance)
            return 1f;

        return Mathf.Clamp01(1f - (speedDifference - metricsController.speedTolerance));
    }

    float CalculateTimeScore()
    {
        if (currentSegment.recommendedTime <= 0)
            return 1f;

        float actualTime = segmentElapsedTime;
        float targetTime = currentSegment.recommendedTime;


        if (actualTime <= targetTime)
            return 1.0f;


        float timeRatio = actualTime / targetTime;


        if (timeRatio <= maxTimeFactor * 0.6f) 
        {
            return Mathf.Clamp01(1f - (timeRatio - 1f) * 2f); 
        }
        else if (timeRatio <= maxTimeFactor * 0.75f)
        {
            return Mathf.Clamp01(0.6f - (timeRatio - 1.2f) * 2f);
        }
        else if (timeRatio <= maxTimeFactor)
        {
            return Mathf.Clamp01(0.1f * (1f - (timeRatio - 1.5f) / (maxTimeFactor - 1.5f)));
        }


        return 0f;
    }

    void ResetSegmentData()
    {
        currentSegment = null;
        currentSegmentIndex = -1;
        isEvaluating = false;
        segmentMaxSpeed = 0f;
        segmentElapsedTime = 0f;
    }

    public void ResetEvaluation()
    {
        ResetSegmentData();
    }

}