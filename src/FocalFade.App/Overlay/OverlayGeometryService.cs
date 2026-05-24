using System.Windows;
using System.Windows.Media;

namespace FocalFade.Overlay;

public static class OverlayGeometryService
{
    public static Geometry CreateDimmingGeometry(Rect monitorBounds, List<Rect> focusRects, double margin, double cornerRadius)
    {
        var group = new GeometryGroup { FillRule = FillRule.EvenOdd };

        // Full monitor rectangle (the dimming area)
        var monitorGeometry = CreateRoundedRect(monitorBounds, 0);
        group.Children.Add(monitorGeometry);

        // Cut holes for focused windows
        foreach (var focusRect in focusRects)
        {
            // Expand focus rect by margin
            var expandedRect = new Rect(
                Math.Max(focusRect.X - margin, monitorBounds.X),
                Math.Max(focusRect.Y - margin, monitorBounds.Y),
                Math.Min(focusRect.Width + margin * 2, monitorBounds.Width),
                Math.Min(focusRect.Height + margin * 2, monitorBounds.Height));

            // Clip to monitor bounds
            expandedRect.Intersect(monitorBounds);

            if (expandedRect.Width > 0 && expandedRect.Height > 0)
            {
                var holeGeometry = CreateRoundedRect(expandedRect, cornerRadius);
                group.Children.Add(holeGeometry);
            }
        }

        return group;
    }

    public static Geometry CreateBorderGeometry(Rect focusRect, double margin, double cornerRadius)
    {
        var expandedRect = new Rect(
            focusRect.X - margin,
            focusRect.Y - margin,
            focusRect.Width + margin * 2,
            focusRect.Height + margin * 2);

        return CreateRoundedRect(expandedRect, cornerRadius);
    }

    private static Geometry CreateRoundedRect(Rect rect, double cornerRadius)
    {
        if (cornerRadius <= 0)
            return new RectangleGeometry(rect);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            double x = rect.X;
            double y = rect.Y;
            double w = rect.Width;
            double h = rect.Height;
            double r = Math.Min(cornerRadius, Math.Min(w / 2, h / 2));

            ctx.BeginFigure(new Point(x + r, y), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(x + w - r, y), isStroked: false, isSmoothJoin: false);
            ctx.ArcTo(new Point(x + w, y + r), new Size(r, r), 0, false, SweepDirection.Clockwise, isStroked: false, isSmoothJoin: false);
            ctx.LineTo(new Point(x + w, y + h - r), isStroked: false, isSmoothJoin: false);
            ctx.ArcTo(new Point(x + w - r, y + h), new Size(r, r), 0, false, SweepDirection.Clockwise, isStroked: false, isSmoothJoin: false);
            ctx.LineTo(new Point(x + r, y + h), isStroked: false, isSmoothJoin: false);
            ctx.ArcTo(new Point(x, y + h - r), new Size(r, r), 0, false, SweepDirection.Clockwise, isStroked: false, isSmoothJoin: false);
            ctx.LineTo(new Point(x, y + r), isStroked: false, isSmoothJoin: false);
            ctx.ArcTo(new Point(x + r, y), new Size(r, r), 0, false, SweepDirection.Clockwise, isStroked: false, isSmoothJoin: false);
        }

        geometry.Freeze();
        return geometry;
    }

    public static List<Rect> GetIntersectionsWithMonitor(Rect monitorBounds, List<Rect> focusRects)
    {
        var result = new List<Rect>();
        foreach (var rect in focusRects)
        {
            var intersection = rect;
            intersection.Intersect(monitorBounds);
            if (intersection.Width > 0 && intersection.Height > 0)
                result.Add(intersection);
        }
        return result;
    }
}
