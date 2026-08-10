using System.Drawing;
using System.Windows.Forms;

namespace PulseInspector.Controls;

public sealed class HistogramControl : Control
{
    private double[] _data = Array.Empty<double>();
    public HistogramControl() { Dock = DockStyle.Fill; BackColor = Color.White; DoubleBuffered = true; }
    public void SetData(IEnumerable<double> data) { _data = data.ToArray(); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.Clear(Color.White); if (_data.Length == 0) return;
        var min = _data.Min(); var max = _data.Max(); if (max == min) max = min + 1;
        var bins = new int[20]; foreach (var v in _data) { var b = Math.Clamp((int)((v - min) / (max - min) * bins.Length), 0, bins.Length - 1); bins[b]++; }
        var maxCount = bins.Max(); if (maxCount == 0) return; var w = Math.Max(1f, (Width - 20f) / bins.Length);
        for (var i = 0; i < bins.Length; i++) { var h = (Height - 30f) * bins[i] / maxCount; e.Graphics.FillRectangle(Brushes.SteelBlue, 10 + i * w, Height - 20 - h, w - 1, h); }
    }
}
