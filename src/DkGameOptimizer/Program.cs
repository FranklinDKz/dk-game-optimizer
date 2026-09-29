using DkGameOptimizer.Services;
using DkGameOptimizer.Ui;

namespace DkGameOptimizer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var setup = new SetupForm(ProfileStore.Load());
        if (setup.ShowDialog() != DialogResult.OK || setup.ResultProfile is null) return;
        Application.Run(new MainForm(setup.ResultProfile));
    }
}
