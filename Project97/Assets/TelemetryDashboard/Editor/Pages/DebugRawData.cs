using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Debug
{
    public static void Build(VisualElement parent)
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/TelemetryDashboard/Editor/UXML/DebugRawData.uxml");

        var page = uxml.CloneTree();
        parent.Add(page);
    }
}
