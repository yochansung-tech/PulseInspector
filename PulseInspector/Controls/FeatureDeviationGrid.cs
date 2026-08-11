using System.Drawing;
using System.Windows.Forms;
using PulseInspector.Models;

namespace PulseInspector.Controls;

public sealed class FeatureDeviationGrid : UserControl
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        ReadOnly = true,
        AllowUserToAddRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };

    public FeatureDeviationGrid()
    {
        Dock = DockStyle.Fill;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Feature", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mean", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Std Dev", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Z-Score", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "|Z|", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contribution", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        Controls.Add(_grid);
    }

    public void SetResults(IEnumerable<FeatureDeviation> results)
    {
        _grid.Rows.Clear();
        foreach (var result in results)
        {
            _grid.Rows.Add(
                result.FeatureName,
                result.Value.ToString("G10"),
                result.Mean.ToString("G10"),
                result.StandardDeviation.ToString("G10"),
                result.ZScore.ToString("F4"),
                result.AbsoluteZScore.ToString("F4"),
                result.MahalanobisContribution.ToString("F6"));
        }
    }
}
