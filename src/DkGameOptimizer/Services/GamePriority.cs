using System.Diagnostics;

namespace DkGameOptimizer.Services;

public static class GamePriority
{
    private sealed record OriginalPriority(int ProcessId, DateTime StartedUtc, ProcessPriorityClass Priority);
    private static readonly Dictionary<int, OriginalPriority> Changed = [];

    public static string Apply(string executablePath)
    {
        if (!File.Exists(executablePath) || !executablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Selecione o executável real do jogo antes de ajustar a prioridade.");

        var name = Path.GetFileNameWithoutExtension(executablePath);
        var matches = new List<Process>();
        foreach (var process in Process.GetProcessesByName(name))
        {
            if (IsSameFile(process, executablePath)) matches.Add(process);
            else process.Dispose();
        }
        if (matches.Count == 0) return "O executável selecionado não está em execução. Abra o jogo e tente novamente.";

        var changed = 0;
        foreach (var process in matches)
        {
            var firstChange = !Changed.ContainsKey(process.Id);
            try
            {
                if (firstChange)
                    Changed[process.Id] = new OriginalPriority(process.Id, process.StartTime.ToUniversalTime(), process.PriorityClass);
                process.PriorityClass = ProcessPriorityClass.High;
                changed++;
            }
            catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
            {
                if (firstChange) Changed.Remove(process.Id);
            }
            finally { process.Dispose(); }
        }
        return changed > 0
            ? $"Prioridade Alta aplicada a {changed} processo(s). Ela será restaurada ao fechar o aplicativo ou pelo botão Restaurar."
            : "O jogo está em execução, mas o Windows ou o anticheat não permitiu alterar a prioridade.";
    }

    public static string Restore(string executablePath)
    {
        var name = Path.GetFileNameWithoutExtension(executablePath);
        var restored = 0;
        foreach (var process in Process.GetProcessesByName(name))
        {
            try
            {
                if (Changed.TryGetValue(process.Id, out var original) &&
                    process.StartTime.ToUniversalTime() == original.StartedUtc &&
                    IsSameFile(process, executablePath))
                {
                    process.PriorityClass = original.Priority;
                    Changed.Remove(process.Id);
                    restored++;
                }
            }
            catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException) { }
            finally { process.Dispose(); }
        }
        return restored > 0 ? $"Prioridade anterior restaurada em {restored} processo(s)." : "Nenhum processo alterado por este aplicativo está em execução.";
    }

    public static void RestoreAll()
    {
        foreach (var item in Changed.Values.ToList())
        {
            try
            {
                using var process = Process.GetProcessById(item.ProcessId);
                if (process.StartTime.ToUniversalTime() == item.StartedUtc)
                    process.PriorityClass = item.Priority;
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException) { }
        }
        Changed.Clear();
    }

    private static bool IsSameFile(Process process, string expectedPath)
    {
        try { return string.Equals(process.MainModule?.FileName, expectedPath, StringComparison.OrdinalIgnoreCase); }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException) { return false; }
    }
}
