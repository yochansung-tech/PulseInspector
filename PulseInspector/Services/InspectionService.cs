using PulseInspector.Models;

namespace PulseInspector.Services;

public sealed class InspectionService
{
    private readonly TrainingValidationService _trainingValidation = new();
    private const double CovarianceRegularization = 1e-6;
    private const int DefaultNormalModeCount = 1;
    private const int MaxKMeansIterations = 50;

    public InspectionService() { }

    public TrainingValidationResult ValidateTraining(IEnumerable<FeatureVector> vectors) =>
        _trainingValidation.Validate(vectors);

    public InspectionModel Train(
        IEnumerable<FeatureVector> vectors,
        double confidence = 0.999,
        int normalModeCount = DefaultNormalModeCount)
    {
        if (confidence <= 0 || confidence >= 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        if (normalModeCount is < 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(normalModeCount), "Normal mode count must be 1 or 2.");

        var samples = vectors.Select(v => v.Clone()).ToArray();
        if (samples.Length == 0)
            throw new InvalidOperationException("At least one training feature vector is required.");

        var validation = _trainingValidation.Validate(samples);
        var errors = validation.Issues
            .Where(i => i.Code.StartsWith("ERROR_", StringComparison.Ordinal))
            .ToArray();
        if (errors.Length > 0)
            throw new InvalidOperationException(
                "Training data validation failed: " + string.Join("; ", errors.Select(e => e.Message)));

        var rawRows = samples.Select(v => v.ToStatisticalArray()).ToArray();
        var featureCount = FeatureVector.StatisticalFeatureCount;
        var featureMeans = Enumerable.Range(0, featureCount)
            .Select(i => rawRows.Average(row => row[i]))
            .ToArray();
        var featureScales = Enumerable.Range(0, featureCount)
            .Select(i => SampleStandardDeviation(rawRows.Select(row => row[i]).ToArray(), featureMeans[i]))
            .Select(scale => scale > 0 && double.IsFinite(scale) ? scale : 1e-12)
            .ToArray();
        var standardizedRows = rawRows
            .Select(row => Standardize(row, featureMeans, featureScales))
            .ToArray();

        int[] assignments;
        if (normalModeCount == 1)
        {
            assignments = new int[standardizedRows.Length];
        }
        else
        {
            assignments = ClusterTwoNormalModes(standardizedRows);
        }

        var modeModels = BuildModeModels(standardizedRows, assignments, confidence, normalModeCount);
        var firstMode = modeModels[0];
        var peakIndex = FeatureVector.GetStatisticalIndex("Peak");

        var model = new InspectionModel
        {
            Mean = firstMode.Mean,
            Covariance = firstMode.Covariance,
            InverseCovariance = firstMode.InverseCovariance,
            StandardDeviations = firstMode.StandardDeviations,
            FeatureMeans = featureMeans,
            FeatureScales = featureScales,
            PeakMean = featureMeans[peakIndex],
            PeakStandardDeviation = featureScales[peakIndex],
            Confidence = confidence,
            Threshold = firstMode.Threshold,
            NormalModes = modeModels.ToList()
        };

        model.ValidateFeatureOrder();
        return model;
    }

    public InspectionResult Inspect(FeatureVector vector, InspectionModel model)
    {
        model.ValidateFeatureOrder();
        var peakStd = model.PeakStandardDeviation > 0 ? model.PeakStandardDeviation : 1e-12;
        vector["ZScore"] = (vector["Peak"] - model.PeakMean) / peakStd;
        var rawValues = vector.ToStatisticalArray();
        if (rawValues.Length != model.FeatureMeans.Length)
            throw new InvalidOperationException("FeatureVector and InspectionModel dimensions do not match.");
        var standardizedValues = Standardize(rawValues, model.FeatureMeans, model.FeatureScales);

        if (model.IsMultiModal)
        {
            var best = model.NormalModes
                .Select(mode => new
                {
                    Mode = mode,
                    Distance = StatisticsService.Mahalanobis(standardizedValues, mode.Mean, mode.InverseCovariance)
                })
                .OrderBy(x => x.Distance)
                .First();
            var defect = best.Distance > best.Mode.Threshold;
            vector["MahalanobisDistance"] = best.Distance;
            vector["Threshold"] = best.Mode.Threshold;
            return new InspectionResult(
                defect,
                best.Distance,
                best.Mode.Threshold,
                vector,
                defect ? $"Abnormal subgroup (closest normal mode: {best.Mode.Name})" : $"Normal subgroup ({best.Mode.Name})");
        }

        var distance = StatisticsService.Mahalanobis(standardizedValues, model.Mean, model.InverseCovariance);
        var legacyDefect = distance > model.Threshold;
        vector["MahalanobisDistance"] = distance;
        vector["Threshold"] = model.Threshold;
        return new InspectionResult(legacyDefect, distance, model.Threshold, vector, legacyDefect ? "Abnormal group" : "Normal group");
    }

    private NormalModeModel[] BuildModeModels(
        double[][] standardizedRows,
        int[] assignments,
        double confidence,
        int modeCount)
    {
        var featureCount = FeatureVector.StatisticalFeatureCount;
        var threshold = StatisticsService.ChiSquareQuantile(featureCount, confidence);
        var result = new NormalModeModel[modeCount];

        for (var modeIndex = 0; modeIndex < modeCount; modeIndex++)
        {
            var rows = standardizedRows.Where((_, index) => assignments[index] == modeIndex).ToArray();
            if (rows.Length < featureCount + 1)
                throw new InvalidOperationException(
                    $"Normal mode {modeIndex + 1} contains only {rows.Length} samples; at least {featureCount + 1} are required.");

            var mean = StatisticsService.Mean(rows);
            var covariance = StatisticsService.Covariance(rows);
            for (var i = 0; i < featureCount; i++) covariance[i, i] += CovarianceRegularization;
            var inverse = StatisticsService.Invert(covariance);
            var standardDeviations = Enumerable.Range(0, featureCount)
                .Select(i => Math.Sqrt(Math.Max(covariance[i, i], 0.0)))
                .Select(x => x > 0 ? x : 1e-12)
                .ToArray();

            result[modeIndex] = new NormalModeModel
            {
                ModeIndex = modeIndex,
                Name = $"Normal Mode {modeIndex + 1}",
                SampleCount = rows.Length,
                Mean = mean,
                Covariance = covariance,
                InverseCovariance = inverse,
                StandardDeviations = standardDeviations,
                Confidence = confidence,
                Threshold = threshold
            };
        }

        if (modeCount == 2)
        {
            var fwhmIndex = FeatureVector.GetStatisticalIndex("FWHM");
            if (result[0].Mean[fwhmIndex] > result[1].Mean[fwhmIndex])
            {
                (result[0], result[1]) = (result[1], result[0]);
                result[0].ModeIndex = 0;
                result[1].ModeIndex = 1;
            }
        }

        return result;
    }

    private static int[] ClusterTwoNormalModes(double[][] rows)
    {
        if (rows.Length < 2)
            throw new InvalidOperationException("At least two samples are required for normal-mode clustering.");
        var fwhmIndex = FeatureVector.GetStatisticalIndex("FWHM");
        var lowIndex = 0;
        var highIndex = 0;
        for (var i = 1; i < rows.Length; i++)
        {
            if (rows[i][fwhmIndex] < rows[lowIndex][fwhmIndex]) lowIndex = i;
            if (rows[i][fwhmIndex] > rows[highIndex][fwhmIndex]) highIndex = i;
        }
        var centroids = new[] { (double[])rows[lowIndex].Clone(), (double[])rows[highIndex].Clone() };
        var assignments = new int[rows.Length];
        for (var iteration = 0; iteration < MaxKMeansIterations; iteration++)
        {
            var changed = false;
            for (var i = 0; i < rows.Length; i++)
            {
                var d0 = SquaredDistance(rows[i], centroids[0]);
                var d1 = SquaredDistance(rows[i], centroids[1]);
                var next = d0 <= d1 ? 0 : 1;
                if (iteration == 0 || assignments[i] != next) { assignments[i] = next; changed = true; }
            }
            var sums = new[]
            {
                new double[FeatureVector.StatisticalFeatureCount],
                new double[FeatureVector.StatisticalFeatureCount]
            };
            var counts = new int[2];
            for (var i = 0; i < rows.Length; i++)
            {
                var cluster = assignments[i];
                counts[cluster]++;
                for (var j = 0; j < sums[cluster].Length; j++) sums[cluster][j] += rows[i][j];
            }
            for (var cluster = 0; cluster < 2; cluster++)
            {
                if (counts[cluster] == 0)
                    throw new InvalidOperationException("Normal-mode clustering produced an empty cluster.");
                for (var j = 0; j < centroids[cluster].Length; j++) centroids[cluster][j] = sums[cluster][j] / counts[cluster];
            }
            if (!changed) break;
        }
        return assignments;
    }

    private static double SquaredDistance(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        double sum = 0;
        for (var i = 0; i < a.Count; i++)
        {
            var d = a[i] - b[i];
            sum += d * d;
        }
        return sum;
    }

    private static double[] Standardize(IReadOnlyList<double> values, IReadOnlyList<double> means, IReadOnlyList<double> scales)
    {
        if (values.Count != means.Count || values.Count != scales.Count)
            throw new ArgumentException("Feature standardization dimensions do not match.");
        var result = new double[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            var scale = scales[i] > 0 ? scales[i] : 1e-12;
            result[i] = (values[i] - means[i]) / scale;
        }
        return result;
    }

    private static double SampleStandardDeviation(IReadOnlyList<double> values, double mean)
    {
        if (values.Count < 2) return 0;
        var sum = values.Sum(x => (x - mean) * (x - mean));
        return Math.Sqrt(sum / (values.Count - 1));
    }
}
