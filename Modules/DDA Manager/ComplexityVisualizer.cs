using UnityEngine;
public class ComplexityVisualizer : MonoBehaviour
{
    [Header("Display Settings")]
    public Vector2 windowSize = new Vector2(260, 90);
    public Vector2 windowPosition = new Vector2(243, -6);

    [Header("References")]
    public ComplexityCoordinator complexityCoordinator;

    private Texture2D backgroundTex;
    void Start()
    {
        backgroundTex = CreateTexture(new Color(0.1f, 0.1f, 0.1f, 0.3f));
    }

    Texture2D CreateTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (complexityCoordinator == null)
            return;

        GUIStyle windowStyle = new GUIStyle(GUI.skin.box);
        windowStyle.normal.background = backgroundTex;
        windowStyle.padding = new RectOffset(10, 10, 10, 10);

        Rect windowRect = new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y);
        GUILayout.BeginArea(windowRect, windowStyle);

        DisplayCurrentValues();

        GUILayout.EndArea();
    }

    void DisplayCurrentValues()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Overall:", GUILayout.Width(70));
        GUILayout.Label(complexityCoordinator.GetOverallComplexity().ToString("F3"));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Target:", GUILayout.Width(50));
        GUILayout.Label(complexityCoordinator.GetTargetComplexity().ToString("F3"));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Track:", GUILayout.Width(70));
        GUILayout.Label(complexityCoordinator.GetTrackComplexity().ToString("F3"));
        GUILayout.FlexibleSpace();
        GUILayout.Label("AI:", GUILayout.Width(50));
        GUILayout.Label(complexityCoordinator.GetAIComplexity().ToString("F3"));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Min:", GUILayout.Width(70));
        GUILayout.Label(complexityCoordinator.GetMinThreshold().ToString("F3"));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Max:", GUILayout.Width(50));
        GUILayout.Label(complexityCoordinator.GetMaxThreshold().ToString("F3"));
        GUILayout.EndHorizontal();


        GUILayout.Space(10);
        GUILayout.EndVertical();
    }
}