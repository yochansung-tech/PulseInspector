using PulseInspector.Models;

namespace PulseInspector.Services;

public static class StatisticsService
{
    public static double[] Mean(IEnumerable<FeatureVector> vectors) => Mean(vectors.Select(v => v.ToArray()).ToArray());

    public static double[] Mean(double[][] data)
    {
        if (data.Length == 0) return Array.Empty<double>();
        var n = data[0].Length; var result = new double[n];
        foreach (var row in data) for (var j = 0; j < n; j++) result[j] += row[j];
        for (var j = 0; j < n; j++) result[j] /= data.Length;
        return result;
    }

    public static double[,] Covariance(double[][] data)
    {
        var mean = Mean(data); var n = mean.Length; var cov = new double[n, n];
        if (data.Length < 2) { for (var i = 0; i < n; i++) cov[i, i] = 1e-12; return cov; }
        foreach (var row in data)
            for (var i = 0; i < n; i++) for (var j = 0; j < n; j++) cov[i, j] += (row[i] - mean[i]) * (row[j] - mean[j]);
        for (var i = 0; i < n; i++) for (var j = 0; j < n; j++) cov[i, j] /= data.Length - 1;
        for (var i = 0; i < n; i++) cov[i, i] += 1e-12;
        return cov;
    }

    public static double[,] Invert(double[,] matrix)
    {
        var n = matrix.GetLength(0); var a = new double[n, 2 * n];
        for (var i = 0; i < n; i++) for (var j = 0; j < n; j++) { a[i, j] = matrix[i, j]; a[i, j + n] = i == j ? 1 : 0; }
        for (var col = 0; col < n; col++)
        {
            var pivot = col; for (var r = col + 1; r < n; r++) if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r;
            if (Math.Abs(a[pivot, col]) < 1e-15) { a[pivot, col] = 1e-12; }
            if (pivot != col) for (var j = 0; j < 2 * n; j++) (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);
            var div = a[col, col]; for (var j = 0; j < 2 * n; j++) a[col, j] /= div;
            for (var r = 0; r < n; r++) if (r != col) { var f = a[r, col]; for (var j = 0; j < 2 * n; j++) a[r, j] -= f * a[col, j]; }
        }
        var inv = new double[n, n]; for (var i = 0; i < n; i++) for (var j = 0; j < n; j++) inv[i, j] = a[i, j + n];
        return inv;
    }

    public static double Mahalanobis(double[] x, double[] mean, double[,] inverseCovariance)
    {
        var n = mean.Length; var d = new double[n]; for (var i = 0; i < n; i++) d[i] = x[i] - mean[i];
        double value = 0; for (var i = 0; i < n; i++) { double s = 0; for (var j = 0; j < n; j++) s += inverseCovariance[i, j] * d[j]; value += d[i] * s; }
        return Math.Max(0, value);
    }

    public static double ChiSquare99_9(int degreesOfFreedom) => degreesOfFreedom switch
    {
        6 => 22.457744,
        _ => ChiSquareWilsonHilferty(degreesOfFreedom, 0.999)
    };

    private static double ChiSquareWilsonHilferty(int k, double p)
    {
        var z = 3.090232306; var a = 2.0 / (9.0 * k); return k * Math.Pow(1 - a + z * Math.Sqrt(a), 3);
    }
}
