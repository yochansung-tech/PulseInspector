using System.Globalization;
using System.Windows.Forms;
using PulseInspector.Models;
using PulseInspector.Services;

namespace PulseInspector.Forms;

public sealed partial class MainForm
{
    private readonly InspectionCsvExporter _inspectionCsvExporter = new();

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        AddExportMenuItem();
    }

    private void AddExportMenuItem()
    {
        if (MainMenuStrip is null || MainMenuStrip.Items.Cast<ToolStripItem>().Any(i => string.Equals(i.Text, "Export", StringComparison.Ordinal)))
            return;

        var export = new ToolStripMenuItem("Export");
        var exportCsv = new ToolStripMenuItem("Export Inspection Result to CSV...");
        exportCsv.Click += (_, _) => ExportInspectionResultCsv();
        export.DropDownItems.Add(exportCsv);
        MainMenuStrip.Items.Add(export);
    }

    private void ExportInspectionResultCsv()
    {
        var groupIndex = _groupList.SelectedIndex;
        if (groupIndex < 0 || groupIndex >= _groups.Count)
        {
            MessageBox.Show(this, "Select an inspected group first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_model is null || _subgroupResults.Count == 0)
        {
            MessageBox.Show(this, "Run 'Inspect Selected Group' before exporting an inspection result.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var group = _groups[groupIndex];
            var result = _groupService.Inspect(group, _model);
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = $"InspectionResult_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Inspection Result"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _inspectionCsvExporter.ExportGroupResult(dialog.FileName, result, _subgroupResults);
            _status.SetState(true, $"Inspection result exported: {dialog.FileName}");
            MessageBox.Show(this, $"Inspection result was exported successfully.\n\n{dialog.FileName}", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _status.SetState(false, "Inspection result export failed");
        }
    }
}
