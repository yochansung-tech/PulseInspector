using System.Drawing;
using System.Windows.Forms;
using PulseInspector.Models;

namespace PulseInspector.Controls;

public sealed class FeatureGrid : UserControl
{
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };
    public FeatureGrid()
    {
        Dock = DockStyle.Fill; _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Feature", Width = 170 }); _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }); Controls.Add(_grid);
    }
    public void SetFeatures(FeatureVector vector)
    {
        _grid.Rows.Clear(); foreach (var name in FeatureVector.FeatureNames) _grid.Rows.Add(name, vector[name].ToString("G10"));
    }
}
