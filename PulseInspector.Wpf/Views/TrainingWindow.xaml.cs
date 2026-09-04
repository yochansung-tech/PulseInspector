using System.Windows;
using PulseInspector.Services;
using PulseInspector.Wpf.ViewModels;

namespace PulseInspector.Wpf.Views;

public partial class TrainingWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public TrainingWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        RefreshValidation();
    }

    private void Validate_Click(object sender, RoutedEventArgs e) => RefreshValidation();

    private void Train_Click(object sender, RoutedEventArgs e)
    {
        var validation = RefreshValidation();
        if (!validation.IsValid) return;
        _viewModel.TrainModel();
        StatusText.Text = _viewModel.StatusText;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private TrainingValidationResult RefreshValidation()
    {
        TrainingValidationResult validation;
        try
        {
            validation = _viewModel.ValidateTraining();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Validation failed: {ex.Message}";
            IssuesGrid.ItemsSource = new[] { new TrainingIssueRow("ERROR", "", ex.Message) };
            return new TrainingValidationResult
            {
                Issues = new[] { new TrainingValidationIssue("", "ERROR_EXCEPTION", ex.Message) }
            };
        }

        CountText.Text = validation.SampleCount.ToString();
        RequiredText.Text = _viewModel.RequiredTrainingGroupCount.ToString();
        ConfidenceText.Text = _viewModel.Confidence.ToString("G6");
        IssuesGrid.ItemsSource = validation.Issues.Select(i => new TrainingIssueRow(
            i.Code.StartsWith("ERROR_", StringComparison.Ordinal) ? "ERROR" : "WARNING",
            i.FeatureName,
            i.Message)).ToArray();
        StatusText.Text = validation.IsValid
            ? validation.HasWarnings ? "Validation passed with warnings." : "Validation passed."
            : "Validation failed. Resolve errors before training.";
        return validation;
    }

    private sealed record TrainingIssueRow(string Severity, string FeatureName, string Message);
}
