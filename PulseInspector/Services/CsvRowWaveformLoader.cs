using System.Globalization;
using PulseInspector.Models;

namespace PulseInspector.Services;

/// <summary>
/// Loads a CSV in which each non-empty data line represents one complete waveform/subgroup.
/// Example: 64 comma-separated samples per line, with the next line containing the next subgroup.
/// </summary>
public sealed class CsvRowWaveformLoader
{
    public IReadOnlyList<WaveformData> LoadRows(string filePath, CsvImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("CSV file was not found.", filePath);

        options ??= new CsvImportOptions();
        if (!options.MeasurementPeriodSeconds.HasValue || options.MeasurementPeriodSeconds.Value <= 0)
            throw new InvalidDataException("A positive measurement period is required for row-based waveform CSV import.");

        var result = new List<WaveformData>();
        var lineNumber = 0;

        foreach (var rawLine in File.ReadLines(filePath))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cells = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.TrimEntries);
            if (cells.Length < 2)
                continue; // Allows a text header or metadata line.

            var samples = new double[cells.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                if (!double.TryParse(cells[i], NumberStyles.Float | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out samples[i]) || !double.IsFinite(samples[i]))
                {
                    throw new InvalidDataException(
                        $"Invalid numeric value at line {lineNumber}, column {i + 1}: '{cells[i]}'.");
                }
            }

            var dt = options.MeasurementPeriodSeconds.Value / samples.Length;
            var data = new WaveformData
            {
                SourceName = $"{Path.GetFileName(filePath)} [row {lineNumber}]",
                Samples = samples,
                SampleIntervalSeconds = dt,
                HasExplicitTimeAxis = false
            };
            data.Validate();
            result.Add(data);
        }

        if (result.Count == 0)
            throw new InvalidDataException($"No waveform rows were found in '{Path.GetFileName(filePath)}'.");

        var sampleCount = result[0].SampleCount;
        if (result.Any(x => x.SampleCount != sampleCount))
            throw new InvalidDataException(
                "All waveform rows in a row-based CSV must contain the same number of samples.");

        return result;
    }
}
