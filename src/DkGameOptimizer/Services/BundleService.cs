using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace DkGameOptimizer.Services;

public sealed record BundleEntry(string Name, long Bytes);
public sealed record BundleExtraction(string Directory, int Extracted, long Bytes);

public static class BundleService
{
    private const string ResourceName = "OptimizerBundle.zip";
    private static readonly string[] TextExtensions = [".bat", ".reg", ".txt", ".ini"];

    public static bool HasBundle => typeof(BundleService).Assembly.GetManifestResourceInfo(ResourceName) is not null;

    public static IReadOnlyList<BundleEntry> List()
    {
        using var archive = OpenArchive();
        return archive.Entries
            .Where(entry => !entry.FullName.EndsWith('/'))
            .Select(entry => new BundleEntry(entry.FullName, entry.Length))
            .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static string ReadText(string name)
    {
        if (!TextExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Este tipo de arquivo não possui prévia em texto.");
        using var archive = OpenArchive();
        var entry = archive.GetEntry(name) ?? throw new FileNotFoundException("Arquivo não encontrado no pacote.");
        if (entry.Length > 1_000_000) throw new InvalidOperationException("Arquivo grande demais para a prévia.");
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { return Encoding.Latin1.GetString(bytes); }
    }

    public static BundleExtraction Extract(string chosenDirectory, string? selectedName = null)
    {
        if (!Directory.Exists(chosenDirectory)) throw new DirectoryNotFoundException("Escolha uma pasta existente.");
        using var archive = OpenArchive();
        var entries = archive.Entries.Where(entry => !entry.FullName.EndsWith('/') &&
            (selectedName is null || entry.FullName == selectedName)).ToList();
        if (entries.Count == 0) throw new FileNotFoundException("Nenhum arquivo correspondente no pacote.");

        var output = Path.Combine(chosenDirectory, "Optimizer-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") +
            "-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(output);
        var count = 0;
        var bytes = 0L;
        foreach (var entry in entries)
        {
            if (!IsSafeEntryPath(output, entry.FullName))
                throw new InvalidDataException("O pacote contém um caminho inválido.");
            var path = Path.GetFullPath(Path.Combine(output, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            entry.ExtractToFile(path, overwrite: false);
            count++;
            bytes += entry.Length;
        }
        return new BundleExtraction(output, count, bytes);
    }

    public static bool IsSafeEntryPath(string root, string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName) || Path.IsPathRooted(entryName)) return false;
        var normalized = entryName.Replace('/', Path.DirectorySeparatorChar);
        if (normalized.Split(Path.DirectorySeparatorChar).Any(part => part is ".." or "." or "")) return false;
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static string Sha256()
    {
        using var stream = OpenStream();
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static ZipArchive OpenArchive() => new(OpenStream(), ZipArchiveMode.Read);

    private static Stream OpenStream() => typeof(BundleService).Assembly.GetManifestResourceStream(ResourceName)
        ?? throw new InvalidOperationException("Este executável não contém o pacote pessoal.");
}
