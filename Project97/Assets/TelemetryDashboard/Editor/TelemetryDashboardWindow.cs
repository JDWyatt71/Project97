using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class TelemetryDashboardWindow : EditorWindow
{
    [MenuItem("Tools/Telemetry Dashboard")]
    public static void Open()
    {
        var window = GetWindow<TelemetryDashboardWindow>();
        window.titleContent = new GUIContent("Telemetry Dashboard");
    }

    private VisualElement contentContainer;

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var dashboardUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TelemetryDashboard/Editor/UXML/TelemetryDashboard.uxml");

        root.Add(dashboardUxml.CloneTree());

        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/TelemetryDashboard/Editor/USS/Dashboard.uss"));
        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/TelemetryDashboard/Editor/USS/Table.uss"));
        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/TelemetryDashboard/Editor/USS/OverviewPage.uss"));

        contentContainer = root.Q<VisualElement>(null, "content");

        var overviewBtn = root.Q<Button>("overview-btn");
        var runsBtn = root.Q<Button>("runs-btn");
        var combatBtn = root.Q<Button>("combat-btn");
        var progressionBtn = root.Q<Button>("progression-btn");
        var dropOffBtn = root.Q<Button>("dropOff-btn");

        overviewBtn.clicked += ShowOverview;
        runsBtn.clicked += ShowRuns;
        combatBtn.clicked += ShowCombat;
        progressionBtn.clicked += ShowProgression;
        dropOffBtn.clicked += ShowDropOff;

        ShowOverview();
    }

    void ShowOverview()
    {
        contentContainer.Clear();
        OverviewPage.Build(contentContainer);
    }

    void ShowRuns()
    {
        contentContainer.Clear();
        RunsPage.Build(contentContainer);
    }
    void ShowCombat()
    {
        contentContainer.Clear();
        CombatMechanics.Build(contentContainer);
    }

    void ShowProgression()
    {
        contentContainer.Clear();
        ProgressionBalance.Build(contentContainer);
    }

    void ShowDropOff()
    {
        contentContainer.Clear();
        FunnelDropOff.Build(contentContainer);
    }
}
