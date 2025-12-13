using SpinMotion;
using UnityEngine;

public class ParameterAdjuster : MonoBehaviour
{
    [Header("Correction settings")]
    public float adjustmentSpeed = 2f;
    public bool enableSteeringAdjustment = true;
    public bool enableTorqueAdjustment = true;
    public bool enableBrakeAdjustment = true;

    [Header("Paramater settings")]
    public float minSteerAngle = 15f;
    public float maxSteerAngle = 30f;
    public float minTorque = 100f;
    public float maxTorque = 1000f;
    public float minBrakeTorque = 100f;
    public float maxBrakeTorque = 500f;

    public ComplexityCoordinator complexityCoordinator;

    private CarController carController;
    private SkillMetricsController metricsController;

    public void Initialize(CarController car, SkillMetricsController metrics)
    {
        carController = car;
        metricsController = metrics;
        complexityCoordinator = FindObjectOfType<ComplexityCoordinator>();
        if (complexityCoordinator == null)
            Debug.LogWarning("ComplexityCoordinator not found!");
    }

    public void AdjustParameters(WaypointSegment currentSegment)
    {
        if (carController == null || currentSegment == null) return;

        float k = metricsController.GetOverallSkill();
        float v = currentSegment.optimizedSafeSpeed;
        float v_player = carController.CurrentSpeed;

        float deltaP = (1f - k) * ((v - v_player) / Mathf.Max(v, 0.1f));

        ApplyAdjustments(deltaP);
    }

    void ApplyAdjustments(float deltaP)
    {
        float complexityFactor = 1f;
        if (complexityCoordinator != null)
        {
            complexityFactor = complexityCoordinator.GetAlpha();
        }

        float effectiveSpeed = adjustmentSpeed * complexityFactor;

        float smoothDelta = Mathf.Clamp(deltaP * adjustmentSpeed * Time.deltaTime, -0.1f, 0.1f);

        if (enableSteeringAdjustment)
        {
            AdjustSteering(smoothDelta);
        }

        if (enableTorqueAdjustment)
        {
            AdjustTorque(smoothDelta);
        }

        if (enableBrakeAdjustment)
        {
            AdjustBrakes(smoothDelta);
        }
    }

      void AdjustSteering(float deltaP)
    {
        if (carController.m_MaximumSteerAngle <= 0) return;

        float steerMultiplier = 1f + deltaP * 0.5f;
        carController.m_MaximumSteerAngle = Mathf.Clamp(
            carController.m_MaximumSteerAngle * steerMultiplier,
            minSteerAngle, maxSteerAngle);
    }

    void AdjustTorque(float deltaP)
    {
        if (carController.m_FullTorqueOverAllWheels <= 0) return;

        float torqueMultiplier = 1f + deltaP * 0.3f;
        carController.m_FullTorqueOverAllWheels = Mathf.Clamp(
            carController.m_FullTorqueOverAllWheels * torqueMultiplier,
            minTorque, maxTorque);
    }

    void AdjustBrakes(float deltaP)
    {
        if (carController.m_BrakeTorque <= 0) return;

        float brakeMultiplier = 1f - deltaP * 0.2f;
        carController.m_BrakeTorque = Mathf.Clamp(
            carController.m_BrakeTorque * brakeMultiplier,
            minBrakeTorque, maxBrakeTorque);
    }
}