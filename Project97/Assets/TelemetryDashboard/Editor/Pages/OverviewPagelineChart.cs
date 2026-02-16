using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class OverviewPagelineChart : VisualElement
{
    private float[] values;
    private DateTime[] times;

    private const int yTicks = 5;
    private const int xTicks = 5;

    private const float leftPadding = 40f;
    private const float bottomPadding = 22f;

    private readonly List<Label> yLabels = new();
    private readonly List<Label> xLabels = new();

    public OverviewPagelineChart(DateTime[] times, float[] data)
    {
        this.times = times;
        values = data;

        style.flexGrow = 1;
        style.height = 160;

        generateVisualContent += Draw;
        RegisterCallback<GeometryChangedEvent>(_ => RefreshLabels());
    }

    // ---------------- LABELS ----------------
    private void RefreshLabels()
    {
        foreach (var l in yLabels) Remove(l);
        foreach (var l in xLabels) Remove(l);

        yLabels.Clear();
        xLabels.Clear();

        if (values == null || values.Length == 0) return;

        float chartHeight = contentRect.height - bottomPadding;
        float chartWidth = contentRect.width - leftPadding;

        RefreshYLabels(chartHeight);
        RefreshXLabels(chartWidth, chartHeight);
    }

    private void RefreshYLabels(float chartHeight)
    {
        float min = Mathf.Min(values);
        float max = Mathf.Max(values);

        if (Mathf.Approximately(min, max))
            max = min + 1;

        float range = NiceNumber(max - min, false);
        float tickSpacing = NiceNumber(range / (yTicks - 1), true);

        float graphMin = Mathf.Floor(min / tickSpacing) * tickSpacing;
        float graphMax = Mathf.Ceil(max / tickSpacing) * tickSpacing;

        for (float yVal = graphMin; yVal <= graphMax + 0.001f; yVal += tickSpacing)
        {
            float t = (yVal - graphMin) / (graphMax - graphMin);
            float y = chartHeight - t * chartHeight;

            var label = new Label($"{yVal:0.##}");
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.top = y - 8;
            label.style.width = leftPadding - 4;
            label.style.fontSize = 10;
            label.style.unityTextAlign = TextAnchor.MiddleRight;

            Add(label);
            yLabels.Add(label);
        }
    }

    private void RefreshXLabels(float chartWidth, float chartHeight)
    {
        if (times == null || times.Length < 2) return;

        DateTime minT = times[0];
        DateTime maxT = times[times.Length - 1];

        TimeSpan total = maxT - minT;

        for (int i = 0; i < xTicks; i++)
        {
            float t = i / (float)(xTicks - 1);

            float x = leftPadding + t * chartWidth;

            DateTime tickTime = minT + TimeSpan.FromTicks((long)(total.Ticks * t));

            var label = new Label(tickTime.ToString("HH:mm")); // change format if you want date
            label.style.position = Position.Absolute;
            label.style.top = chartHeight + 2;
            label.style.left = x - 25;
            label.style.width = 50;
            label.style.fontSize = 10;
            label.style.unityTextAlign = TextAnchor.UpperCenter;

            Add(label);
            xLabels.Add(label);
        }
    }

    // ---------------- DRAW ----------------
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

        DateTime minT = times[0];
        DateTime maxT = times[times.Length - 1];
        double totalSeconds = (maxT - minT).TotalSeconds;

        // ---- horizontal grid ----
        painter.strokeColor = new Color(1, 1, 1, 0.15f);

        for (float yVal = graphMin; yVal <= graphMax + 0.001f; yVal += tickSpacing)
        {
            float y = chartHeight - ((yVal - graphMin) / (graphMax - graphMin)) * chartHeight;

            painter.BeginPath();
            painter.MoveTo(new Vector2(leftPadding, y));
            painter.LineTo(new Vector2(r.width, y));
            painter.Stroke();
        }

        // ---- vertical grid ----
        for (int i = 0; i < xTicks; i++)
        {
            float x = leftPadding + (i / (float)(xTicks - 1)) * chartWidth;

            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0));
            painter.LineTo(new Vector2(x, chartHeight));
            painter.Stroke();
        }

        // ---- line ----
        painter.strokeColor = Color.cyan;
        painter.lineWidth = 2;

        painter.BeginPath();

        for (int i = 0; i < values.Length; i++)
        {
            double seconds = (times[i] - minT).TotalSeconds;
            float tx = (float)(seconds / totalSeconds);

            float x = leftPadding + tx * chartWidth;

            float ty = (values[i] - graphMin) / (graphMax - graphMin);
            float y = chartHeight - ty * chartHeight;

            if (i == 0)
                painter.MoveTo(new Vector2(x, y));
            else
                painter.LineTo(new Vector2(x, y));
        }

        painter.Stroke();
    }

    // nice number helper
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
