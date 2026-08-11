using System.Globalization;
using PulseInspector.Models;

namespace PulseInspector.Services;

/// <summary>
/// Loads a CSV in which each non-empty data line represents one complete waveform/subgroup.
/// </summary>
public sealed class CsvRowWaveformLoader
{
    public IReadOnlyList<WaveformData> LoadRows(string filePath, CsvImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("CSV file was not found.", filePath);

        options ??= new CsvImportOptions();
        var result = new List<WaveformData>();
        var lineNumber = 0;
        int? expectedSampleCount = null;

        foreach (var rawLine in File.ReadLines(filePath))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.TrimEntries);
            if (cells.Length < 2) continue;

            var samples = new double[cells.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                if (!double.TryParse(cells[i], NumberStyles.Float | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out samples[i]) || !double.IsFinite(samples[i]))
                    throw new InvalidDataException($"Invalid numeric value at line {lineNumber}, column {i + 1}: '{cells[i]}'.");
            }

            expectedSampleCount ??= samples.Length;
            if (samples.Length != expectedSampleCount.Value)
                throw new InvalidDataException($"Inconsistent subgroup sample count at line {lineNumber}: expected {expectedSampleCount.Value}, received {samples.Length}.");

            var dt = ResolveSampleInterval(options, samples.Length);
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

        return result;
    }

    private static double ResolveSampleInterval(CsvImportOptions options, int sampleCount)
    {
        if (options.SampleIntervalSeconds.HasValue)
        {
            if (!double.IsFinite(options.SampleIntervalSeconds.Value) || options.SampleIntervalSeconds.Value <= 0)
                throw new InvalidDataException("Sample interval must be a positive finite value.");
            return options.SampleIntervalSeconds.Value;
        }

        if (!options.MeasurementPeriodSeconds.HasValue || options.MeasurementPeriodSeconds.Value <= 0)
            throw new InvalidDataException("Measurement period or sample interval is required for row-based waveform CSV import.");

        return options.MeasurementPeriodSeconds.Value / sampleCount;
    }
}
