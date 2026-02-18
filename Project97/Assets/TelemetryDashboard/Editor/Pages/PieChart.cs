using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PieChart : VisualElement
{
    public class Slice
    {
        public float Value;
        public Color Color;

        public Slice(float value, Color color)
        {
            Value = value;
            Color = color;
        }
    }

    private List<Slice> _slices = new();

    public void SetData(List<Slice> slices)
    {
        _slices = slices;
        MarkDirtyRepaint();
    }

    public PieChart()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (_slices == null || _slices.Count == 0)
            return;

        float total = 0f;
        foreach (var slice in _slices)
            total += Mathf.Max(0, slice.Value);

        if (total <= 0f)
            return;

        var painter = ctx.painter2D;

        Vector2 center = contentRect.center;
        float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.5f;

        float startAngle = 0f;

        foreach (var slice in _slices)
        {
            float percent = slice.Value / total;
            float angle = percent * 360f;

            painter.fillColor = slice.Color;
            painter.BeginPath();

            painter.MoveTo(center);

            const int segments = 64;
            for (int i = 0; i <= segments; i++)
            {
                float t = Mathf.Lerp(startAngle, startAngle + angle, i / (float)segments);
                float rad = Mathf.Deg2Rad * t;

                Vector2 point = center + new Vector2(
                    Mathf.Cos(rad),
                    Mathf.Sin(rad)
                ) * radius;

                painter.LineTo(point);
            }

            painter.ClosePath();
            painter.Fill();

            startAngle += angle;
        }
    }
}
