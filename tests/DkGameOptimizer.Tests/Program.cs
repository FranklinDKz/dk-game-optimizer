using DkGameOptimizer.Models;
using DkGameOptimizer.Services;

var checks = new (string name, Action run)[]
{
    ("Catálogo sem nomes repetidos", () =>
    {
        Check(GameCatalog.All.Count == 21, "O catálogo deve ter 21 jogos.");
        Check(GameCatalog.All.Select(game => game.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 21,
            "O catálogo contém nomes repetidos.");
    }),
    ("Recomendações variam com a RAM", () =>
    {
        var game = GameCatalog.All.First(item => item.Name == "FiveM");
        var lowRam = GameCatalog.Advice(game, new HardwareProfile { RamGiB = 8 });
        var enoughRam = GameCatalog.Advice(game, new HardwareProfile { RamGiB = 16 });
        Check(lowRam.Any(line => line.Contains("menos de 16 GB")), "A recomendação de pouca RAM não apareceu.");
        Check(enoughRam.Any(line => line.Contains("Limpar a RAM à força")), "A recomendação de RAM suficiente não apareceu.");
    }),
    ("Limpeza rejeita caminhos fora da pasta", () =>
    {
        var root = Path.Combine(Path.GetTempPath(), "dk-check-root");
        Check(CleanupService.IsWithinRoot(Path.Combine(root, "sub", "old.tmp"), root), "Arquivo interno foi rejeitado.");
        Check(!CleanupService.IsWithinRoot(Path.Combine(root, "..", "outside.tmp"), root), "Caminho com travessia foi aceito.");
        Check(!CleanupService.IsWithinRoot(root + "-other\\old.tmp", root), "Pasta com prefixo parecido foi aceita.");
    }),
    ("Formatação do espaço recuperável", () =>
    {
        Check(CleanupService.FormatBytes(0) == "0 B", "Zero bytes incorreto.");
        Check(CleanupService.FormatBytes(1536) == "1,5 KB" || CleanupService.FormatBytes(1536) == "1.5 KB",
            "Conversão de kilobytes incorreta.");
    })
};

var failures = 0;
foreach (var (name, run) in checks)
{
    try { run(); Console.WriteLine("OK  " + name); }
    catch (Exception error) { failures++; Console.WriteLine("FALHOU  " + name + ": " + error.Message); }
}
Console.WriteLine($"\n{checks.Length - failures}/{checks.Length} verificações passaram.");
return failures == 0 ? 0 : 1;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
