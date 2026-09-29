using System.Text.Json;

namespace DkGameOptimizer.Services;

public sealed record ActionOutcome(bool Success, string Message);

public static class ActionWorker
{
    private static string ResultDirectory => Path.Combine(ProfileStore.DataDirectory, "runs");

    public static string ResultPath(Guid token) => Path.Combine(ResultDirectory, token.ToString("N") + ".json");

    public static int Run(string id, string mode, string tokenText)
    {
        if (!Guid.TryParse(tokenText, out var token) || mode is not ("apply" or "restore")) return 2;
        ActionOutcome outcome;
        try
        {
            var message = AdvancedActions.ExecuteAsync(id, mode == "restore").GetAwaiter().GetResult();
            outcome = new ActionOutcome(true, message);
        }
        catch (Exception error)
        {
            outcome = new ActionOutcome(false, error.Message);
        }

        try
        {
            Directory.CreateDirectory(ResultDirectory);
            File.WriteAllText(ResultPath(token), JsonSerializer.Serialize(outcome));
        }
        catch { return 3; }
        return outcome.Success ? 0 : 1;
    }

    public static ActionOutcome? Read(Guid token)
    {
        var path = ResultPath(token);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<ActionOutcome>(File.ReadAllText(path)); }
        finally
        {
            try { File.Delete(path); }
            catch (IOException) { }
        }
    }
}
