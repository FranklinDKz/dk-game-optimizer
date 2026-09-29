using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DkGameOptimizer.Services;

public static class SystemTweaks
{
    private const string GameBarKey = @"Software\Microsoft\GameBar";
    private const string GameModeValue = "AutoGameModeEnabled";
    private const string HighPerformance = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private static readonly string StatePath = Path.Combine(ProfileStore.DataDirectory, "restore.json");

    private sealed class RestoreState
    {
        public bool HasGameModeBackup { get; set; }
        public int? PreviousGameMode { get; set; }
        public string? PreviousPowerScheme { get; set; }
    }

    private static RestoreState ReadState()
    {
        try
        {
            return File.Exists(StatePath)
                ? JsonSerializer.Deserialize<RestoreState>(File.ReadAllText(StatePath)) ?? new RestoreState()
                : new RestoreState();
        }
        catch { return new RestoreState(); }
    }

    private static void SaveState(RestoreState state)
    {
        Directory.CreateDirectory(ProfileStore.DataDirectory);
        var temporary = StatePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, StatePath, true);
    }

    public static string GameModeStatus()
    {
        using var key = Registry.CurrentUser.OpenSubKey(GameBarKey);
        return key?.GetValue(GameModeValue) is int value
            ? value == 1 ? "Ativado" : "Desativado"
            : "Padrão do Windows";
    }

    public static string EnableGameMode()
    {
        var state = ReadState();
        using var key = Registry.CurrentUser.CreateSubKey(GameBarKey)
            ?? throw new InvalidOperationException("Não foi possível abrir a configuração do Modo de Jogo.");
        if (!state.HasGameModeBackup)
        {
            state.HasGameModeBackup = true;
            state.PreviousGameMode = key.GetValue(GameModeValue) is int previous ? previous : null;
            SaveState(state);
        }
        key.SetValue(GameModeValue, 1, RegistryValueKind.DWord);
        return "Modo de Jogo ativado. Reinicie o jogo para comparar o resultado.";
    }

    public static string RestoreGameMode()
    {
        var state = ReadState();
        if (!state.HasGameModeBackup) return "Nenhuma alteração do Modo de Jogo foi feita por este aplicativo.";
        using var key = Registry.CurrentUser.CreateSubKey(GameBarKey)
            ?? throw new InvalidOperationException("Não foi possível abrir a configuração do Modo de Jogo.");
        if (state.PreviousGameMode is int previous)
            key.SetValue(GameModeValue, previous, RegistryValueKind.DWord);
        else
            key.DeleteValue(GameModeValue, false);
        state.HasGameModeBackup = false;
        state.PreviousGameMode = null;
        SaveState(state);
        return "Configuração anterior do Modo de Jogo restaurada.";
    }

    public static string PowerStatus()
    {
        var output = RunPowerCfg("/getactivescheme");
        var guid = Regex.Match(output, @"[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}").Value;
        return guid.Length > 0 ? guid : "Não identificado";
    }

    public static string EnableHighPerformance()
    {
        var current = PowerStatus();
        if (!Guid.TryParse(current, out _))
            throw new InvalidOperationException("O plano de energia atual não pôde ser identificado.");
        if (current.Equals(HighPerformance, StringComparison.OrdinalIgnoreCase))
            return "O plano Alto desempenho já está ativo.";

        var available = RunPowerCfg("/list");
        if (!available.Contains(HighPerformance, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O plano Alto desempenho não está disponível neste computador. Use as configurações de Energia do Windows.");

        var state = ReadState();
        state.PreviousPowerScheme ??= current;
        SaveState(state);
        RunPowerCfg($"/setactive {HighPerformance}");
        return "Plano Alto desempenho ativado. Em notebooks, o consumo de bateria pode aumentar.";
    }

    public static string RestorePowerPlan()
    {
        var state = ReadState();
        if (state.PreviousPowerScheme is not { } previous)
            return "Nenhuma alteração do plano de energia foi feita por este aplicativo.";
        RunPowerCfg($"/setactive {previous}");
        state.PreviousPowerScheme = null;
        SaveState(state);
        return "Plano de energia anterior restaurado.";
    }

    private static string RunPowerCfg(string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("powercfg.exe", arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(10000))
        {
            process.Kill();
            throw new TimeoutException("O comando de energia demorou mais que o esperado.");
        }
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "O Windows recusou a alteração do plano de energia." : error.Trim());
        return output;
    }
}
