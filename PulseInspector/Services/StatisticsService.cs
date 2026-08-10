using PulseInspector.Models;

namespace PulseInspector.Services;

public static class StatisticsService
{
    public static double[] Mean(IEnumerable<FeatureVector> vectors) => Mean(vectors.Select(v => v.ToArray()).ToArray());

    public static double[] Mean(double[][] data)
    {
        if (data.Length == 0) return Array.Empty<double>();
        var n = data[0].Length;
        if (data.Any(row => row.Length != n))
            throw new ArgumentException("All rows must have the same feature count.", nameof(data));

        var result = new double[n];
        foreach (var row in data)
            for (var j = 0; j < n; j++) result[j] += row[j];
        for (var j = 0; j < n; j++) result[j] /= data.Length;
        return result;
    }

    public static double[,] Covariance(double[][] data)
    {
        var mean = Mean(data);
        var n = mean.Length;
        var cov = new double[n, n];
        if (data.Length < 2)
        {
            for (var i = 0; i < n; i++) cov[i, i] = 1e-12;
            return cov;
        }

        foreach (var row in data)
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    cov[i, j] += (row[i] - mean[i]) * (row[j] - mean[j]);

        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                cov[i, j] /= data.Length - 1;

        // Small diagonal regularization keeps the matrix invertible for highly correlated features.
        for (var i = 0; i < n; i++) cov[i, i] += 1e-12;
        return cov;
    }

    public static double[,] Invert(double[,] matrix)
    {
        var n = matrix.GetLength(0);
        if (matrix.GetLength(1) != n) throw new ArgumentException("Matrix must be square.", nameof(matrix));

        var a = new double[n, 2 * n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
            {
                a[i, j] = matrix[i, j];
                a[i, j + n] = i == j ? 1 : 0;
            }

        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            for (var r = col + 1; r < n; r++)
                if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r;

            if (Math.Abs(a[pivot, col]) < 1e-15)
                throw new InvalidOperationException("Covariance matrix is singular or numerically unstable.");

            if (pivot != col)
                for (var j = 0; j < 2 * n; j++)
                    (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);

            var div = a[col, col];
            for (var j = 0; j < 2 * n; j++) a[col, j] /= div;

            for (var r = 0; r < n; r++)
            {
                if (r == col) continue;
                var factor = a[r, col];
                if (Math.Abs(factor) < 1e-30) continue;
                for (var j = 0; j < 2 * n; j++) a[r, j] -= factor * a[col, j];
            }
        }

        var inverse = new double[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++) inverse[i, j] = a[i, j + n];
        return inverse;
    }

    public static double Mahalanobis(double[] x, double[] mean, double[,] inverseCovariance)
    {
        if (x.Length != mean.Length || inverseCovariance.GetLength(0) != mean.Length || inverseCovariance.GetLength(1) != mean.Length)
            throw new ArgumentException("Feature dimensions do not match the inspection model.");

        var n = mean.Length;
        var d = new double[n];
        for (var i = 0; i < n; i++) d[i] = x[i] - mean[i];

        double value = 0;
        for (var i = 0; i < n; i++)
        {
            double s = 0;
            for (var j = 0; j < n; j++) s += inverseCovariance[i, j] * d[j];
            value += d[i] * s;
        }
        return Math.Max(0, value);
    }

    public static double ChiSquare99_9(int degreesOfFreedom) => ChiSquareQuantile(degreesOfFreedom, 0.999);

    public static double ChiSquareQuantile(int degreesOfFreedom, double confidence)
    {
        if (degreesOfFreedom <= 0) throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom));
        if (confidence <= 0 || confidence >= 1) throw new ArgumentOutOfRangeException(nameof(confidence));

        // Exact value used by the Release 1.0 six-feature model.
        if (degreesOfFreedom == 6 && Math.Abs(confidence - 0.999) < 1e-12)
            return 22.457744;

        // Wilson-Hilferty approximation for other df/confidence combinations.
        var z = InverseStandardNormal(confidence);
        var a = 2.0 / (9.0 * degreesOfFreedom);
        return degreesOfFreedom * Math.Pow(1 - a + z * Math.Sqrt(a), 3);
    }

    private static double InverseStandardNormal(double p)
    {
        // Acklam's rational approximation.
        const double a1 = -39.6968302866538, a2 = 220.946098424521, a3 = -275.928510446969;
        const double a4 = 138.357751867269, a5 = -30.6647980661472, a6 = 2.50662827745924;
        const double b1 = -54.4760987982241, b2 = 161.585836858041, b3 = -155.698979859887;
        const double b4 = 66.8013118877197, b5 = -13.2806815528857;
        const double c1 = -0.00778489400243029, c2 = -0.322396458041136, c3 = -2.40075827716184;
        const double c4 = -2.54973253934373, c5 = 4.37466414146497, c6 = 2.93816398269878;
        const double d1 = 0.00778469570904146, d2 = 0.32246712907004, d3 = 2.445134137143;
        const double d4 = 3.75440866190742;

        var plow = 0.02425;
        var phigh = 1 - plow;
        if (p < plow)
        {
            var q = Math.Sqrt(-2 * Math.Log(p));
            return (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                   ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
        if (p > phigh)
        {
            var q = Math.Sqrt(-2 * Math.Log(1 - p));
            return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                    ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }

        var r = p - 0.5;
        var s = r * r;
        return (((((a1 * s + a2) * s + a3) * s + a4) * s + a5) * s + a6) * r /
               (((((b1 * s + b2) * s + b3) * s + b4) * s + b5) * s + 1);
    }
}
