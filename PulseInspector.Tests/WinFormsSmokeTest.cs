using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using PulseInspector.Forms;

namespace PulseInspector.Tests;

internal static class WinFormsSmokeTest
{
    public static void Run()
    {
        Console.WriteLine("=== WinForms Smoke Test ===");
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new MainForm();
                Assert(form.Text == "PulseInspector Release 1.0", "MainForm title is incorrect.");
                Console.WriteLine("MainForm           : PASS");

                Assert(form.MainMenuStrip is not null, "MainMenuStrip was not created.");
                Console.WriteLine("MenuStrip          : PASS");

                var load = typeof(MainForm).GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(load is not null, "MainForm.OnLoad could not be located.");
                load!.Invoke(form, new object[] { EventArgs.Empty });

                var menu = form.MainMenuStrip!;
                Assert(FindMenu(menu.Items, "File") is not null, "File menu is missing.");
                Assert(FindMenu(menu.Items, "Training") is not null, "Training menu is missing.");
                Assert(FindMenu(menu.Items, "Settings") is not null, "Settings menu is missing.");
                Assert(FindMenu(menu.Items, "About") is not null, "About menu is missing.");

                var export = FindMenu(menu.Items, "Export");
                Assert(export is not null, "Export menu is missing.");
                Assert(FindMenu(export!.DropDownItems, "Export Inspection Result to CSV...") is not null,
                    "CSV export command is missing.");
                Console.WriteLine("Export Menu        : PASS");

                Assert(form.Controls.OfType<MenuStrip>().Any(), "MenuStrip is not attached to MainForm.");
                Assert(form.Controls.OfType<Panel>().Any(), "Main analysis/status panels were not created.");
                Console.WriteLine("Controls           : PASS");
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw new InvalidOperationException("WinForms UI smoke test failed.", failure);

        Console.WriteLine("Dispose            : PASS");
        Console.WriteLine("WINFORMS SMOKE TEST PASSED");
    }

    private static ToolStripMenuItem? FindMenu(ToolStripItemCollection items, string text)
    {
        return items.Cast<ToolStripItem>()
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(item => string.Equals(item.Text, text, StringComparison.Ordinal));
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
