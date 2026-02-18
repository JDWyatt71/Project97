using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ProgressionBalance
{
    public static void Build(VisualElement parent)
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TelemetryDashboard/Editor/UXML/Progression.uxml");

        var page = uxml.CloneTree();
        parent.Add(page);
    }
}
