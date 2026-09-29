namespace DkGameOptimizer.Services;

public sealed record ActionSpec(
    string Id, string Category, string Title, string Description,
    string Preview, string SourceFile, bool RequiresAdmin,
    bool CanRestore, bool RequiresRestart = false);

public static class ActionCatalog
{
    public static readonly IReadOnlyList<ActionSpec> All =
    [
        new("game_dvr", "Registro", "Desativar capturas em segundo plano",
            "Desliga Game DVR e captura de jogos para o usuário atual. Você deixará de gravar clipes pelo Xbox Game Bar até restaurar.",
            "HKCU\\System\\GameConfigStore: GameDVR_Enabled = 0\nHKCU\\...\\GameDVR: AppCaptureEnabled = 0",
            "3 - Regedits/Desativar Game DVR 1 e 2.reg", false, true),
        new("visual_effects", "Registro", "Reduzir efeitos visuais",
            "Seleciona o perfil de desempenho e desliga a animação de janelas. Algumas mudanças aparecem após sair e entrar na sessão.",
            "HKCU\\...\\Explorer\\VisualEffects: VisualFXSetting = 2\nHKCU\\Control Panel\\Desktop\\WindowMetrics: MinAnimate = 0",
            "2 - .Bats/Windows/Desativar efeitos visuais.bat", false, true),
        new("transparency", "Registro", "Desativar transparência",
            "Desliga transparência da interface para o usuário atual. O efeito no desempenho costuma ser pequeno.",
            "HKCU\\...\\Themes\\Personalize: EnableTransparency = 0",
            "2 - .Bats/Windows/Desativar Transparência do Windows.bat", false, true),
        new("ram_cache", "RAM", "Cache de arquivos padrão",
            "Define LargeSystemCache como 0, a configuração comum em PCs de uso pessoal. Só altera se o valor atual for diferente.",
            "HKLM\\...\\Memory Management: LargeSystemCache = 0",
            "2 - .Bats/Memória RAM/Otimizar Memory Management.bat", true, true, true),
        new("gpu_hags", "GPU", "Desativar agendamento de GPU por hardware",
            "Teste para solucionar travamentos em alguns drivers. Compare antes e depois; reinicie para aplicar.",
            "HKLM\\...\\GraphicsDrivers: HwSchMode = 1",
            "7 - Otimizar Driver de Vídeo/AMD e NVIDIA/Desativar HGS.reg", true, true, true),
        new("gpu_mpo", "GPU", "Desativar MPO",
            "Ajuste de diagnóstico para cintilação e problemas de sobreposição de janelas. Reinicie para aplicar.",
            "HKLM\\SOFTWARE\\Microsoft\\Windows\\Dwm: OverlayTestMode = 5",
            "7 - Otimizar Driver de Vídeo/AMD, NVIDIA e INTEL/Desativar MPO", true, true, true),
        new("trim", "SSD / HDD", "Verificar e habilitar TRIM",
            "Consulta o estado do NTFS e habilita notificações de exclusão apenas se estiverem desligadas.",
            "fsutil behavior query DisableDeleteNotify\nfsutil behavior set DisableDeleteNotify 0 (somente se necessário)",
            "2 - .Bats/SSD/Habilitar TRIM.bat", true, true),
        new("optimize_drives", "SSD / HDD", "Otimizar unidades",
            "Executa a operação recomendada pelo Windows para cada unidade. Pode levar vários minutos ou mais.",
            "defrag /C /O /U /V",
            "2 - .Bats/SSD e HDD + ferramenta Otimizar Unidades do Windows", true, false),
        new("flush_dns", "Rede", "Limpar cache DNS",
            "Remove entradas DNS locais antigas. Não altera a velocidade contratada nem garante menor ping.",
            "ipconfig /flushdns",
            "6 - Otimizar Rede (ping)/Resetar Rede (use no fim).bat", true, false),
        new("restore_point", "Manutenção", "Criar ponto de restauração",
            "Solicita um ponto de restauração ao Windows antes de mudanças maiores. Depende da Proteção do Sistema estar ativa.",
            "PowerShell: Checkpoint-Computer -Description 'DK Game Optimizer' -RestorePointType MODIFY_SETTINGS",
            "1 - Criar Ponto de Restauração", true, false),
        new("repair_windows", "Manutenção", "Reparar arquivos do Windows",
            "Executa DISM e SFC em sequência. É uma correção de integridade e pode demorar bastante.",
            "DISM /Online /Cleanup-Image /RestoreHealth\nsfc /scannow",
            "9 - Otimização Semanal/Arrumar Windows.bat", true, false)
    ];

    public static ActionSpec Get(string id) => All.FirstOrDefault(item => item.Id == id)
        ?? throw new ArgumentException("Ação não reconhecida.", nameof(id));
}
