namespace DkGameOptimizer.Models;

public enum GameStyle { Competitive, OpenWorld, Racing, Sports, Survival, Other }

public sealed record GameProfile(string Name, GameStyle Style, string Note);

public static class GameCatalog
{
    public static readonly IReadOnlyList<GameProfile> All =
    [
        new("GTA V", GameStyle.OpenWorld, "Use o benchmark interno para ajustar distância de visão e qualidade de sombras."),
        new("FiveM", GameStyle.OpenWorld, "Em servidores com muitos recursos, monitore RAM e armazenamento livre antes de reduzir gráficos."),
        new("Call of Duty: Warzone", GameStyle.Competitive, "Ajuste o limite de FPS de acordo com a taxa do monitor e verifique o uso de VRAM no jogo."),
        new("VALORANT", GameStyle.Competitive, "Priorize estabilidade de FPS e configure a taxa correta de atualização do monitor."),
        new("CS:GO / Counter-Strike 2", GameStyle.Competitive, "Use o contador de FPS e procure estabilidade antes de aumentar o limite de quadros."),
        new("Assetto Corsa", GameStyle.Racing, "Ajuste reflexos, sombras e quantidade de carros visíveis conforme seu hardware."),
        new("Forza Horizon 6", GameStyle.Racing, "Compare os presets no benchmark do jogo e mantenha espaço livre no SSD."),
        new("EA Sports FC 27", GameStyle.Sports, "Prefira tela cheia e verifique se o limite de FPS acompanha o monitor."),
        new("ARK: Survival Evolved", GameStyle.Survival, "Distância de visão e sombras costumam pesar mais em áreas complexas."),
        new("Delta Force", GameStyle.Competitive, "Ajuste escala de resolução e limite de FPS dentro do jogo."),
        new("Red Dead Redemption 2", GameStyle.OpenWorld, "Use o benchmark interno e ajuste iluminação volumétrica e reflexos."),
        new("Tom Clancy's Ghost Recon Breakpoint", GameStyle.OpenWorld, "Monitore VRAM e prefira texturas compatíveis com a placa de vídeo."),
        new("Euro Truck Simulator 2", GameStyle.Racing, "Reduza espelhos e escala de resolução se houver quedas em cidades."),
        new("171", GameStyle.OpenWorld, "Comece com preset médio e aumente opções uma por vez, medindo o FPS."),
        new("DayZ", GameStyle.Survival, "Mantenha drivers atualizados e compare desempenho em áreas urbanas."),
        new("eFootball", GameStyle.Sports, "Use a taxa de atualização nativa do monitor e teste o limite de FPS."),
        new("PUBG: Battlegrounds", GameStyle.Competitive, "Busque consistência de FPS; texturas podem ficar mais altas se houver VRAM suficiente."),
        new("Free Fire (emulador)", GameStyle.Competitive, "Configure CPU, RAM e resolução dentro do emulador sem exceder os recursos disponíveis."),
        new("Naruto x Boruto: Ultimate Ninja Storm Connections", GameStyle.Other, "Confira resolução, modo de tela e limite de FPS disponíveis no jogo."),
        new("Naruto Shippuden: Ultimate Ninja Storm 4", GameStyle.Other, "Use a resolução nativa e reduza efeitos se houver quedas de quadros."),
        new("Enlisted", GameStyle.Competitive, "Ajuste escala de renderização e sombras para equilibrar visibilidade e FPS.")
    ];

    public static IReadOnlyList<string> Advice(GameProfile game, HardwareProfile hardware)
    {
        var advice = new List<string>
        {
            game.Note,
            "Ative o Modo de Jogo e compare o desempenho antes e depois. Os ajustes do Windows podem ser revertidos nesta ferramenta."
        };

        if (hardware.RamGiB > 0 && hardware.RamGiB < 16)
            advice.Add("Com menos de 16 GB de RAM, feche aplicativos pesados em segundo plano antes de jogar. Não desative o arquivo de paginação.");
        else if (hardware.RamGiB >= 16)
            advice.Add("Sua RAM permite manter o Windows gerenciando a memória. Limpar a RAM à força tende a causar mais recarregamentos.");

        if (game.Style is GameStyle.OpenWorld or GameStyle.Survival)
            advice.Add("Se o jogo estiver em HDD e houver SSD disponível, mover a instalação pode melhorar carregamentos e reduzir travamentos por leitura.");

        if (game.Style == GameStyle.Competitive)
            advice.Add("Prefira rede cabeada quando possível e use o medidor de latência do próprio jogo para avaliar sua conexão.");

        return advice;
    }
}
