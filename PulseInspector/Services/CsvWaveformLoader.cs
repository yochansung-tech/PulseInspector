using System.Globalization;
using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class CsvImportOptions
{
    // Used when the CSV contains only waveform samples and no time column.
    public double? MeasurementPeriodSeconds { get; init; } = 2.56e-6;
    public double TimeToleranceRelative { get; init; } = 1e-6;
}

public sealed class CsvWaveformLoader
{
    public WaveformData Load(string filePath, CsvImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("CSV file was not found.", filePath);

        options ??= new CsvImportOptions();
        var rows = ReadNumericRows(filePath);
        if (rows.Count == 0)
            throw new InvalidDataException($"No numeric data was found in '{Path.GetFileName(filePath)}'.");

        WaveformData result;
        if (rows.All(r => r.Count == 1))
        {
            var samples = rows.Select(r => r[0]).ToArray();
            if (samples.Length < 2)
                throw new InvalidDataException("A waveform must contain at least two numeric samples.");

            if (!options.MeasurementPeriodSeconds.HasValue || options.MeasurementPeriodSeconds.Value <= 0)
                throw new InvalidDataException("Measurement period is required for a single-column CSV.");

            result = new WaveformData
            {
                SourceName = Path.GetFileName(filePath),
                Samples = samples,
                SampleIntervalSeconds = options.MeasurementPeriodSeconds.Value / samples.Length,
                HasExplicitTimeAxis = false
            };
        }
        else if (rows.All(r => r.Count == 2))
        {
            var time = rows.Select(r => r[0]).ToArray();
            var samples = rows.Select(r => r[1]).ToArray();
            var dt = EstimateUniformInterval(time, options.TimeToleranceRelative);

            result = new WaveformData
            {
                SourceName = Path.GetFileName(filePath),
                Samples = samples,
                SampleIntervalSeconds = dt,
                HasExplicitTimeAxis = true
            };
        }
        else
        {
            throw new InvalidDataException(
                "CSV must contain either one numeric waveform column or two numeric columns (time, value). " +
                "Mixed-width numeric rows are not supported.");
        }

        result.Validate();
        return result;
    }

    private static List<double[]> ReadNumericRows(string filePath)
    {
        var rows = new List<double[]>();
        foreach (var rawLine in File.ReadLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.TrimEntries);
            var values = new List<double>(cells.Length);
            foreach (var cell in cells)
            {
                if (double.TryParse(cell, NumberStyles.Float | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var value))
                    values.Add(value);
            }

            // Header/non-numeric lines are ignored. A partially numeric row is rejected.
            if (values.Count == 0) continue;
            if (values.Count != cells.Length)
                throw new InvalidDataException($"Non-numeric cell found in data row: '{rawLine}'.");

            rows.Add(values.ToArray());
        }
        return rows;
    }

    private static double EstimateUniformInterval(IReadOnlyList<double> time, double relativeTolerance)
    {
        if (time.Count < 2) throw new InvalidDataException("At least two time samples are required.");

        var dt = time[1] - time[0];
        if (!double.IsFinite(dt) || dt <= 0)
            throw new InvalidDataException("Time axis must be strictly increasing.");

        for (var i = 1; i < time.Count - 1; i++)
        {
            var current = time[i + 1] - time[i];
            var tolerance = Math.Max(Math.Abs(dt), Math.Abs(current)) * relativeTolerance;
            if (!double.IsFinite(current) || current <= 0 || Math.Abs(current - dt) > tolerance)
                throw new InvalidDataException("Time axis is not uniformly sampled.");
        }

        return dt;
    }
}
