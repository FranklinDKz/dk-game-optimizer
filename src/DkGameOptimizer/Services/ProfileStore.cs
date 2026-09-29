using System.Text.Json;
using DkGameOptimizer.Models;

namespace DkGameOptimizer.Services;

public static class ProfileStore
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DKGameOptimizer");

    private static string ProfilePath => Path.Combine(DataDirectory, "setup.json");

    public static HardwareProfile? Load()
    {
        try
        {
            var profile = File.Exists(ProfilePath)
                ? JsonSerializer.Deserialize<HardwareProfile>(File.ReadAllText(ProfilePath))
                : null;
            if (profile is not null)
            {
                profile.GamePaths ??= [];
                profile.Disks ??= [];
            }
            return profile;
        }
        catch { return null; }
    }

    public static void Save(HardwareProfile profile)
    {
        Directory.CreateDirectory(DataDirectory);
        var temporary = ProfilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, ProfilePath, true);
    }
}
