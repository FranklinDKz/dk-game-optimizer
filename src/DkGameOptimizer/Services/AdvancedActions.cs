using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DkGameOptimizer.Services;

public static class AdvancedActions
{
    private sealed record RegistryChange(RegistryHive Hive, string Key, string Name, object Value, RegistryValueKind Kind);
    private sealed class SavedValue
    {
        public SavedValue() { }
        public RegistryHive Hive { get; set; }
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Existed { get; set; }
        public RegistryValueKind Kind { get; set; }
        public string? Value { get; set; }
    }

    private static readonly string BackupDirectory = Path.Combine(ProfileStore.DataDirectory, "backups");
    private static readonly string LogDirectory = Path.Combine(ProfileStore.DataDirectory, "logs");

    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static async Task<string> ExecuteAsync(string id, bool restore)
    {
        var spec = ActionCatalog.Get(id);
        if (spec.RequiresAdmin && !IsAdministrator())
            throw new UnauthorizedAccessException("Esta ação precisa de permissão de administrador.");
        if (restore && !spec.CanRestore)
            throw new InvalidOperationException("Esta ação não possui restauração automática.");

        if (id == "trim") return await TrimAsync(restore);
        if (restore) return RestoreRegistry(id);

        return id switch
        {
            "game_dvr" or "visual_effects" or "transparency" or "ram_cache" or "gpu_hags" or "gpu_mpo"
                => ApplyRegistry(id),
            "optimize_drives" => await RunAndLogAsync("defrag.exe", "/C", "/O", "/U", "/V"),
            "flush_dns" => await RunAndLogAsync("ipconfig.exe", "/flushdns"),
            "restore_point" => await RunAndLogAsync("powershell.exe", "-NoProfile", "-NonInteractive", "-Command",
                "Checkpoint-Computer -Description 'DK Game Optimizer' -RestorePointType MODIFY_SETTINGS"),
            "repair_windows" => await RepairWindowsAsync(),
            _ => throw new ArgumentException("Ação não reconhecida.", nameof(id))
        };
    }

