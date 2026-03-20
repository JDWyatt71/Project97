using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public static class RunsPage
{
    public static void Build(VisualElement parent)
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TelemetryDashboard/Editor/UXML/RunsPage.uxml");

        var page = uxml.CloneTree();
        parent.Add(page);

        //MOCK DATA AGAIN
        int totalRuns = 64;
        int completedRuns = 36;
        int failedRuns = 28;
        float runsDuration = 0.75f;

        page.Q<Label>("totalRunsLabel").text = totalRuns.ToString("0.0");
        page.Q<Label>("completedRunsLabel").text = completedRuns.ToString("0.0");
        page.Q<Label>("failedRunsLabel").text = failedRuns.ToString("0.0");
        page.Q<Label>("avgDurationLabel").text = runsDuration.ToString("0.00") + " hours";

        var pieChartContainer = page.Q<VisualElement>("pieChartContainer");

        var pieChart = new PieChart
        {
            style =
            {
                width = 250,
                height = 250
            }
        };

        pieChart.SetData(new List<PieChart.Slice>
        {
            new PieChart.Slice(completedRuns, Color.green),
            new PieChart.Slice(failedRuns, Color.red)
        });

        pieChartContainer.Add(pieChart);
    }
}
