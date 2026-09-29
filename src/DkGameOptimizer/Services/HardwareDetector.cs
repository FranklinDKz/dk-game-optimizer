using System.Management;
using DkGameOptimizer.Models;

namespace DkGameOptimizer.Services;

public static class HardwareDetector
{
    private static List<Dictionary<string, object?>> Query(string wmiClass, params string[] properties)
        => QueryScope(@"root\cimv2", wmiClass, properties);

    private static List<Dictionary<string, object?>> QueryScope(string scope, string wmiClass, params string[] properties)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope,
                $"SELECT {string.Join(",", properties)} FROM {wmiClass}");
            using var results = searcher.Get();
            return results.Cast<ManagementObject>()
                .Select(item => properties.ToDictionary(name => name, name => (object?)item[name]))
                .ToList();
        }
        catch { return []; }
    }

    private static string Value(Dictionary<string, object?>? row, string key, string fallback = "Não sei informar")
        => row?.GetValueOrDefault(key)?.ToString()?.Trim() is { Length: > 0 } value ? value : fallback;

    public static HardwareProfile Detect()
    {
        var profile = new HardwareProfile();
        var cpu = Query("Win32_Processor", "Name", "Manufacturer").FirstOrDefault();
        var cpuManufacturer = Value(cpu, "Manufacturer");
        profile.CpuBrand = cpuManufacturer.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? "AMD"
            : cpuManufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? "Intel" : cpuManufacturer;
        profile.CpuModel = Value(cpu, "Name");

        var memory = Query("Win32_PhysicalMemory", "Capacity", "ConfiguredClockSpeed", "Speed");
        var bytes = memory.Aggregate(0UL, (total, row) => total +
            (ulong.TryParse(Value(row, "Capacity", "0"), out var amount) ? amount : 0UL));
        profile.RamGiB = (int)Math.Round(bytes / 1024d / 1024 / 1024);
        profile.RamMHz = memory.Select(row => int.TryParse(Value(row, "ConfiguredClockSpeed", "0"), out var speed) && speed > 0
            ? speed : int.TryParse(Value(row, "Speed", "0"), out speed) ? speed : 0).DefaultIfEmpty().Max();

        var gpus = Query("Win32_VideoController", "Name", "AdapterCompatibility", "DriverVersion")
            .Where(row => !Value(row, "Name", "").Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var gpu = gpus.FirstOrDefault();
        profile.GpuCount = gpus.Count;
        profile.GpuBrand = Value(gpu, "AdapterCompatibility");
        profile.GpuModel = Value(gpu, "Name");
        profile.GpuDriverVersion = Value(gpu, "DriverVersion", "Não identificado");

        var board = Query("Win32_BaseBoard", "Manufacturer", "Product").FirstOrDefault();
        profile.MotherboardBrand = Value(board, "Manufacturer");
        profile.MotherboardModel = Value(board, "Product");
        profile.OperatingSystem = Value(Query("Win32_OperatingSystem", "Caption").FirstOrDefault(), "Caption", "Windows");

        var physicalDisks = QueryScope(@"root\Microsoft\Windows\Storage", "MSFT_PhysicalDisk", "FriendlyName", "MediaType", "Size");
        profile.Disks = physicalDisks.Count > 0
            ? physicalDisks.Select(row => $"{Value(row, "FriendlyName")} · {DiskType(Value(row, "MediaType", "0"))} · {FormatSize(Value(row, "Size", "0"))}").ToList()
            : Query("Win32_DiskDrive", "Model", "MediaType", "Size")
                .Select(row => $"{Value(row, "Model")} · Tipo não identificado · {FormatSize(Value(row, "Size", "0"))}").ToList();
        return profile;
    }

    private static string DiskType(string code) => code switch
    {
        "3" => "HDD", "4" => "SSD", "5" => "SCM", _ => "Tipo não identificado"
    };

    private static string FormatSize(string bytes)
        => ulong.TryParse(bytes, out var size) ? $"{size / 1024d / 1024 / 1024:0} GB" : "Capacidade não identificada";
}
