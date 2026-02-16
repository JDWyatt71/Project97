using System.IO;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public static class OverviewPage
{
    public static void Build(VisualElement parent)
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TelemetryDashboard/Editor/UXML/OverviewPage.uxml");

        var page = uxml.CloneTree();
        parent.Add(page);

        // ------MOCK AGGREGATE DATA (need to be replaced with python output) ------
        float avgLevelReached = 6f;
        float avgAccuracy = 0.67f;
        float avgSessionDuration = 1.3f;

        float[] sessionsOverTime = { 3, 5, 4, 7, 10, 8, 12, 4, 8, 15};

        DateTime startTime = DateTime.Now.Date.AddHours(8); // Start at 8:00 AM today
        int numPoints = 10;
        TimeSpan interval = TimeSpan.FromMinutes(30);
        DateTime[] times = new DateTime[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            times[i] = startTime + TimeSpan.FromMinutes(i * interval.TotalMinutes);
        }

        float[] moveHeightDist = { 42, 38, 20 }; //Low, Mid, High

        // -------KPIs---------
        page.Q<Label>("avg-level").text = avgLevelReached.ToString("0.0");
        page.Q<Label>("avg-accuracy").text = (avgAccuracy * 100f).ToString("0") + "%";
        page.Q<Label>("avg-session-duration").text = avgSessionDuration.ToString("0.0") + " hours";

        // ------CHARTS-----------
        var sessionsChartContainer = page.Q<VisualElement>("chart");
        sessionsChartContainer.Add(new OverviewPagelineChart(times ,sessionsOverTime));

        var heightChartContainer = page.Q<VisualElement>("height-chart");
        heightChartContainer.Add(new BarChart(moveHeightDist, new string[] { "low", "Mid", "High" }));
    }
}
