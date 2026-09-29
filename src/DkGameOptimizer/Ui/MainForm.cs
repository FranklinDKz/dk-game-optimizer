using System.Diagnostics;
using DkGameOptimizer.Models;
using DkGameOptimizer.Services;

namespace DkGameOptimizer.Ui;

public sealed class MainForm : Form
{
    private readonly HardwareProfile profile;
    private readonly Panel contentHost = new();
    private readonly Label statusLabel;
    private readonly Dictionary<string, Button> navigation = [];
    private CleanupScan? lastScan;
    private string activePage = "Visão geral";
    private bool actionRunning;

    public MainForm(HardwareProfile profile)
    {
        this.profile = profile;
        Text = "DK Game Optimizer · Franklin DK RP";
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1240, 840);
        MinimumSize = new Size(1050, 680);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 225));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Sidebar };
        sidebar.Controls.Add(Theme.Label("DK", 25, 27, 84, 47, 25, Theme.Accent, FontStyle.Bold));
        sidebar.Controls.Add(Theme.Label("GAME", 112, 31, 105, 25, 15, Theme.Text, FontStyle.Bold));
        sidebar.Controls.Add(Theme.Label("OPTIMIZER", 112, 53, 105, 21, 10, Theme.Accent, FontStyle.Bold));
        sidebar.Controls.Add(Theme.Label("SEU PAINEL", 25, 119, 170, 24, 9, Theme.Muted, FontStyle.Bold));
        AddNav(sidebar, "Visão geral", 155);
        AddNav(sidebar, "Jogos", 205);
        AddNav(sidebar, "Windows", 255);
        AddNav(sidebar, "Ações", 305);
        AddNav(sidebar, "Limpeza", 355);
        AddNav(sidebar, "Drivers", 405);
        if (BundleService.HasBundle) AddNav(sidebar, "Arquivos", 455);
        var setupButton = Theme.Button("Editar meu setup", 23, BundleService.HasBundle ? 528 : 478, 180, 40);
        setupButton.Click += (_, _) => EditSetup();
        sidebar.Controls.Add(setupButton);
        sidebar.Controls.Add(Theme.Label("Franklin DK RP", 25, 723, 180, 26, 9, Theme.Muted));
        sidebar.Resize += (_, _) => sidebar.Controls.OfType<Label>().Last().Top = sidebar.ClientSize.Height - 49;
        root.Controls.Add(sidebar, 0, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 73));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        header.Controls.Add(Theme.Label("DK Game Optimizer", 28, 20, 340, 33, 17, Theme.Text, FontStyle.Bold));
        header.Controls.Add(Theme.Label("WINDOWS 10 / 11", 760, 26, 190, 25, 9, Theme.Accent, FontStyle.Bold));
        right.Controls.Add(header, 0, 0);

        statusLabel = Theme.Label("Pronto para analisar este computador.", 25, 8, 930, 28, 9, Theme.Muted);
        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Sidebar };
        footer.Controls.Add(statusLabel);
        right.Controls.Add(footer, 0, 2);

        contentHost.Dock = DockStyle.Fill;
        contentHost.BackColor = Theme.Background;
        right.Controls.Add(contentHost, 0, 1);
        root.Controls.Add(right, 1, 0);
        Controls.Add(root);
        FormClosing += (_, _) => GamePriority.RestoreAll();
        ShowOverview();
        Shown += (_, _) =>
        {
            Navigate("Visão geral");
            navigation["Visão geral"].Focus();
        };
    }

    private void AddNav(Panel sidebar, string name, int y)
    {
        var button = Theme.Button(name, 15, y, 195, 43);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(13, 0, 0, 0);
        button.Click += (_, _) => Navigate(name);
        sidebar.Controls.Add(button);
        navigation[name] = button;
    }

    private void Navigate(string page)
    {
        activePage = page;
        foreach (var item in navigation)
        {
            item.Value.BackColor = item.Key == page ? Theme.Accent : Color.FromArgb(30, 48, 65);
            item.Value.ForeColor = item.Key == page ? Theme.Background : Theme.Text;
        }
        switch (page)
        {
            case "Jogos": ShowGames(); break;
            case "Windows": ShowWindows(); break;
            case "Ações": ShowActions(); break;
            case "Limpeza": ShowCleanup(); break;
            case "Drivers": ShowDrivers(); break;
            case "Arquivos" when BundleService.HasBundle: ShowBundle(); break;
            default: ShowOverview(); break;
        }
        BeginInvoke(() =>
        {
            if (activePage != page) return;
            var layout = contentHost.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            var flow = layout?.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            if (flow is not null) flow.AutoScrollPosition = Point.Empty;
            navigation[page].Focus();
        });
    }

    private FlowLayoutPanel Page(string eyebrow, string title, string subtitle)
    {
        contentHost.Controls.Clear();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 123));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var heading = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        heading.Controls.Add(Theme.Label(eyebrow, 30, 19, 700, 20, 9, Theme.Accent, FontStyle.Bold));
        heading.Controls.Add(Theme.Label(title, 29, 44, 800, 40, 22, Theme.Text, FontStyle.Bold));
        heading.Controls.Add(Theme.Label(subtitle, 31, 88, 850, 27, 10, Theme.Muted));
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true,
            Padding = new Padding(29, 10, 26, 24), BackColor = Theme.Background
        };
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(flow, 0, 1);
        contentHost.Controls.Add(layout);
        return flow;
    }

    private static void AddCard(FlowLayoutPanel flow, Panel card)
    {
        card.Width = Math.Max(730, flow.ClientSize.Width - 86);
        flow.Controls.Add(card);
        flow.Resize += (_, _) => card.Width = Math.Max(730, flow.ClientSize.Width - 86);
    }

    private static void AddSettingRow(Panel card, string label, string value, int y)
    {
        card.Controls.Add(Theme.Label(label.ToUpperInvariant(), 24, y, 165, 22, 9, Theme.Muted, FontStyle.Bold));
        card.Controls.Add(Theme.Label(value, 190, y - 2, card.Width - 215, 31, 10, Theme.Text));
    }

    private void ShowOverview()
    {
        var flow = Page("VISÃO GERAL", "Seu computador", "Confira o hardware informado e os recursos disponíveis antes de jogar.");
        var hardware = Theme.CardPanel(225);
        hardware.Controls.Add(Theme.Label("Configuração do setup", 24, 20, 620, 30, 15, Theme.Text, FontStyle.Bold));
        AddSettingRow(hardware, "Processador", profile.CpuModel, 62);
        AddSettingRow(hardware, "Memória", profile.RamGiB > 0 ? $"{profile.RamGiB} GB · {Number(profile.RamMHz, "MHz")}" : "Não sei informar", 94);
        AddSettingRow(hardware, "Placa de vídeo", $"{profile.GpuModel} · {Number(profile.GpuCount, "unidade(s)")}", 126);
        AddSettingRow(hardware, "Placa-mãe", $"{profile.MotherboardBrand} {profile.MotherboardModel}", 158);
        AddSettingRow(hardware, "Sistema", profile.OperatingSystem, 190);
        AddCard(flow, hardware);

        var drive = Theme.CardPanel(130 + Math.Max(1, profile.Disks.Count) * 27);
        drive.Controls.Add(Theme.Label("Armazenamento", 24, 18, 700, 30, 15, Theme.Text, FontStyle.Bold));
        var diskLines = profile.Disks.Count > 0 ? profile.Disks : ["Tipo de disco não identificado. Use a detecção automática no setup."];
        for (var i = 0; i < diskLines.Count; i++)
            drive.Controls.Add(Theme.Label("•  " + diskLines[i], 24, 58 + i * 27, 800, 24, 10, Theme.Muted));
        var systemDrive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
        drive.Controls.Add(Theme.Label($"Espaço livre em {systemDrive.Name}: {CleanupService.FormatBytes(systemDrive.AvailableFreeSpace)}",
            24, 75 + diskLines.Count * 27, 800, 27, 10, Theme.Accent));
        AddCard(flow, drive);

        var next = Theme.CardPanel(144);
        next.Controls.Add(Theme.Label("Por onde começar", 24, 19, 750, 29, 15, Theme.Text, FontStyle.Bold));
        next.Controls.Add(Theme.Label("Escolha um jogo para ver recomendações, revise o Modo de Jogo e faça a limpeza com prévia.",
            24, 54, 800, 29, 10, Theme.Muted));
        var games = Theme.Button("Ver jogos", 24, 90, 150, 37, true);
        games.Click += (_, _) => Navigate("Jogos");
        next.Controls.Add(games);
        AddCard(flow, next);
        flow.PerformLayout();
        flow.AutoScrollPosition = Point.Empty;
    }

    private static string Number(int value, string unit) => value > 0 ? $"{value} {unit}" : "Não identificado";

    private void ShowGames()
    {
        var flow = Page("BIBLIOTECA", "Perfis de jogos", "Recomendações para o seu setup, sem modificar arquivos dos jogos ou anticheat.");
        var choice = Theme.CardPanel(137);
        choice.Controls.Add(Theme.Label("Selecione um jogo", 24, 19, 600, 28, 15, Theme.Text, FontStyle.Bold));
        var combo = new ComboBox
        {
            Location = new Point(24, 65), Width = 660, Height = 36,
            DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(28, 45, 63),
            ForeColor = Theme.Text, Font = Theme.Font(10)
        };
        combo.Items.AddRange(GameCatalog.All.Select(game => game.Name).ToArray());
        choice.Controls.Add(combo);
        AddCard(flow, choice);

        var advice = Theme.CardPanel(245);
        AddCard(flow, advice);
        var executable = Theme.CardPanel(220);
        AddCard(flow, executable);

        void UpdateGame()
        {
            if (combo.SelectedIndex < 0) return;
            var game = GameCatalog.All[combo.SelectedIndex];
            advice.Controls.Clear();
            advice.Controls.Add(Theme.Label("Ajustes recomendados", 24, 18, 720, 31, 15, Theme.Text, FontStyle.Bold));
            var lines = GameCatalog.Advice(game, profile);
            advice.Height = 69 + lines.Count * 39;
            for (var i = 0; i < lines.Count; i++)
                advice.Controls.Add(Theme.Label("•  " + lines[i], 24, 56 + i * 39, 800, 39, 10, Theme.Muted));

            executable.Controls.Clear();
            executable.Controls.Add(Theme.Label("Executável do jogo", 24, 18, 720, 31, 15, Theme.Text, FontStyle.Bold));
            var path = profile.GamePaths.GetValueOrDefault(game.Name, "Nenhum executável selecionado");
            executable.Controls.Add(Theme.Label(path, 24, 54, 805, 32, 9, Theme.Muted));
            var choose = Theme.Button("Selecionar .exe", 24, 106, 175, 39, true);
            choose.Click += (_, _) =>
            {
                using var dialog = new OpenFileDialog { Filter = "Executável (*.exe)|*.exe", Title = $"Selecionar {game.Name}" };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                profile.GamePaths[game.Name] = dialog.FileName;
                ProfileStore.Save(profile);
                UpdateGame();
                SetStatus("Executável salvo. Configure a preferência de GPU nas opções de Gráficos do Windows.");
            };
            executable.Controls.Add(choose);
            var graphics = Theme.Button("Abrir opções de gráficos", 213, 106, 234, 39);
            graphics.Click += (_, _) => OpenWindows("ms-settings:display-advancedgraphics");
            executable.Controls.Add(graphics);
            var high = Theme.Button("Prioridade Alta na sessão", 24, 158, 234, 39);
            high.Click += (_, _) =>
            {
                try { SetStatus(GamePriority.Apply(profile.GamePaths.GetValueOrDefault(game.Name, ""))); }
                catch (Exception error) { SetStatus(error.Message); }
            };
            executable.Controls.Add(high);
            var normal = Theme.Button("Restaurar prioridade", 271, 158, 205, 39);
            normal.Click += (_, _) => SetStatus(GamePriority.Restore(profile.GamePaths.GetValueOrDefault(game.Name, "")));
            executable.Controls.Add(normal);
        }

        combo.SelectedIndexChanged += (_, _) => UpdateGame();
        combo.SelectedIndex = 0;
    }

    private void ShowWindows()
    {
        var flow = Page("AJUSTES DO SISTEMA", "Windows para jogar", "Altere apenas o que faz sentido para seu computador. As mudanças abaixo podem ser restauradas.");
        var gameMode = Theme.CardPanel(166);
        gameMode.Controls.Add(Theme.Label("Modo de Jogo", 24, 17, 640, 30, 15, Theme.Text, FontStyle.Bold));
        gameMode.Controls.Add(Theme.Label("Estado atual: " + SafeStatus(SystemTweaks.GameModeStatus), 24, 49, 700, 26, 10, Theme.Accent));
        gameMode.Controls.Add(Theme.Label("Ajusta a priorização de recursos do Windows durante jogos. Altera uma opção do usuário no Registro.",
            24, 76, 800, 28, 10, Theme.Muted));
        var enableMode = Theme.Button("Ativar", 24, 116, 130, 37, true);
        enableMode.Click += async (_, _) => await RunAction(SystemTweaks.EnableGameMode, ShowWindows);
        var restoreMode = Theme.Button("Restaurar anterior", 164, 116, 184, 37);
        restoreMode.Click += async (_, _) => await RunAction(SystemTweaks.RestoreGameMode, ShowWindows);
        gameMode.Controls.AddRange([enableMode, restoreMode]);
        AddCard(flow, gameMode);

        var power = Theme.CardPanel(185);
        power.Controls.Add(Theme.Label("Plano de energia", 24, 17, 650, 30, 15, Theme.Text, FontStyle.Bold));
        power.Controls.Add(Theme.Label("Plano ativo: " + SafeStatus(SystemTweaks.PowerStatus), 24, 49, 760, 26, 10, Theme.Accent));
        power.Controls.Add(Theme.Label("Ativa Alto desempenho, se o plano existir. Pode aumentar o consumo e a temperatura, especialmente em notebooks.",
            24, 77, 800, 42, 10, Theme.Muted));
        var enablePower = Theme.Button("Usar Alto desempenho", 24, 132, 215, 38, true);
        enablePower.Click += async (_, _) => await RunAction(SystemTweaks.EnableHighPerformance, ShowWindows);
        var restorePower = Theme.Button("Restaurar anterior", 251, 132, 184, 38);
        restorePower.Click += async (_, _) => await RunAction(SystemTweaks.RestorePowerPlan, ShowWindows);
        power.Controls.AddRange([enablePower, restorePower]);
        AddCard(flow, power);

        var manual = Theme.CardPanel(185);
        manual.Controls.Add(Theme.Label("Outros ajustes do Windows", 24, 17, 700, 30, 15, Theme.Text, FontStyle.Bold));
        manual.Controls.Add(Theme.Label("Revise a GPU por aplicativo, programas na inicialização e opções de armazenamento pelas telas oficiais do sistema.",
            24, 49, 800, 45, 10, Theme.Muted));
        var gpu = Theme.Button("Gráficos", 24, 118, 135, 39);
        gpu.Click += (_, _) => OpenWindows("ms-settings:display-advancedgraphics");
        var startup = Theme.Button("Inicialização", 171, 118, 155, 39);
        startup.Click += (_, _) => OpenWindows("ms-settings:startupapps");
        var gaming = Theme.Button("Modo de Jogo", 338, 118, 165, 39);
        gaming.Click += (_, _) => OpenWindows("ms-settings:gaming-gamemode");
        manual.Controls.AddRange([gpu, startup, gaming]);
        AddCard(flow, manual);
    }

    private static string SafeStatus(Func<string> action)
    {
        try { return action(); }
        catch { return "Não identificado"; }
    }

    private void ShowActions()
    {
        var flow = Page("EXECUÇÃO CONTROLADA", "Ações do otimizador",
            "Ações baseadas nos arquivos da pasta de referência. Veja o efeito e execute uma por vez.");
        var intro = Theme.CardPanel(112);
        intro.Controls.Add(Theme.Label("Antes de executar", 24, 17, 750, 30, 15, Theme.Text, FontStyle.Bold));
        intro.Controls.Add(Theme.Label("Mudanças no sistema pedem permissão de administrador. O programa salva os valores anteriores das ações reversíveis.",
            24, 52, 810, 47, 10, Theme.Muted));
        AddCard(flow, intro);

        foreach (var spec in ActionCatalog.All)
        {
            var card = Theme.CardPanel(193);
            card.Controls.Add(Theme.Label(spec.Category.ToUpperInvariant(), 24, 14, 770, 20, 9, Theme.Accent, FontStyle.Bold));
            card.Controls.Add(Theme.Label(spec.Title, 24, 37, 790, 29, 14, Theme.Text, FontStyle.Bold));
            card.Controls.Add(Theme.Label(spec.Description, 24, 68, 805, 37, 9, Theme.Muted));
            var preview = Theme.Label(spec.Preview, 24, 107, 805, 36, 8.3f, Theme.Warning);
            preview.Font = new Font("Consolas", 8.3f);
            card.Controls.Add(preview);
            var run = Theme.Button("Executar", 24, 151, 122, 32, true);
            run.Click += async (_, _) => await RunAdvancedAction(spec, false, run);
            card.Controls.Add(run);
            if (spec.CanRestore)
            {
                var restore = Theme.Button("Restaurar", 156, 151, 130, 32);
                restore.Click += async (_, _) => await RunAdvancedAction(spec, true, restore);
                card.Controls.Add(restore);
            }
            card.Controls.Add(Theme.Label($"Origem: {spec.SourceFile}" + (spec.RequiresRestart ? " · reinício recomendado" : ""),
                305, 156, 515, 24, 8, Theme.Muted));
            AddCard(flow, card);
        }
    }

    private async Task RunAdvancedAction(ActionSpec spec, bool restore, Button button)
    {
        if (actionRunning)
        {
            SetStatus("Aguarde a ação atual terminar.");
            return;
        }
        var verb = restore ? "Restaurar" : "Executar";
        var question = $"{verb} “{spec.Title}”?\n\n{spec.Description}\n\n{spec.Preview}";
        if (MessageBox.Show(this, question, "Confirmar ação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        actionRunning = true;
        button.Enabled = false;
        SetStatus($"{verb} {spec.Title}: em andamento... Aguarde o término.");
        try
        {
            string message;
            if (spec.RequiresAdmin && !AdvancedActions.IsAdministrator())
            {
                var token = Guid.NewGuid();
                var mode = restore ? "restore" : "apply";
                var info = new ProcessStartInfo(Application.ExecutablePath,
                    $"--run-action {spec.Id} {mode} {token:N}")
                { UseShellExecute = true, Verb = "runas" };
                using var process = Process.Start(info)
                    ?? throw new InvalidOperationException("Não foi possível iniciar a ação com permissão de administrador.");
                await process.WaitForExitAsync();
                var outcome = ActionWorker.Read(token);
                if (outcome is { Success: false }) throw new InvalidOperationException(outcome.Message);
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(outcome?.Message ?? "A ação elevada não foi concluída.");
                message = outcome?.Message ?? "Ação concluída. Consulte o histórico de execução para detalhes.";
            }
            else message = await AdvancedActions.ExecuteAsync(spec.Id, restore);
            SetStatus(message);
            MessageBox.Show(this, message, "Ação concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error)
        {
            SetStatus("Ação não concluída: " + error.Message);
            MessageBox.Show(this, error.Message, "Ação não concluída", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            actionRunning = false;
            if (!button.IsDisposed) button.Enabled = true;
        }
    }

    private async Task RunAction(Func<string> action, Action refresh)
    {
        Cursor = Cursors.WaitCursor;
        try
        {
            var message = await Task.Run(action);
            SetStatus(message);
            refresh();
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, "Ajuste não aplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SetStatus("Nenhuma alteração foi aplicada: " + error.Message);
        }
        finally { Cursor = Cursors.Default; }
    }

    private void ShowCleanup()
    {
        var flow = Page("MANUTENÇÃO", "Limpeza com prévia", "Arquivos pessoais, Downloads e jogos não entram na limpeza. Arquivos em uso são ignorados.");
        var options = Theme.CardPanel(272);
        options.Controls.Add(Theme.Label("Escolha o que verificar", 24, 18, 700, 30, 15, Theme.Text, FontStyle.Bold));
        var temp = new CheckBox
        {
            Text = "Temporários do usuário com mais de 7 dias", Checked = true,
            Location = new Point(24, 61), Size = new Size(650, 26),
            Font = Theme.Font(10), ForeColor = Theme.Text
        };
        var shader = new CheckBox
        {
            Text = "Cache de shaders DirectX com mais de 30 dias", Checked = false,
            Location = new Point(24, 91), Size = new Size(650, 26),
            Font = Theme.Font(10), ForeColor = Theme.Text
        };
        var gpuCache = new CheckBox
        {
            Text = "Cache de shaders NVIDIA / AMD com mais de 30 dias", Checked = false,
            Location = new Point(24, 121), Size = new Size(650, 26),
            Font = Theme.Font(10), ForeColor = Theme.Text
        };
        options.Controls.AddRange([temp, shader, gpuCache]);
        options.Controls.Add(Theme.Label("O cache de shaders será recompilado quando necessário; o primeiro uso pode apresentar travamentos temporários.",
            24, 153, 800, 39, 9, Theme.Warning));
        var scanButton = Theme.Button("Analisar arquivos", 24, 208, 177, 39, true);
        options.Controls.Add(scanButton);
        AddCard(flow, options);

        var preview = Theme.CardPanel(176);
        preview.Controls.Add(Theme.Label("Prévia", 24, 19, 700, 30, 15, Theme.Text, FontStyle.Bold));
        var summary = Theme.Label("Clique em “Analisar arquivos” para ver a quantidade e o espaço recuperável.",
            24, 58, 805, 47, 10, Theme.Muted);
        preview.Controls.Add(summary);
        var delete = Theme.Button("Limpar arquivos analisados", 24, 117, 254, 39);
        delete.Enabled = false;
        preview.Controls.Add(delete);
        AddCard(flow, preview);

        scanButton.Click += async (_, _) =>
        {
            if (!temp.Checked && !shader.Checked && !gpuCache.Checked)
            {
                SetStatus("Selecione pelo menos uma categoria para analisar.");
                return;
            }
            scanButton.Enabled = false;
            delete.Enabled = false;
            summary.Text = "Analisando arquivos...";
            try
            {
                lastScan = await Task.Run(() => CleanupService.Scan(temp.Checked, shader.Checked, gpuCache.Checked));
                summary.Text = $"{lastScan.Items.Count:N0} arquivo(s) · {CleanupService.FormatBytes(lastScan.TotalBytes)} recuperáveis" +
                    (lastScan.Truncated ? " · análise limitada a 100 mil arquivos" : "");
                delete.Enabled = lastScan.Items.Count > 0;
                SetStatus("Análise concluída. Revise a prévia antes de limpar.");
            }
            catch (Exception error)
            {
                summary.Text = "Não foi possível concluir a análise: " + error.Message;
            }
            finally { scanButton.Enabled = true; }
        };

        delete.Click += async (_, _) =>
        {
            if (lastScan is null || lastScan.Items.Count == 0) return;
            var answer = MessageBox.Show(this,
                $"Excluir {lastScan.Items.Count:N0} arquivo(s) analisados ({CleanupService.FormatBytes(lastScan.TotalBytes)})?\n\nA exclusão não pode ser desfeita.",
                "Confirmar limpeza", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            delete.Enabled = false;
            summary.Text = "Limpando arquivos...";
            var result = await Task.Run(() => CleanupService.Delete(lastScan));
            summary.Text = $"{result.Deleted:N0} removido(s) · {CleanupService.FormatBytes(result.FreedBytes)} liberados · {result.Failed:N0} ignorado(s)";
            SetStatus("Limpeza finalizada. Arquivos bloqueados ou alterados desde a análise foram ignorados.");
            lastScan = null;
        };

        var storage = Theme.CardPanel(155);
        storage.Controls.Add(Theme.Label("Ferramentas do Windows", 24, 18, 700, 30, 15, Theme.Text, FontStyle.Bold));
        storage.Controls.Add(Theme.Label("Para arquivos do sistema e otimização de SSD/HDD, use as ferramentas que reconhecem o tipo de unidade.",
            24, 52, 800, 37, 10, Theme.Muted));
        var storageButton = Theme.Button("Armazenamento", 24, 100, 171, 39);
        storageButton.Click += (_, _) => OpenWindows("ms-settings:storagesense");
        var drives = Theme.Button("Otimizar unidades", 207, 100, 189, 39);
        drives.Click += (_, _) => OpenProgram("dfrgui.exe");
        storage.Controls.AddRange([storageButton, drives]);
        AddCard(flow, storage);
    }

    private void ShowDrivers()
    {
        var flow = Page("DRIVERS", "Placa de vídeo e atualizações", "Verifique versões e obtenha drivers diretamente do fabricante do seu hardware.");
        var gpu = Theme.CardPanel(159);
        gpu.Controls.Add(Theme.Label("GPU detectada", 24, 18, 700, 30, 15, Theme.Text, FontStyle.Bold));
        AddSettingRow(gpu, "Modelo", profile.GpuModel, 62);
        AddSettingRow(gpu, "Fabricante", profile.GpuBrand, 92);
        AddSettingRow(gpu, "Driver", profile.GpuDriverVersion, 122);
        AddCard(flow, gpu);

        var sources = Theme.CardPanel(208);
        sources.Controls.Add(Theme.Label("Downloads oficiais", 24, 18, 700, 30, 15, Theme.Text, FontStyle.Bold));
        sources.Controls.Add(Theme.Label("Confirme o modelo antes de instalar. Este aplicativo não baixa nem instala drivers automaticamente.",
            24, 52, 800, 45, 10, Theme.Muted));
        var nvidia = Theme.Button("NVIDIA", 24, 108, 130, 39);
        nvidia.Click += (_, _) => OpenWindows("https://www.nvidia.com/Download/index.aspx");
        var amd = Theme.Button("AMD", 166, 108, 120, 39);
        amd.Click += (_, _) => OpenWindows("https://www.amd.com/en/support/download/drivers.html");
        var intel = Theme.Button("Intel", 298, 108, 120, 39);
        intel.Click += (_, _) => OpenWindows("https://www.intel.com/content/www/us/en/download-center/home.html");
        var update = Theme.Button("Windows Update", 430, 108, 170, 39, true);
        update.Click += (_, _) => OpenWindows("ms-settings:windowsupdate");
        sources.Controls.AddRange([nvidia, amd, intel, update]);
        AddCard(flow, sources);

        var note = Theme.CardPanel(112);
        note.Controls.Add(Theme.Label("Acompanhe o resultado", 24, 17, 700, 29, 15, Theme.Text, FontStyle.Bold));
        note.Controls.Add(Theme.Label("Anote FPS médio, 1% low, temperatura e latência antes e depois de cada mudança. Nem todo ajuste melhora todos os PCs.",
            24, 51, 805, 46, 10, Theme.Muted));
        AddCard(flow, note);
    }

    private void ShowBundle()
    {
        var entries = BundleService.List();
        var total = entries.Sum(entry => entry.Bytes);
        var flow = Page("ACERVO PESSOAL", "Arquivos incorporados",
            $"{entries.Count} arquivos · {CleanupService.FormatBytes(total)} · conteúdo da pasta Optimizer neste executável.");
        var note = Theme.CardPanel(85);
        note.Controls.Add(Theme.Label("Consulte antes de usar", 24, 17, 750, 29, 15, Theme.Text, FontStyle.Bold));
        note.Controls.Add(Theme.Label("O acervo inclui programas de terceiros e scripts que mudam o sistema. Eles não são executados pelo aplicativo. A extração é manual.",
            24, 47, 810, 34, 9, Theme.Muted));
        AddCard(flow, note);

        var card = Theme.CardPanel(420);
        card.Controls.Add(Theme.Label("Buscar pelo nome ou pasta", 24, 17, 750, 26, 12, Theme.Text, FontStyle.Bold));
        var search = Theme.Input(24, 52, 824);
        card.Controls.Add(search);
        var list = new ListBox
        {
            Location = new Point(24, 98), Size = new Size(824, 225),
            BackColor = Theme.Sidebar, ForeColor = Theme.Text,
            Font = Theme.Font(9), BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false, DisplayMember = nameof(BundleEntry.Name)
        };
        list.DataSource = entries.ToList();
        card.Controls.Add(list);
        search.TextChanged += (_, _) =>
        {
            list.DataSource = entries.Where(entry => entry.Name.Contains(search.Text,
                StringComparison.CurrentCultureIgnoreCase)).ToList();
        };
        var preview = Theme.Button("Ver texto", 24, 339, 133, 38);
        preview.Click += (_, _) =>
        {
            if (list.SelectedItem is not BundleEntry item) return;
            try { ShowBundleText(item.Name, BundleService.ReadText(item.Name)); }
            catch (Exception error) { MessageBox.Show(this, error.Message, "Prévia indisponível", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        };
        var selected = Theme.Button("Extrair selecionado", 170, 339, 188, 38);
        selected.Click += async (_, _) =>
        {
            if (list.SelectedItem is BundleEntry item) await ExtractBundle(item.Name);
        };
        var all = Theme.Button("Extrair todos", 371, 339, 168, 38, true);
        all.Click += async (_, _) => await ExtractBundle(null);
        card.Controls.AddRange([preview, selected, all]);
        card.Controls.Add(Theme.Label("Arquivos extraídos ficam em uma nova pasta Optimizer com data e hora.",
            24, 389, 800, 25, 9, Theme.Muted));
        AddCard(flow, card);
    }

    private void ShowBundleText(string name, string content)
    {
        using var window = new Form
        {
            Text = name, Size = new Size(930, 710), MinimumSize = new Size(600, 400),
            StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Background
        };
        window.Controls.Add(new TextBox
        {
            Text = content, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both,
            WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas", 9),
            BackColor = Theme.Sidebar, ForeColor = Theme.Text
        });
        window.ShowDialog(this);
    }

    private async Task ExtractBundle(string? name)
    {
        var description = name is null ? "todos os arquivos" : name;
        if (MessageBox.Show(this, $"Extrair {description}? Os arquivos poderão ser executados fora do aplicativo.",
            "Confirmar extração", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        using var folder = new FolderBrowserDialog { Description = "Escolha a pasta de destino" };
        if (folder.ShowDialog(this) != DialogResult.OK) return;
        SetStatus("Extraindo arquivos... Aguarde.");
        try
        {
            var result = await Task.Run(() => BundleService.Extract(folder.SelectedPath, name));
            var message = $"{result.Extracted} arquivo(s) extraído(s) em {result.Directory}.";
            SetStatus(message);
            MessageBox.Show(this, message, "Extração concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error)
        {
            SetStatus("A extração não foi concluída: " + error.Message);
            MessageBox.Show(this, error.Message, "Falha na extração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void EditSetup()
    {
        using var setup = new SetupForm(profile);
        if (setup.ShowDialog(this) != DialogResult.OK || setup.ResultProfile is null) return;
        var updated = setup.ResultProfile;
        profile.CpuBrand = updated.CpuBrand; profile.CpuModel = updated.CpuModel;
        profile.RamGiB = updated.RamGiB; profile.RamMHz = updated.RamMHz;
        profile.GpuBrand = updated.GpuBrand; profile.GpuModel = updated.GpuModel;
        profile.GpuCount = updated.GpuCount; profile.MotherboardBrand = updated.MotherboardBrand;
        profile.MotherboardModel = updated.MotherboardModel; profile.OperatingSystem = updated.OperatingSystem;
        profile.GpuDriverVersion = updated.GpuDriverVersion; profile.Disks = updated.Disks;
        Navigate(activePage);
        SetStatus("Configuração do setup atualizada.");
    }

    private void SetStatus(string message) => statusLabel.Text = message;

    private void OpenWindows(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception error) { SetStatus("Não foi possível abrir: " + error.Message); }
    }

    private void OpenProgram(string fileName)
    {
        try { Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true }); }
        catch (Exception error) { SetStatus("Não foi possível abrir: " + error.Message); }
    }
}
