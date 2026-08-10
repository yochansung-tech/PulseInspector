using System.Drawing;
using System.Windows.Forms;
using PulseInspector.Models;

namespace PulseInspector.Controls;

public sealed class WaveformControl : Control
{
    private double[] _data = Array.Empty<double>();
    public WaveformControl() { Dock = DockStyle.Fill; BackColor = Color.White; DoubleBuffered = true; }
    public void SetData(IReadOnlyList<double> data) { _data = data.ToArray(); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.Clear(Color.White); if (_data.Length < 2) return;
        var min = _data.Min(); var max = _data.Max(); if (Math.Abs(max - min) < 1e-15) max = min + 1;
        using var pen = new Pen(Color.Navy, 1.5f); var points = new PointF[_data.Length];
        for (var i = 0; i < _data.Length; i++) points[i] = new PointF(i * (Width - 1f) / (_data.Length - 1), Height - 20 - (float)((_data[i] - min) / (max - min) * (Height - 40)));
        e.Graphics.DrawLines(pen, points); e.Graphics.DrawString("Waveform", Font, Brushes.Black, 8, 8);
    }
}
