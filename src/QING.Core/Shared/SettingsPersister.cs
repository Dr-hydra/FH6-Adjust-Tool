using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QING.Core;

public static class SettingsPersister
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "FH6AdjustTool"
    );
    private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

    public static Dictionary<string, object> Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (dict != null)
                {
                    var typedDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in dict)
                    {
                        if (kvp.Value is JsonElement elem)
                        {
                            switch (elem.ValueKind)
                            {
                                case JsonValueKind.String:
                                    typedDict[kvp.Key] = elem.GetString() ?? "";
                                    break;
                                case JsonValueKind.Number:
                                    if (elem.TryGetInt32(out int i)) typedDict[kvp.Key] = i;
                                    else if (elem.TryGetDouble(out double d)) typedDict[kvp.Key] = d;
                                    break;
                                case JsonValueKind.True:
                                    typedDict[kvp.Key] = true;
                                    break;
                                case JsonValueKind.False:
                                    typedDict[kvp.Key] = false;
                                    break;
                                default:
                                    typedDict[kvp.Key] = elem.ToString();
                                    break;
                            }
                        }
                        else
                        {
                            typedDict[kvp.Key] = kvp.Value;
                        }
                    }
                    return typedDict;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    public static void Save(Dictionary<string, object> settings)
    {
        try
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
