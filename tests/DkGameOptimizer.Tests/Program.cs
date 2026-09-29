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
    }),
    ("Ações avançadas possuem IDs e prévias explícitas", () =>
    {
        Check(ActionCatalog.All.Count == 11, "O catálogo de ações está incompleto.");
        Check(ActionCatalog.All.Select(item => item.Id).Distinct().Count() == ActionCatalog.All.Count,
            "Há IDs de ações repetidos.");
        Check(ActionCatalog.All.All(item => item.Preview.Length > 0 && item.SourceFile.Length > 0),
            "Há uma ação sem prévia ou origem.");
        Check(ActionCatalog.All.Where(item => item.Category is "GPU" or "RAM").All(item => item.CanRestore),
            "Ajuste de GPU ou RAM sem restauração.");
    }),
    ("Caches de GPU ficam em pastas de dados locais", () =>
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Check(CleanupService.RootFor(CleanupArea.NvidiaDxCache).StartsWith(local), "Cache NVIDIA fora do perfil.");
        Check(CleanupService.RootFor(CleanupArea.AmdDxCache).StartsWith(local), "Cache AMD fora do perfil.");
    }),
    ("Extração do acervo rejeita travessia de caminho", () =>
    {
        var root = Path.Combine(Path.GetTempPath(), "dk-bundle-test");
        Check(BundleService.IsSafeEntryPath(root, "2 - .Bats/CPU/ajuste.bat"), "Arquivo interno foi rejeitado.");
        Check(!BundleService.IsSafeEntryPath(root, "../outside.bat"), "Travessia foi aceita.");
        Check(!BundleService.IsSafeEntryPath(root, "C:/Windows/System32/teste.bat"), "Caminho absoluto foi aceito.");
    }),
    ("Acervo incorporado está legível quando presente", () =>
    {
        if (!BundleService.HasBundle) return;
        var entries = BundleService.List();
        Check(entries.Count > 0, "O pacote incorporado está vazio.");
        Check(BundleService.Sha256().Length == 64, "O pacote não pôde ser lido integralmente.");
        var textFile = entries.FirstOrDefault(entry => entry.Name.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));
        if (textFile is not null) Check(BundleService.ReadText(textFile.Name).Length > 0, "A prévia de texto falhou.");
        var destination = Path.Combine(Path.GetTempPath(), "dk-bundle-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destination);
        try
        {
            var result = BundleService.Extract(destination, entries[0].Name);
            var extracted = Path.Combine(result.Directory, entries[0].Name.Replace('/', Path.DirectorySeparatorChar));
            Check(result.Extracted == 1 && new FileInfo(extracted).Length == entries[0].Bytes,
                "A extração de um arquivo falhou.");
            var complete = BundleService.Extract(destination);
            Check(complete.Extracted == entries.Count && complete.Bytes == entries.Sum(entry => entry.Bytes),
                "A extração completa não corresponde ao catálogo.");
            Check(Directory.EnumerateFiles(complete.Directory, "*", SearchOption.AllDirectories).Count() == entries.Count,
                "Há arquivos faltando após a extração completa.");
        }
        finally
        {
            var tempRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(destination).StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase))
                Directory.Delete(destination, recursive: true);
        }
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
