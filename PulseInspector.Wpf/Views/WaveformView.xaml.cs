using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PulseInspector.Wpf.Views;

public partial class WaveformView : UserControl
{
    private IReadOnlyList<double> _samples = Array.Empty<double>();

    public WaveformView() => InitializeComponent();

    public void SetData(IReadOnlyList<double>? samples)
    {
        _samples = samples ?? Array.Empty<double>();
        Render();
    }

    private void PlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Render();

    private void Render()
    {
        PlotCanvas.Children.Clear();
        var values = _samples;
        EmptyText.Visibility = values.Count < 2 ? Visibility.Visible : Visibility.Collapsed;
        if (values.Count < 2 || PlotCanvas.ActualWidth < 10 || PlotCanvas.ActualHeight < 10) return;

        var min = values.Min();
        var max = values.Max();
        var span = max - min;
        if (!double.IsFinite(span) || span <= 0) span = 1;
        var width = PlotCanvas.ActualWidth;
        var height = PlotCanvas.ActualHeight;
        var margin = 18.0;
        var points = new PointCollection();
        var count = values.Count;
        for (var i = 0; i < count; i++)
        {
            var x = margin + (width - 2 * margin) * i / (count - 1);
            var y = margin + (height - 2 * margin) * (1.0 - (values[i] - min) / span);
            points.Add(new Point(x, y));
        }

        var polyline = new Polyline
        {
            Points = points,
            Stroke = SystemColors.HighlightBrush,
            StrokeThickness = 1.5,
            SnapsToDevicePixels = true
        };
        PlotCanvas.Children.Add(polyline);
    }
}
