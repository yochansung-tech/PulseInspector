namespace PulseInspector.Tests;

// Legacy WinForms UI smoke coverage has been retired from the active test suite.
// WPF is now the supported application UI; domain/application regression tests
// remain in Program.cs and continue to protect the migration boundary.
internal static class WinFormsSmokeTest
{
    public static void Run() => Console.WriteLine("WinForms UI smoke test retired; WPF is the supported UI.");
}
