using System.Drawing;
using System.Windows.Forms;

namespace PulseInspector.Controls;

public sealed class ScatterPlotControl : Control
{
    private readonly List<(double X, double Y)> _points = new();
    public ScatterPlotControl() { Dock = DockStyle.Fill; BackColor = Color.White; DoubleBuffered = true; }
    public void SetPoints(IEnumerable<(double X, double Y)> points) { _points.Clear(); _points.AddRange(points); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.Clear(Color.White); if (_points.Count == 0) return;
        var minX = _points.Min(p => p.X); var maxX = _points.Max(p => p.X); var minY = _points.Min(p => p.Y); var maxY = _points.Max(p => p.Y);
        if (maxX == minX) maxX = minX + 1; if (maxY == minY) maxY = minY + 1;
        foreach (var p in _points) { var x = 20 + (float)((p.X - minX) / (maxX - minX) * (Width - 40)); var y = Height - 20 - (float)((p.Y - minY) / (maxY - minY) * (Height - 40)); e.Graphics.FillEllipse(Brushes.DarkBlue, x - 3, y - 3, 6, 6); }
    }
}
