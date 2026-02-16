using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CombatMechanics
{
    public static void Build(VisualElement parent)
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TelemetryDashboard/Editor/UXML/CombatMechanics.uxml");

        var page = uxml.CloneTree();
        parent.Add(page);

        //MOCK DATA
        float accuracy = 50f;
        int avgHpLeft = 34;
        float avgFightTime = 5f;
        float[] avgsatusFreqPerFight = { 1, 0, 1 };
        float[] mostUsedItemsFreqPerFight = { 3, 2 };
        string freqBoughtItem = "Health Potion";

        page.Q<Label>("accuracy").text = accuracy.ToString("0.0");
        page.Q<Label>("avgHpLeft").text = avgHpLeft.ToString("0");
        page.Q<Label>("avgFightTime").text = avgFightTime.ToString("0.0");
        page.Q<Label>("freqBoughtItem").text = freqBoughtItem;

        var heightChartContainer = page.Q<VisualElement>("height-chart");
        heightChartContainer.Add(new BarChart(avgsatusFreqPerFight, new string[] { "stunned", "bleeding", "guard_break" }));
    }
}
