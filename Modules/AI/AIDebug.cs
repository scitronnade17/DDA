using UnityEngine;

public class AIDebug : MonoBehaviour
{
    private AIAdaptiveSystem aiAdaptiveSystem;
    private CrossCorrelationSystem correlationSystem;

    private GUIStyle labelStyle;
    private GUIStyle valueStyle;

    void Start()
    {
        InitializeComponents();
        InitializeGUI();
    }

    void InitializeComponents()
    {
        aiAdaptiveSystem = FindObjectOfType<AIAdaptiveSystem>();
        correlationSystem = FindObjectOfType<CrossCorrelationSystem>();

        if (aiAdaptiveSystem == null)
            Debug.LogWarning("AIDebug: AIAdaptiveSystem not found!");
        if (correlationSystem == null)
            Debug.LogWarning("AIDebug: CrossCorrelationSystem not found!");
    }

    void InitializeGUI()
    {
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.yellow;
        labelStyle.fontStyle = FontStyle.Bold;

        valueStyle = new GUIStyle();
        valueStyle.fontSize = 14;
        valueStyle.normal.textColor = Color.white;
        valueStyle.fontStyle = FontStyle.Bold;
    }

    void OnGUI()
    {
        if (aiAdaptiveSystem == null && correlationSystem == null)
        {
            GUI.Label(new Rect(20, 20, 300, 30), "Debug systems not found!", labelStyle);
            return;
        }

        int y = 20;
        int lineHeight = 30;

        if (aiAdaptiveSystem != null)
        {
            GUI.Label(new Rect(20, y, 200, 30), "AI Adaptive System:", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(40, y, 400, 30),
                     $"Active Chromosome: {aiAdaptiveSystem.GetActiveChromosomeType()}", valueStyle);
            y += lineHeight;
        }

        if (correlationSystem != null)
        {
            GUI.Label(new Rect(20, y, 200, 30), "Cross Correlation:", labelStyle);
            y += lineHeight;

            float correlation = correlationSystem.GetOverallCorrelation();
            GUI.Label(new Rect(40, y, 400, 30),
                     $"Correlation: {correlation:F3}", valueStyle);
            y += lineHeight;
        }
    }
}