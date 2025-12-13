using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SpinMotion;

[System.Serializable]
public class CorrelationMetrics
{
    public float steeringCorrelation = 0f;
    public float speedCorrelation = 0f;
    public float trajectoryCorrelation = 0f;
    public float overallCorrelation = 0f;
    public int optimalTimeShift = 0;
    public float confidence = 0f;
    public float distanceFactor = 1f;
    public float directionAlignment = 1f;
}

[System.Serializable]
public class MatchRecord
{
    public float timestamp;
    public float playerSkill;
    public float aiCorrelation;
    public float matchScore;
    public float distance;
    public float directionDot;

    public MatchRecord(float time, float skill, float correlation, float dist, float dirDot)
    {
        timestamp = time;
        playerSkill = skill;
        aiCorrelation = correlation;
        matchScore = CalculateMatchScore(skill, correlation);
        distance = dist;
        directionDot = dirDot;
    }

    private float CalculateMatchScore(float skill, float correlation)
    {
        float difference = Mathf.Abs(skill - correlation);
        return Mathf.Clamp01(1.0f - difference);
    }
}

public class CrossCorrelationSystem : MonoBehaviour
{
    private int analysisWindow = 100;
    private float sampleRate = 0.1f;
    private int maxTimeShift = 20;
    private float analysisUpdateInterval = 2.0f;
    private float maxValidDistance = 50f;

    private CorrelationMetrics currentMetrics;
    private List<float> correlationHistory = new List<float>();

    private float currentPlayerSkill = 0f;
    private float currentAICorrelation = 0f;
    private float currentDistance = 0f;
    private float currentDirectionAlignment = 1f;

    private PlayerSkillManager playerSkillManager;
    private CarController playerCar;
    private CarController aiCar;


    private List<Vector3> playerPositions = new List<Vector3>();
    private List<Vector3> aiPositions = new List<Vector3>();
    private List<float> playerSpeeds = new List<float>();
    private List<float> aiSpeeds = new List<float>();
    private List<float> playerSteering = new List<float>();
    private List<float> aiSteering = new List<float>();

    private List<Vector3> playerDirections = new List<Vector3>();
    private List<Vector3> aiDirections = new List<Vector3>();

    private List<MatchRecord> matchHistory = new List<MatchRecord>();

    private float nextSampleTime = 0f;
    private float nextAnalysisTime = 0f;

    private GUIStyle guiStyle;

    void Start()
    {
        InitializeSystem();
        InitializeGUI();
    }

    void InitializeSystem()
    {
        FindAndLinkComponents();

        if (playerSkillManager == null)
        {
            Debug.LogWarning("CrossCorrelationSystem: PlayerSkillManager not found!");
        }

        if (playerCar == null || aiCar == null)
        {
            FindCarsInScene();
        }
    }

    void FindAndLinkComponents()
    {
        playerSkillManager = FindObjectOfType<PlayerSkillManager>();

        FindCarsInScene();
    }