    private static IReadOnlyList<RegistryChange> ChangesFor(string id) => id switch
    {
        "game_dvr" =>
        [
            new(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord),
            new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord)
        ],
        "visual_effects" =>
        [
            new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord),
            new(RegistryHive.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String)
        ],
        "transparency" =>
        [new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, RegistryValueKind.DWord)],
        "ram_cache" =>
        [new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0, RegistryValueKind.DWord)],
        "gpu_hags" =>
        [new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1, RegistryValueKind.DWord)],
        "gpu_mpo" =>
        [new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5, RegistryValueKind.DWord)],
        _ => throw new ArgumentException("Ação de Registro desconhecida.", nameof(id))
    };

    private static RegistryKey Hive(RegistryHive hive) => hive switch
    {
        RegistryHive.CurrentUser => Registry.CurrentUser,
        RegistryHive.LocalMachine => Registry.LocalMachine,
        _ => throw new ArgumentOutOfRangeException(nameof(hive))
    };

    private static string BackupPath(string id) => Path.Combine(BackupDirectory, id + ".json");

    private static string ApplyRegistry(string id)
    {
        var changes = ChangesFor(id);
        Directory.CreateDirectory(BackupDirectory);
        var path = BackupPath(id);
        if (!File.Exists(path) && changes.All(change =>
        {
            using var key = Hive(change.Hive).OpenSubKey(change.Key);
            return key?.GetValue(change.Name) is { } current &&
                key.GetValueKind(change.Name) == change.Kind && Equals(current, change.Value);
        })) return "Este ajuste já está aplicado. Nenhuma mudança foi feita.";
        if (!File.Exists(path))
        {
            var previous = changes.Select(change =>
            {
                using var key = Hive(change.Hive).OpenSubKey(change.Key);
                var value = key?.GetValue(change.Name);
                return new SavedValue
                {
                    Hive = change.Hive, Key = change.Key, Name = change.Name,
                    Existed = value is not null,
                    Kind = value is null ? change.Kind : key!.GetValueKind(change.Name),
                    Value = value switch
                    {
                        byte[] bytes => Convert.ToBase64String(bytes),
                        string[] values => JsonSerializer.Serialize(values),
                        _ => value?.ToString()
                    }
                };
            }).ToList();
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(previous, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
        }

        foreach (var change in changes)
        {
            using var key = Hive(change.Hive).CreateSubKey(change.Key)
                ?? throw new InvalidOperationException("Não foi possível abrir a chave do Registro.");
            key.SetValue(change.Name, change.Value, change.Kind);
        }
        return ActionCatalog.Get(id).RequiresRestart
            ? "Ajuste aplicado. Reinicie o Windows para avaliar o resultado."
            : "Ajuste aplicado. O estado anterior foi salvo para restauração.";
    }

    private static string RestoreRegistry(string id)
    {
        var path = BackupPath(id);
        if (!File.Exists(path)) return "Não há estado anterior salvo para esta ação.";
        var previous = JsonSerializer.Deserialize<List<SavedValue>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Cópia do Registro inválida.");
        var expected = ChangesFor(id);
        if (previous.Count != expected.Count || expected.Any(change =>
            previous.Count(item => item.Hive == change.Hive && item.Key == change.Key && item.Name == change.Name) != 1))
            throw new InvalidDataException("A cópia não corresponde às chaves desta ação.");
        foreach (var item in previous)
        {
            using var key = Hive(item.Hive).CreateSubKey(item.Key)
                ?? throw new InvalidOperationException("Não foi possível abrir a chave do Registro.");
            if (item.Existed)
            {
                object value = item.Kind switch
                {
                    RegistryValueKind.DWord => int.Parse(item.Value ?? "0"),
                    RegistryValueKind.QWord => long.Parse(item.Value ?? "0"),
                    RegistryValueKind.Binary or RegistryValueKind.None => Convert.FromBase64String(item.Value ?? ""),
                    RegistryValueKind.MultiString => JsonSerializer.Deserialize<string[]>(item.Value ?? "[]") ?? [],
                    _ => item.Value ?? ""
                };
                key.SetValue(item.Name, value, item.Kind);
            }
            else key.DeleteValue(item.Name, false);
        }
        File.Delete(path);
        return ActionCatalog.Get(id).RequiresRestart
            ? "Estado anterior restaurado. Reinicie o Windows para concluir."
            : "Estado anterior restaurado.";
    }

    private static async Task<string> TrimAsync(bool restore)
    {
        Directory.CreateDirectory(BackupDirectory);
        var backup = Path.Combine(BackupDirectory, "trim.txt");
        if (restore)
        {
            if (!File.Exists(backup)) return "Nenhuma alteração de TRIM foi salva.";
            var previous = File.ReadAllText(backup).Trim();
            if (previous is not ("0" or "1"))
                throw new InvalidDataException("Estado anterior de TRIM inválido.");
            await RunAndLogAsync("fsutil.exe", "behavior", "set", "DisableDeleteNotify", previous);
            File.Delete(backup);
            return "Estado anterior de TRIM restaurado.";
        }
        var output = await RunCommandAsync("fsutil.exe", "behavior", "query", "DisableDeleteNotify");
        var match = Regex.Match(output, @"NTFS\s+DisableDeleteNotify\s*=\s*([01])", RegexOptions.IgnoreCase);
        if (!match.Success) throw new InvalidOperationException("Não foi possível identificar o estado de TRIM no NTFS.");
        if (match.Groups[1].Value == "0") return "TRIM já está habilitado para NTFS. Nenhuma mudança foi feita.";
        if (!File.Exists(backup)) File.WriteAllText(backup, match.Groups[1].Value);
        await RunAndLogAsync("fsutil.exe", "behavior", "set", "DisableDeleteNotify", "0");
        return "TRIM habilitado para NTFS. O estado anterior foi salvo.";
    }

    private static async Task<string> RepairWindowsAsync()
    {
        await RunAndLogAsync("dism.exe", "/Online", "/Cleanup-Image", "/RestoreHealth");
        await RunAndLogAsync("sfc.exe", "/scannow");
        return "DISM e SFC concluídos. Consulte os logs em %LOCALAPPDATA%\\DKGameOptimizer\\logs.";
    }

    private static async Task<string> RunAndLogAsync(string fileName, params string[] arguments)
    {
        var output = await RunCommandAsync(fileName, arguments);
        Directory.CreateDirectory(LogDirectory);
        var log = Path.Combine(LogDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Path.GetFileNameWithoutExtension(fileName) + ".txt");
        await File.WriteAllTextAsync(log, output);
        return $"Comando concluído. Log: {log}";
    }

    private static async Task<string> RunCommandAsync(string fileName, params string[] arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await stdout + Environment.NewLine + await stderr;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} retornou código {process.ExitCode}. " +
                output[^Math.Min(500, output.Length)..].Trim());
        return output;
    }
}
