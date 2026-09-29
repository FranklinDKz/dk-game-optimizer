namespace DkGameOptimizer.Models;

public sealed class HardwareProfile
{
    public string CpuBrand { get; set; } = "Não sei informar";
    public string CpuModel { get; set; } = "Não sei informar";
    public int RamGiB { get; set; }
    public int RamMHz { get; set; }
    public string GpuBrand { get; set; } = "Não sei informar";
    public string GpuModel { get; set; } = "Não sei informar";
    public int GpuCount { get; set; }
    public string MotherboardBrand { get; set; } = "Não sei informar";
    public string MotherboardModel { get; set; } = "Não sei informar";
    public string OperatingSystem { get; set; } = "Windows";
    public string GpuDriverVersion { get; set; } = "Não identificado";
    public List<string> Disks { get; set; } = [];
    public Dictionary<string, string> GamePaths { get; set; } = [];
}
