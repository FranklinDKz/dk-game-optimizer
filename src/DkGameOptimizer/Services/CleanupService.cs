namespace DkGameOptimizer.Services;

public enum CleanupArea { UserTemp, ShaderCache, NvidiaDxCache, NvidiaGlCache, AmdDxCache, AmdVkCache }

public sealed record CleanupItem(string Path, long Bytes, CleanupArea Area, DateTime CutoffUtc);
public sealed record CleanupScan(IReadOnlyList<CleanupItem> Items, bool Truncated)
{
    public long TotalBytes => Items.Sum(item => item.Bytes);
}
public sealed record CleanupResult(int Deleted, long FreedBytes, int Failed);

public static class CleanupService
{
    private const int MaxFiles = 100_000;

    public static string RootFor(CleanupArea area) => area switch
    {
        CleanupArea.UserTemp => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
        CleanupArea.ShaderCache => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
        CleanupArea.NvidiaDxCache => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "DXCache"),
        CleanupArea.NvidiaGlCache => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "GLCache"),
        CleanupArea.AmdDxCache => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMD", "DxCache"),
        CleanupArea.AmdVkCache => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMD", "VkCache"),
        _ => throw new ArgumentOutOfRangeException(nameof(area))
    };

    public static bool IsWithinRoot(string path, string root)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static CleanupScan Scan(bool includeTemp, bool includeShaders, bool includeGpuCache = false)
    {
        var items = new List<CleanupItem>();
        var truncated = false;
        if (includeTemp) Collect(CleanupArea.UserTemp, TimeSpan.FromDays(7));
        if (includeShaders) Collect(CleanupArea.ShaderCache, TimeSpan.FromDays(30));
        if (includeGpuCache)
        {
            Collect(CleanupArea.NvidiaDxCache, TimeSpan.FromDays(30));
            Collect(CleanupArea.NvidiaGlCache, TimeSpan.FromDays(30));
            Collect(CleanupArea.AmdDxCache, TimeSpan.FromDays(30));
            Collect(CleanupArea.AmdVkCache, TimeSpan.FromDays(30));
        }
        return new CleanupScan(items, truncated);

        void Collect(CleanupArea area, TimeSpan age)
        {
            var root = RootFor(area);
            if (!Directory.Exists(root)) return;
            var cutoff = DateTime.UtcNow - age;
            var directories = new Stack<string>();
            directories.Push(root);

            while (directories.Count > 0)
            {
                var directory = directories.Pop();
                try
                {
                    foreach (var child in Directory.EnumerateDirectories(directory))
                    {
                        if (area == CleanupArea.UserTemp && directory == root &&
                            Path.GetFileName(child).Equals(".net", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0 && IsWithinRoot(child, root))
                            directories.Push(child);
                    }
                    foreach (var path in Directory.EnumerateFiles(directory))
                    {
                        if (items.Count >= MaxFiles) { truncated = true; return; }
                        if (!IsWithinRoot(path, root)) continue;
                        var info = new FileInfo(path);
                        if ((info.Attributes & (FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.ReadOnly)) != 0) continue;
                        if (info.LastWriteTimeUtc >= cutoff) continue;
                        items.Add(new CleanupItem(path, info.Length, area, cutoff));
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
    }

    public static CleanupResult Delete(CleanupScan scan)
    {
        var deleted = 0;
        var freed = 0L;
        var failed = 0;
        foreach (var item in scan.Items)
        {
            try
            {
                if (!IsWithinRoot(item.Path, RootFor(item.Area))) { failed++; continue; }
                var info = new FileInfo(item.Path);
                if (!info.Exists || info.LastWriteTimeUtc >= item.CutoffUtc ||
                    (info.Attributes & (FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.ReadOnly)) != 0)
                {
                    failed++;
                    continue;
                }
                var length = info.Length;
                info.Delete();
                deleted++;
                freed += length;
            }
            catch (UnauthorizedAccessException) { failed++; }
            catch (IOException) { failed++; }
        }
        return new CleanupResult(deleted, freed, failed);
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        var units = new[] { "KB", "MB", "GB", "TB" };
        var value = (double)bytes;
        var index = -1;
        do { value /= 1024; index++; } while (value >= 1024 && index < units.Length - 1);
        return $"{value:0.#} {units[index]}";
    }
}