    void FindCarsInScene()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null && playerSkillManager != null)
        {
            playerObj = playerSkillManager.gameObject;
        }

        if (playerObj != null)
            playerCar = playerObj.GetComponent<CarController>();

        CarController[] allCars = FindObjectsOfType<CarController>();
        foreach (var car in allCars)
        {
            if (car != playerCar)
            {
                aiCar = car;
                break;
            }
        }
    }

    void InitializeGUI()
    {
        guiStyle = new GUIStyle();
        guiStyle.fontSize = 20;
        guiStyle.normal.textColor = Color.white;
        guiStyle.fontStyle = FontStyle.Bold;
        guiStyle.alignment = TextAnchor.UpperLeft;
    }

    void Update()
    {
        if (playerCar == null || aiCar == null)
        {
            InitializeSystem();
            return;
        }

        if (Time.time >= nextSampleTime)
        {
            CollectCorrelationData();
            nextSampleTime = Time.time + sampleRate;
        }

        if (playerPositions.Count >= analysisWindow)
        {
            PerformCorrelationAnalysis();
        }

        if (playerSkillManager != null && Time.time >= nextAnalysisTime)
        {
            PerformIntegrationAnalysis();
            nextAnalysisTime = Time.time + analysisUpdateInterval;
        }
    }

    void CollectCorrelationData()
    {
        playerPositions.Add(playerCar.transform.position);
        aiPositions.Add(aiCar.transform.position);

        playerSpeeds.Add(playerCar.CurrentSpeed);
        aiSpeeds.Add(aiCar.CurrentSpeed);

        playerSteering.Add(GetSteeringAngle(playerCar));
        aiSteering.Add(GetSteeringAngle(aiCar));

        if (playerPositions.Count >= 2)
        {
            Vector3 playerDir = (playerPositions[playerPositions.Count - 1] -
                               playerPositions[playerPositions.Count - 2]).normalized;
            playerDirections.Add(playerDir);

            Vector3 aiDir = (aiPositions[aiPositions.Count - 1] -
                           aiPositions[aiPositions.Count - 2]).normalized;
            aiDirections.Add(aiDir);
        }
        else
        {
            playerDirections.Add(playerCar.transform.forward);
            aiDirections.Add(aiCar.transform.forward);
        }

        if (playerPositions.Count > analysisWindow * 2)
        {
            int removeCount = playerPositions.Count - analysisWindow;

            playerPositions.RemoveRange(0, removeCount);
            aiPositions.RemoveRange(0, removeCount);
            playerSpeeds.RemoveRange(0, removeCount);
            aiSpeeds.RemoveRange(0, removeCount);
            playerSteering.RemoveRange(0, removeCount);
            aiSteering.RemoveRange(0, removeCount);

            if (playerDirections.Count > removeCount)
                playerDirections.RemoveRange(0, removeCount);
            if (aiDirections.Count > removeCount)
                aiDirections.RemoveRange(0, removeCount);
        }
    }

    float GetSteeringAngle(CarController car)
    {
        if (car.m_MaximumSteerAngle > 0)
            return Mathf.Clamp(car.CurrentSteerAngle / car.m_MaximumSteerAngle, -1f, 1f);
        return 0f;
    }

    void PerformCorrelationAnalysis()
    {
        if (playerPositions.Count < analysisWindow || aiPositions.Count < analysisWindow)
            return;

        int startIndex = Mathf.Max(0, playerPositions.Count - analysisWindow);
        int count = Mathf.Min(analysisWindow, playerPositions.Count - startIndex);

        CalculateCurrentDistanceAndDirection(startIndex, count);

        float distanceFactor = CalculateDistanceFactor();
        float directionFactor = CalculateDirectionFactor(startIndex, count);

        float[] pTrajX, aTrajX;
        PrepareTrajectoryData(startIndex, count, out pTrajX, out aTrajX);

        float[] pSteer = new float[count];
        float[] aSteer = new float[count];
        float[] pSpeed = new float[count];
        float[] aSpeed = new float[count];

        for (int i = 0; i < count; i++)
        {
            int idx = startIndex + i;
            pSteer[i] = playerSteering[idx];
            aSteer[i] = aiSteering[idx];
            pSpeed[i] = playerSpeeds[idx];
            aSpeed[i] = aiSpeeds[idx];
        }

        float steerCorr = CalculateCrossCorrelation(pSteer, aSteer, out int steerShift);
        float speedCorr = CalculateCrossCorrelation(pSpeed, aSpeed, out int speedShift);

        float rawTrajCorr = CalculateCrossCorrelation(pTrajX, aTrajX, out int trajShift);
        float trajCorr = rawTrajCorr * distanceFactor * directionFactor;

        currentMetrics = new CorrelationMetrics
        {
            steeringCorrelation = steerCorr,
            speedCorrelation = speedCorr,
            trajectoryCorrelation = trajCorr,
            overallCorrelation = (steerCorr + speedCorr + trajCorr) / 3f,
            optimalTimeShift = (steerShift + speedShift + trajShift) / 3,
            distanceFactor = distanceFactor,
            directionAlignment = directionFactor
        };

        currentMetrics.confidence = CalculateConfidence(count);

        correlationHistory.Add(currentMetrics.overallCorrelation);
        if (correlationHistory.Count > 100)
            correlationHistory.RemoveAt(0);
    }

    void CalculateCurrentDistanceAndDirection(int startIndex, int count)
    {
        currentDistance = Vector3.Distance(
            playerPositions.Last(),
            aiPositions.Last()
        );

        if (playerDirections.Count > 0 && aiDirections.Count > 0)
        {
            currentDirectionAlignment = Vector3.Dot(
                playerDirections.Last(),
                aiDirections.Last()
            );
        }
        else
        {
            currentDirectionAlignment = Vector3.Dot(
                playerCar.transform.forward,
                aiCar.transform.forward
            );
        }
    }

    float CalculateDistanceFactor()
    {
        if (currentDistance <= maxValidDistance)
        {
            return Mathf.Clamp01(1f - (currentDistance / maxValidDistance));
        }
        return 0f;
    }

    float CalculateDirectionFactor(int startIndex, int count)
    {
        if (playerDirections.Count < count || aiDirections.Count < count)
            return 1f;

        float totalAlignment = 0f;
        int validSamples = 0;

        for (int i = 0; i < count; i++)
        {
            int idx = startIndex + i;
            if (idx < playerDirections.Count && idx < aiDirections.Count)
            {
                float alignment = Vector3.Dot(playerDirections[idx], aiDirections[idx]);
                totalAlignment += alignment;
                validSamples++;
            }
        }

        if (validSamples == 0) return 1f;

        float averageAlignment = totalAlignment / validSamples;

        return Mathf.Clamp01((averageAlignment + 1f) / 2f);
    }

    void PrepareTrajectoryData(int startIndex, int count, out float[] pTrajX, out float[] aTrajX)
    {
        pTrajX = new float[count];
        aTrajX = new float[count];

        for (int i = 0; i < count; i++)
        {
            int idx = startIndex + i;

            pTrajX[i] = playerPositions[idx].x;
            aTrajX[i] = aiPositions[idx].x;
        }
    }

    float CalculateCrossCorrelation(float[] x, float[] y, out int bestShift)
    {
        bestShift = 0;
        if (x.Length == 0 || y.Length == 0 || x.Length != y.Length)
            return 0f;

        float bestCorr = -1f;
        int n = x.Length;

        float[] xNorm = NormalizeForPearsonCorrelation(x);
        float[] yNorm = NormalizeForPearsonCorrelation(y);

        for (int shift = -maxTimeShift; shift <= maxTimeShift; shift++)
        {
            float sum = 0f;
            int valid = 0;

            for (int i = 0; i < n; i++)
            {
                int j = i + shift;
                if (j >= 0 && j < n)
                {
                    sum += xNorm[i] * yNorm[j];
                    valid++;
                }
            }

            if (valid > 0)
            {
                float corr = sum / Mathf.Max(valid - 1, 1); 
                if (corr > bestCorr) 
                {
                    bestCorr = corr;
                    bestShift = shift;
                }
            }
        }

        return Mathf.Clamp(bestCorr, -1f, 1f);
    }

    float[] NormalizeForPearsonCorrelation(float[] data)
    {
        if (data.Length < 2)
            return data;

        float mean = 0f;
        foreach (float val in data)
            mean += val;
        mean /= data.Length;

        float sumSqDiff = 0f;
        foreach (float val in data)
        {
            float diff = val - mean;
            sumSqDiff += diff * diff;
        }
        float stdDev = Mathf.Sqrt(sumSqDiff / (data.Length - 1));

        if (stdDev < 0.0001f) 
            return new float[data.Length];

        float[] normalized = new float[data.Length];
        for (int i = 0; i < data.Length; i++)
            normalized[i] = (data[i] - mean) / stdDev;

        return normalized;
    }

    float CalculateConfidence(int sampleCount)
    {
        float sampleConfidence = Mathf.Clamp01((float)sampleCount / analysisWindow);

        float stability = 1f;
        if (correlationHistory.Count >= 10)
        {
            float sumDiff = 0f;
            for (int i = 1; i < Mathf.Min(10, correlationHistory.Count); i++)
            {
                sumDiff += Mathf.Abs(correlationHistory[i] - correlationHistory[i - 1]);
            }
            stability = Mathf.Clamp01(1f - sumDiff / 10f);
        }

        float distanceConfidence = Mathf.Clamp01(1f - (currentDistance / maxValidDistance));

        return sampleConfidence * stability * distanceConfidence;
    }

    void PerformIntegrationAnalysis()
    {
        if (playerSkillManager == null)
            return;

        currentPlayerSkill = playerSkillManager.GetOverallSkill();

        currentAICorrelation = GetOverallCorrelation();

        matchHistory.Add(new MatchRecord(
            Time.time,
            currentPlayerSkill,
            currentAICorrelation,
            currentDistance,
            currentDirectionAlignment
        ));

        if (matchHistory.Count > 100)
        {
            matchHistory.RemoveAt(0);
        }
    }

    public float GetOverallCorrelation()
    {
        if (currentMetrics != null)
            return currentMetrics.overallCorrelation;
        return 0f;
    }

    public void ResetHistory()
    {
        matchHistory.Clear();
        correlationHistory.Clear();
    }
}