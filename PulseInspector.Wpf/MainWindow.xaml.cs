using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using PulseInspector.Wpf.ViewModels;
using PulseInspector.Wpf.Views;

namespace PulseInspector.Wpf;

public partial class MainWindow : Window
{
    private string? _lastSortMember;
    private ListSortDirection _lastSortDirection = ListSortDirection.Ascending;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.WaveformChanged += OnWaveformChanged;
        OnWaveformChanged(ViewModel, EventArgs.Empty);
    }

    private void AddGroup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Multiselect = true, Title = "Select waveforms belonging to one group" };
        if (dialog.ShowDialog(this) == true) ViewModel?.AddGroup(dialog.FileNames);
    }

    private void AddRows_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Multiselect = true, Title = "Select CSV files (one row = one subgroup)" };
        if (dialog.ShowDialog(this) == true) ViewModel?.AddGroupsFromRows(dialog.FileNames);
    }

    private void Training_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        new TrainingWindow(ViewModel) { Owner = this }.ShowDialog();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        var window = new SettingsWindow(ViewModel.DecisionPolicy, ViewModel.Confidence, ViewModel.SampleIntervalSeconds) { Owner = this };
        if (window.ShowDialog() == true) ViewModel.ApplySettings(window.Policy, window.Confidence, window.SampleIntervalSeconds);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void Subgroups_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (e.Column.SortMemberPath is not { Length: > 0 } member) return;
        e.Handled = true;
        var direction = _lastSortMember == member && _lastSortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        _lastSortMember = member;
        _lastSortDirection = direction;

        var view = CollectionViewSource.GetDefaultCollectionView(ViewModel?.Subgroups);
        if (view is null) return;
        using (view.DeferRefresh())
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(member, direction));
            if (!string.Equals(member, nameof(SubgroupResultViewModel.Index), StringComparison.Ordinal))
                view.SortDescriptions.Add(new SortDescription(nameof(SubgroupResultViewModel.Index), ListSortDirection.Ascending));
        }
    }

    private void OnWaveformChanged(object? sender, EventArgs e) => WaveformView.SetData(ViewModel?.Waveform);

    protected override void OnClosed(EventArgs e)
    {
        if (ViewModel is not null) ViewModel.WaveformChanged -= OnWaveformChanged;
        base.OnClosed(e);
    }
}
