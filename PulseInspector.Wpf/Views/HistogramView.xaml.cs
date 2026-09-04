using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PulseInspector.Wpf.Views;

public partial class HistogramView : UserControl
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<double>), typeof(HistogramView),
        new FrameworkPropertyMetadata(Array.Empty<double>(), FrameworkPropertyMetadataOptions.AffectsRender, OnValuesChanged));

    public HistogramView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Render();
    }

    public IReadOnlyList<double> Values
    {
        get => (IReadOnlyList<double>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HistogramView)d).Render();

    private void Render()
    {
        if (!IsInitialized) return;
        PlotCanvas.Children.Clear();
        var values = Values?.Where(double.IsFinite).ToArray() ?? Array.Empty<double>();
        EmptyText.Visibility = values.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (values.Length == 0) return;

        var width = Math.Max(1, ActualWidth - 20);
        var height = Math.Max(1, ActualHeight - 20);
        var min = values.Min();
        var max = values.Max();
        var bins = Math.Clamp((int)Math.Sqrt(values.Length), 5, 20);
        var range = max - min;
        if (range <= 0 || !double.IsFinite(range)) range = 1;
        var counts = new int[bins];
        foreach (var value in values)
        {
            var index = (int)((value - min) / range * bins);
            index = Math.Clamp(index, 0, bins - 1);
            counts[index]++;
        }

        var maxCount = Math.Max(1, counts.Max());
        var barWidth = width / bins;
        var chartBrush = (Brush)FindResource("ChartBrush");
        for (var i = 0; i < bins; i++)
        {
            var barHeight = height * counts[i] / maxCount;
            var bar = new Rectangle { Width = Math.Max(1, barWidth - 3), Height = barHeight, Fill = chartBrush };
            Canvas.SetLeft(bar, 10 + i * barWidth);
            Canvas.SetTop(bar, height - barHeight + 10);
            PlotCanvas.Children.Add(bar);
        }
    }
}
