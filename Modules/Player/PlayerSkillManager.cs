using UnityEngine;
using System.Collections.Generic;
using SpinMotion;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("Components")]
    public SkillMetricsController metricsController;
    public SegmentEvaluator segmentEvaluator;
    public ParameterAdjuster parameterAdjuster;
    public SkillDebugUI debugUI;

    private CarController carController;
    private RoadAnalyzer roadAnalyzer;

    void Start()
    {
        carController = GetComponent<CarController>();
        roadAnalyzer = FindObjectOfType<RoadAnalyzer>();

        if (carController == null || roadAnalyzer == null)
        {
            Debug.LogError("PlayerSkillManager: component not found!");
            enabled = false;
            return;
        }

        InitializeComponents();
    }

    void InitializeComponents()
    {
        if (metricsController == null)
            metricsController = gameObject.AddComponent<SkillMetricsController>();
        metricsController.Initialize(roadAnalyzer);

        if (segmentEvaluator == null)
            segmentEvaluator = gameObject.AddComponent<SegmentEvaluator>();
        segmentEvaluator.Initialize(roadAnalyzer, carController, metricsController);

        if (parameterAdjuster == null)
            parameterAdjuster = gameObject.AddComponent<ParameterAdjuster>();
        parameterAdjuster.Initialize(carController, metricsController);

        if (debugUI == null)
            debugUI = gameObject.AddComponent<SkillDebugUI>();
        debugUI.Initialize(metricsController);
    }

    void Update()
    {
        if (roadAnalyzer.waypoints == null || roadAnalyzer.waypoints.Count == 0)
            return;

        segmentEvaluator.UpdateEvaluation();

        if (segmentEvaluator.CurrentSegment != null)
        {
            parameterAdjuster.AdjustParameters(segmentEvaluator.CurrentSegment);
        }
    }

    public void RegisterCollision(GameObject collidedObject)
    {
        metricsController.RegisterCollision(collidedObject);
    }

    public float GetOverallSkill()
    {
        return metricsController.GetOverallSkill();
    }

    public SkillMetrics GetMetrics()
    {
        return metricsController.GetMetrics();
    }

    public void ResetAllMetrics()
    {
        metricsController.ResetAllMetrics();
        segmentEvaluator.ResetEvaluation();
    }
}