using PulseInspector.Controls;
using PulseInspector.Models;

namespace PulseInspector.Tests;

internal static class FeatureDeviationGridTests
{
    public static void Run()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var grid = new FeatureDeviationGrid();
                var results = new[]
                {
                    new FeatureDeviation("Peak", 180, 100, 2, 40, 40, 12.5),
                    new FeatureDeviation("Charge", 25, 10, 1, 15, 15, 4.0),
                    new FeatureDeviation("FWHM", 35, 20, 1, 15, 15, 3.0),
                    new FeatureDeviation("Noise", 2, 0.5, 0.1, 15, 15, 2.0),
                    new FeatureDeviation("RiseTime", 5, 2, 0.1, 30, 30, 1.5),
                    new FeatureDeviation("ZScore", 4, 0, 1, 4, 4, 0.5)
                };

                grid.SetResults(results);
                Assert(grid.RowCount == results.Length, "FeatureDeviationGrid row count is incorrect.");
                Assert(grid.DisplayedFeatureNames.SequenceEqual(results.Select(r => r.FeatureName)), "FeatureDeviationGrid feature order was not preserved.");

                grid.SetResults(Array.Empty<FeatureDeviation>());
                Assert(grid.RowCount == 0, "FeatureDeviationGrid did not clear previous results.");
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw new InvalidOperationException("FeatureDeviationGrid UI integration test failed.", failure);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
