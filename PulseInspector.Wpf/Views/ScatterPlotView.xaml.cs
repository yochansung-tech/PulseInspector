using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PulseInspector.Wpf.ViewModels;

namespace PulseInspector.Wpf.Views;

public partial class ScatterPlotView : UserControl
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IReadOnlyList<ChartPointViewModel>), typeof(ScatterPlotView),
        new FrameworkPropertyMetadata(Array.Empty<ChartPointViewModel>(), OnPointsChanged));

    public ScatterPlotView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Render();
    }

    public IReadOnlyList<ChartPointViewModel> Points
    {
        get => (IReadOnlyList<ChartPointViewModel>)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ScatterPlotView)d).Render();

    private void Render()
    {
        if (!IsInitialized) return;
        PlotCanvas.Children.Clear();
        var points = Points?.Where(p => double.IsFinite(p.X) && double.IsFinite(p.Y)).ToArray() ?? Array.Empty<ChartPointViewModel>();
        EmptyText.Visibility = points.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (points.Length == 0) return;

        var width = Math.Max(1, ActualWidth - 30);
        var height = Math.Max(1, ActualHeight - 30);
        var minX = points.Min(p => p.X); var maxX = points.Max(p => p.X);
        var minY = Math.Min(0, points.Min(p => p.Y)); var maxY = points.Max(p => p.Y);
        if (maxX <= minX) maxX = minX + 1;
        if (maxY <= minY) maxY = minY + 1;
        foreach (var point in points)
        {
            var x = 15 + (point.X - minX) / (maxX - minX) * width;
            var y = 15 + height - (point.Y - minY) / (maxY - minY) * height;
            var ellipse = new Ellipse { Width = 8, Height = 8, Fill = SystemColors.HighlightBrush, ToolTip = $"#{point.X:G0}: MD={point.Y:F6}" };
            Canvas.SetLeft(ellipse, x - 4); Canvas.SetTop(ellipse, y - 4);
            PlotCanvas.Children.Add(ellipse);
        }
    }
}
