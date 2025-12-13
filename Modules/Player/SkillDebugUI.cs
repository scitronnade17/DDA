using UnityEngine;

public class SkillDebugUI : MonoBehaviour
{
    [Header("Visual sttings")]
    public Vector2 screenPosition = new Vector2(10, 10);
    public int fontSize = 16;
    private Color goodSkillColor = Color.green;
    private Color mediumSkillColor = Color.yellow;
    private Color poorSkillColor = Color.red;

    private SkillMetricsController metricsController;

    public void Initialize(SkillMetricsController metrics)
    {
        metricsController = metrics;
    }

    void OnGUI()
    {
        if (metricsController == null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = fontSize;
        style.normal.textColor = GetSkillColor(metricsController.GetOverallSkill());

        GUI.Label(new Rect(screenPosition.x, screenPosition.y, 300, 30),
                 $"Skill level: {metricsController.GetOverallSkill():F3}", style);

    }

    Color GetSkillColor(float skill)
    {
        if (skill >= 0.7f) return goodSkillColor;
        if (skill >= 0.4f) return mediumSkillColor;
        return poorSkillColor;
    }
}