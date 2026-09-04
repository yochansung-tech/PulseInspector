using System.Globalization;
using System.Windows;
using PulseInspector.Models;

namespace PulseInspector.Wpf.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(GroupDecisionPolicy policy, double confidence, double sampleIntervalSeconds, AppTheme theme)
    {
        InitializeComponent();
        ThemeBox.ItemsSource = new[] { "Light", "Dark" };
        ThemeBox.SelectedIndex = theme == AppTheme.Dark ? 1 : 0;
        RuleBox.ItemsSource = new[] { "Any defective subgroup", "Defective subgroup rate", "Maximum Mahalanobis" };
        RuleBox.SelectedIndex = (int)policy.Rule;
        RateBox.Text = policy.DefectiveSubgroupRateThreshold.ToString("G6", CultureInfo.InvariantCulture);
        ConfidenceBox.Text = confidence.ToString("G6", CultureInfo.InvariantCulture);
        IntervalBox.Text = sampleIntervalSeconds.ToString("G10", CultureInfo.InvariantCulture);
    }

    public GroupDecisionPolicy Policy { get; private set; } = new();
    public double Confidence { get; private set; }
    public double SampleIntervalSeconds { get; private set; }
    public AppTheme Theme { get; private set; } = AppTheme.Light;

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ThemeBox.SelectedIndex < 0) throw new InvalidOperationException("A theme is required.");
            if (RuleBox.SelectedIndex < 0) throw new InvalidOperationException("A decision rule is required.");
            var rate = Parse(RateBox.Text, "Defect rate threshold");
            var confidence = Parse(ConfidenceBox.Text, "Confidence");
            var interval = Parse(IntervalBox.Text, "Sample interval");
            var policy = new GroupDecisionPolicy((GroupDecisionRule)RuleBox.SelectedIndex, rate);
            policy.Validate();
            if (confidence <= 0 || confidence >= 1) throw new InvalidOperationException("Confidence must be between 0 and 1.");
            if (interval <= 0) throw new InvalidOperationException("Sample interval must be greater than zero.");
            Policy = policy;
            Confidence = confidence;
            SampleIntervalSeconds = interval;
            Theme = ThemeBox.SelectedIndex == 1 ? AppTheme.Dark : AppTheme.Light;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static double Parse(string text, string name)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            throw new InvalidOperationException($"{name} must be a valid finite number.");
        return value;
    }
}
