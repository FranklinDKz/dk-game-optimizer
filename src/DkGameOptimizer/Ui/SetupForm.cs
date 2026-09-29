using DkGameOptimizer.Models;
using DkGameOptimizer.Services;

namespace DkGameOptimizer.Ui;

public sealed class SetupForm : Form
{
    private readonly HardwareProfile existing;
    private readonly Dictionary<string, TextBox> fields = [];
    private readonly Label detectionStatus;
    private readonly Button detectButton;
    public HardwareProfile? ResultProfile { get; private set; }

    public SetupForm(HardwareProfile? profile)
    {
        existing = profile ?? new HardwareProfile();
        Text = "DK Game Optimizer · Configuração inicial";
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        ClientSize = new Size(770, 690);
        MinimumSize = Size;
        MaximumSize = Size;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        Controls.Add(Theme.Label("DK", 36, 25, 74, 48, 24, Theme.Accent, FontStyle.Bold));
        Controls.Add(Theme.Label("GAME OPTIMIZER", 119, 33, 400, 30, 18, Theme.Text, FontStyle.Bold));
        Controls.Add(Theme.Label("Antes de começar, confira a configuração do seu computador.", 38, 92, 680, 30, 11, Theme.Muted));
        detectButton = Theme.Button("Detectar automaticamente", 38, 136, 250, 42, true);
        detectButton.Click += async (_, _) => await DetectAsync();
        Controls.Add(detectButton);
        detectionStatus = Theme.Label("Você também pode preencher os campos manualmente. Use “Não sei informar” onde precisar.",
            305, 140, 420, 50, 9, Theme.Muted);
        Controls.Add(detectionStatus);

        var panel = new Panel
        {
            Location = new Point(38, 197), Size = new Size(693, 402),
            AutoScroll = true, BackColor = Theme.Sidebar
        };
        Controls.Add(panel);
        AddSection(panel, "PROCESSADOR", 20, ("Marca", "CpuBrand", existing.CpuBrand), ("Modelo", "CpuModel", existing.CpuModel));
        AddSection(panel, "MEMÓRIA RAM", 112, ("Quantidade (GB)", "RamGiB", Number(existing.RamGiB)),
            ("Frequência (MHz)", "RamMHz", Number(existing.RamMHz)));
        AddSection(panel, "PLACA DE VÍDEO", 204, ("Quantidade", "GpuCount", Number(existing.GpuCount)),
            ("Marca", "GpuBrand", existing.GpuBrand), ("Modelo", "GpuModel", existing.GpuModel));
        AddSection(panel, "PLACA-MÃE", 296, ("Marca", "MotherboardBrand", existing.MotherboardBrand),
            ("Modelo", "MotherboardModel", existing.MotherboardModel));

        Controls.Add(Theme.Label("Desenvolvido por Franklin DK RP", 38, 632, 370, 28, 9, Theme.Muted));
        var start = Theme.Button("Salvar e abrir painel  →", 490, 620, 241, 45, true);
        start.Click += (_, _) => SaveAndOpen();
        Controls.Add(start);
        AcceptButton = start;
    }

    private static string Number(int value) => value > 0 ? value.ToString() : "Não sei informar";

    private void AddSection(Panel panel, string title, int y, params (string label, string key, string value)[] items)
    {
        panel.Controls.Add(Theme.Label(title, 20, y, 650, 24, 9, Theme.Accent, FontStyle.Bold));
        var width = items.Length == 3 ? 205 : 311;
        for (var i = 0; i < items.Length; i++)
        {
            var x = 20 + i * (width + 14);
            panel.Controls.Add(Theme.Label(items[i].label, x, y + 27, width, 21, 9, Theme.Muted));
            var input = Theme.Input(x, y + 50, width, items[i].value);
            panel.Controls.Add(input);
            fields[items[i].key] = input;
        }
    }

    private async Task DetectAsync()
    {
        detectButton.Enabled = false;
        detectionStatus.Text = "Identificando processador, memória, vídeo e placa-mãe...";
        try
        {
            var detected = await Task.Run(HardwareDetector.Detect);
            Set("CpuBrand", detected.CpuBrand); Set("CpuModel", detected.CpuModel);
            Set("RamGiB", Number(detected.RamGiB)); Set("RamMHz", Number(detected.RamMHz));
            Set("GpuCount", Number(detected.GpuCount)); Set("GpuBrand", detected.GpuBrand);
            Set("GpuModel", detected.GpuModel); Set("MotherboardBrand", detected.MotherboardBrand);
            Set("MotherboardModel", detected.MotherboardModel);
            existing.OperatingSystem = detected.OperatingSystem;
            existing.GpuDriverVersion = detected.GpuDriverVersion;
            existing.Disks = detected.Disks;
            detectionStatus.Text = "Detecção concluída. Revise os dados antes de continuar.";
        }
        catch (Exception error)
        {
            detectionStatus.Text = "Não foi possível detectar tudo: " + error.Message;
        }
        finally { detectButton.Enabled = true; }
    }

    private void Set(string key, string value) => fields[key].Text = value;
    private string Read(string key) => string.IsNullOrWhiteSpace(fields[key].Text) ? "Não sei informar" : fields[key].Text.Trim();
    private int ReadNumber(string key) => int.TryParse(fields[key].Text, out var value) && value > 0 ? value : 0;

    private void SaveAndOpen()
    {
        var profile = new HardwareProfile
        {
            CpuBrand = Read("CpuBrand"), CpuModel = Read("CpuModel"),
            RamGiB = ReadNumber("RamGiB"), RamMHz = ReadNumber("RamMHz"),
            GpuCount = ReadNumber("GpuCount"), GpuBrand = Read("GpuBrand"), GpuModel = Read("GpuModel"),
            MotherboardBrand = Read("MotherboardBrand"), MotherboardModel = Read("MotherboardModel"),
            OperatingSystem = existing.OperatingSystem, GpuDriverVersion = existing.GpuDriverVersion,
            Disks = existing.Disks, GamePaths = existing.GamePaths
        };
        try
        {
            ProfileStore.Save(profile);
            ResultProfile = profile;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception error)
        {
            MessageBox.Show(this, "Não foi possível salvar a configuração: " + error.Message,
                "Erro ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
