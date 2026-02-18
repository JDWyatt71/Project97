using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class BarChart : VisualElement
{
    private float[] values;
    private string[] labels;

    private const int yTicks = 5;
    private const float leftPadding = 40f;
    private const float barWidth = 40f;
    private const float spacing = 16f;

    private readonly List<Label> yLabels = new();

    public BarChart(float[] values, string[] labels)
    {
        this.values = values;
        this.labels = labels;

        style.height = 120;

        generateVisualContent += DrawGrid;

        RegisterCallback<GeometryChangedEvent>(_ => Rebuild());
    }

    private void Rebuild()
    {
        Clear();

        DrawBars();
        RefreshYLabels();
    }

    private void DrawBars()
    {
        float h = contentRect.height;

        float max = Mathf.Max(values);

        float range = NiceNumber(max, false);
        float tickSpacing = NiceNumber(range / (yTicks - 1), true);
        float graphMax = Mathf.Ceil(max / tickSpacing) * tickSpacing;

        for (int i = 0; i < values.Length; i++)
        {
            float x = leftPadding + i * (barWidth + spacing);
            float barHeight = (values[i] / graphMax) * h;

            var bar = new VisualElement();
            bar.style.position = Position.Absolute;
            bar.style.left = x;
            bar.style.bottom = 0;
            bar.style.width = barWidth;
            bar.style.height = barHeight;
            bar.style.backgroundColor = new Color(0.4f, 0.7f, 1f);

            bar.tooltip = $"{labels[i]}: {values[i]}";

            Add(bar);

            var label = new Label(labels[i]);
            label.style.position = Position.Absolute;
            label.style.left = x;
            label.style.bottom = -18;
            label.style.width = barWidth;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.fontSize = 10;

            Add(label);
        }
    }

    private void DrawGrid(MeshGenerationContext ctx)
    {
        if (values == null || values.Length == 0) return;

        var painter = ctx.painter2D;

        float h = contentRect.height;

        float max = Mathf.Max(values);

        float range = NiceNumber(max, false);
        float tickSpacing = NiceNumber(range / (yTicks - 1), true);
        float graphMax = Mathf.Ceil(max / tickSpacing) * tickSpacing;

        painter.strokeColor = new Color(1, 1, 1, 0.15f);

        for (float yVal = 0; yVal <= graphMax + 0.001f; yVal += tickSpacing)
        {
            float y = (yVal / graphMax) * h;

            painter.BeginPath();
            painter.MoveTo(new Vector2(leftPadding, h - y));
            painter.LineTo(new Vector2(contentRect.width, h - y));
            painter.Stroke();
        }
    }

    private void RefreshYLabels()
    {
        float h = contentRect.height;

        float max = Mathf.Max(values);

        float range = NiceNumber(max, false);
        float tickSpacing = NiceNumber(range / (yTicks - 1), true);
        float graphMax = Mathf.Ceil(max / tickSpacing) * tickSpacing;

        for (float yVal = 0; yVal <= graphMax + 0.001f; yVal += tickSpacing)
        {
            float y = h - (yVal / graphMax) * h;

            var label = new Label($"{yVal:0.##}");
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.top = y - 8;
            label.style.width = leftPadding - 4;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.fontSize = 10;

            Add(label);
            yLabels.Add(label);
        }
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
