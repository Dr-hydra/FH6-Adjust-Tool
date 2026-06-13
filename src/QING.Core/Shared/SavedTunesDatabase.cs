using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QING.Core;

public class SavedTune
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public string CarSearchKeyword { get; set; } = "";
    public string SelectedCarText { get; set; } = "";
    public TuningState State { get; set; } = new();
    public TuningResult Result { get; set; } = new();
}

public static class SavedTunesDatabase
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "FH6AdjustTool"
    );
    private static readonly string SavedTunesFilePath = Path.Combine(AppDataFolder, "saved_tunes.json");

    private static List<SavedTune> _tunes = new();

    public static List<SavedTune> Tunes
    {
        get { return _tunes; }
    }

    public static void Initialize()
    {
        Load();
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SavedTunesFilePath))
            {
                string json = File.ReadAllText(SavedTunesFilePath);
                var list = JsonSerializer.Deserialize<List<SavedTune>>(json);
                if (list != null)
                {
                    _tunes = list;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading saved tunes: {ex.Message}");
            _tunes = new List<SavedTune>();
        }
    }

    public static void Save()
    {
        try
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
            string json = JsonSerializer.Serialize(_tunes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavedTunesFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving tunes: {ex.Message}");
        }
    }

    public static void SaveTune(SavedTune tune)
    {
        var existing = _tunes.Find(t => t.Id == tune.Id || t.Name.Equals(tune.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Name = tune.Name;
            existing.SavedAt = DateTime.Now;
            existing.CarSearchKeyword = tune.CarSearchKeyword;
            existing.SelectedCarText = tune.SelectedCarText;
            existing.State = tune.State;
            existing.Result = tune.Result;
            // Ensure ID is matched to avoid duplication when overwriting by name
            tune.Id = existing.Id;
        }
        else
        {
            _tunes.Add(tune);
        }
        Save();
    }

    public static void DeleteTune(string id)
    {
        _tunes.RemoveAll(t => t.Id == id);
        Save();
    }
}
