using System.Globalization;
using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class CsvImportOptions
{
    public double? MeasurementPeriodSeconds { get; init; } = 2.56e-6;
    public double? SampleIntervalSeconds { get; init; }
    public double TimeToleranceRelative { get; init; } = 1e-6;
}

public sealed class CsvWaveformLoader
{
    public WaveformData Load(string filePath, CsvImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath); if (!File.Exists(filePath)) throw new FileNotFoundException("CSV file was not found.", filePath); options ??= new CsvImportOptions(); var rows = ReadNumericRows(filePath); if (rows.Count == 0) throw new InvalidDataException($"No numeric data was found in '{Path.GetFileName(filePath)}'."); WaveformData result;
        if (rows.All(r => r.Length == 1)) { var samples = rows.Select(r => r[0]).ToArray(); if (samples.Length < 2) throw new InvalidDataException("A waveform must contain at least two numeric samples."); result = new WaveformData { SourceName = Path.GetFileName(filePath), Samples = samples, SampleIntervalSeconds = ResolveSampleInterval(options, samples.Length), HasExplicitTimeAxis = false }; }
        else if (rows.All(r => r.Length == 2)) { var time = rows.Select(r => r[0]).ToArray(); var samples = rows.Select(r => r[1]).ToArray(); result = new WaveformData { SourceName = Path.GetFileName(filePath), Samples = samples, SampleIntervalSeconds = EstimateUniformInterval(time, options.TimeToleranceRelative), HasExplicitTimeAxis = true }; }
        else throw new InvalidDataException("CSV must contain either one numeric waveform column or two numeric columns (time, value). Mixed-width numeric rows are not supported.");
        result.Validate(); return result;
    }
    private static double ResolveSampleInterval(CsvImportOptions options, int sampleCount)
    { if (options.SampleIntervalSeconds.HasValue) { if (!double.IsFinite(options.SampleIntervalSeconds.Value) || options.SampleIntervalSeconds.Value <= 0) throw new InvalidDataException("Sample interval must be a positive finite value."); return options.SampleIntervalSeconds.Value; } if (!options.MeasurementPeriodSeconds.HasValue || options.MeasurementPeriodSeconds.Value <= 0) throw new InvalidDataException("Measurement period or sample interval is required for a single-column CSV."); return options.MeasurementPeriodSeconds.Value / sampleCount; }
    private static List<double[]> ReadNumericRows(string filePath)
    { var rows = new List<double[]>(); foreach (var rawLine in File.ReadLines(filePath)) { var line = rawLine.Trim(); if (string.IsNullOrWhiteSpace(line)) continue; var cells = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.TrimEntries); var values = new List<double>(cells.Length); foreach (var cell in cells) if (double.TryParse(cell, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)) values.Add(value); if (values.Count == 0) continue; if (values.Count != cells.Length) throw new InvalidDataException($"Non-numeric cell found in data row: '{rawLine}'."); rows.Add(values.ToArray()); } return rows; }
    private static double EstimateUniformInterval(IReadOnlyList<double> time, double relativeTolerance)
    { if (time.Count < 2) throw new InvalidDataException("At least two time samples are required."); var dt = time[1] - time[0]; if (!double.IsFinite(dt) || dt <= 0) throw new InvalidDataException("Time axis must be strictly increasing."); for (var i = 1; i < time.Count - 1; i++) { var current = time[i + 1] - time[i]; var tolerance = Math.Max(Math.Abs(dt), Math.Abs(current)) * relativeTolerance; if (!double.IsFinite(current) || current <= 0 || Math.Abs(current - dt) > tolerance) throw new InvalidDataException("Time axis is not uniformly sampled."); } return dt; }
}
