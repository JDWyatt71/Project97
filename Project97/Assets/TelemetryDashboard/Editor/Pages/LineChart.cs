using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class LineChart : VisualElement
{
    private float[] values;

    private const int yTicks = 5;
    private const float leftPadding = 40f;
    private const float bottomPadding = 10f;

    private readonly List<Label> yLabels = new();

    public LineChart(float[] data)
    {
        values = data;

        style.flexGrow = 1;
        style.height = 140;

        generateVisualContent += Draw;
        RegisterCallback<GeometryChangedEvent>(_ => RefreshLabels());
    }

    private void RefreshLabels()
    {
        foreach (var l in yLabels)
            Remove(l);

        yLabels.Clear();

        if (values == null || values.Length == 0) return;

        float min = Mathf.Min(values);
        float max = Mathf.Max(values);

        if (Mathf.Approximately(min, max))
            max = min + 1;

        float range = NiceNumber(max - min, false);
        float tickSpacing = NiceNumber(range / (yTicks - 1), true);
        float graphMin = Mathf.Floor(min / tickSpacing) * tickSpacing;
        float graphMax = Mathf.Ceil(max / tickSpacing) * tickSpacing;

        float chartHeight = contentRect.height - bottomPadding;

        for (float yVal = graphMin; yVal <= graphMax + 0.001f; yVal += tickSpacing)
        {
            float t = (yVal - graphMin) / (graphMax - graphMin);
            float y = chartHeight - t * chartHeight;

            var label = new Label($"{yVal:0.##}");
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.top = y - 8;
            label.style.fontSize = 10;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.width = leftPadding - 4;

            Add(label);
            yLabels.Add(label);
        }
    }

    private void Draw(MeshGenerationContext ctx)
    {
        if (values == null || values.Length < 2) return;

        var painter = ctx.painter2D;

        Rect r = contentRect;

        float chartWidth = r.width - leftPadding;
        float chartHeight = r.height - bottomPadding;

        float min = Mathf.Min(values);
        float max = Mathf.Max(values);

        if (Mathf.Approximately(min, max))
            max = min + 1;

        float range = NiceNumber(max - min, false);
        float tickSpacing = NiceNumber(range / (yTicks - 1), true);
        float graphMin = Mathf.Floor(min / tickSpacing) * tickSpacing;
        float graphMax = Mathf.Ceil(max / tickSpacing) * tickSpacing;

        painter.strokeColor = new Color(1, 1, 1, 0.15f);
        painter.lineWidth = 1;

        for (float yVal = graphMin; yVal <= graphMax + 0.001f; yVal += tickSpacing)
        {
            float t = (yVal - graphMin) / (graphMax - graphMin);
            float y = chartHeight - t * chartHeight;

            painter.BeginPath();
            painter.MoveTo(new Vector2(leftPadding, y));
            painter.LineTo(new Vector2(r.width, y));
            painter.Stroke();
        }

        painter.strokeColor = Color.cyan;
        painter.lineWidth = 1;

        float stepX = chartWidth / (values.Length - 1);

        painter.BeginPath();

        for (int i = 0; i < values.Length; i++)
        {
            float t = (values[i] - graphMin) / (graphMax - graphMin);

            float x = leftPadding + stepX * i;
            float y = chartHeight - t * chartHeight;

            if (i == 0)
                painter.MoveTo(new Vector2(x, y));
            else
                painter.LineTo(new Vector2(x, y));
        }

        painter.Stroke();
    }

    private float NiceNumber(float range, bool round)
    {
        float exponent = Mathf.Floor(Mathf.Log10(range));
        float fraction = range / Mathf.Pow(10, exponent);

        float niceFraction;

        if (round)
        {
            if (fraction < 1.5f) niceFraction = 1;
            else if (fraction < 3) niceFraction = 2;
            else if (fraction < 7) niceFraction = 5;
            else niceFraction = 10;
        }
        else
        {
            if (fraction <= 1) niceFraction = 1;
            else if (fraction <= 2) niceFraction = 2;
            else if (fraction <= 5) niceFraction = 5;
            else niceFraction = 10;
        }

        return niceFraction * Mathf.Pow(10, exponent);
    }
}
