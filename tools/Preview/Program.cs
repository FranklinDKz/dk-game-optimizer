using DkGameOptimizer.Models;
using DkGameOptimizer.Ui;

ApplicationConfiguration.Initialize();
Application.Run(new MainForm(new HardwareProfile
{
    CpuBrand = "AMD", CpuModel = "AMD Ryzen 5",
    RamGiB = 16, RamMHz = 3200,
    GpuBrand = "AMD", GpuModel = "Radeon RX 6600", GpuCount = 1,
    MotherboardBrand = "Placa-mãe", MotherboardModel = "A520",
    OperatingSystem = "Windows 11",
    GpuDriverVersion = "Versão de exemplo",
    Disks = ["SSD · 500 GB", "HDD · 1 TB"]
}));
