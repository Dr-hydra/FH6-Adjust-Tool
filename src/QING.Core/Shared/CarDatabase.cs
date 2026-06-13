using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QING.Core;

public class Car
{
    public string make { get; set; } = "";
    public string model { get; set; } = "";
    public string year { get; set; } = "";
    public string drive { get; set; } = "RWD";
    public string cls { get; set; } = "D";
    public double weight { get; set; } = 0; // Weight in kg/lbs
    public bool ev { get; set; } = false;
    public int pi { get; set; } = 100;
    public double? fd { get; set; } // final drive (optional in JSON)
    public List<double>? gears { get; set; } // gear ratios (optional in JSON)
}

public class CarDatabaseResponse
{
    public int version { get; set; }
    public string updated { get; set; } = "";
    public List<Car> cars { get; set; } = new();
}

public static class CarDatabase
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "FH6AdjustTool"
    );
    private static readonly string CacheFilePath = Path.Combine(AppDataFolder, "cars_cache.json");
    private static readonly string VersionFilePath = Path.Combine(AppDataFolder, "cars_version.txt");

    private const string PrimaryUrl = "https://raw.githubusercontent.com/Dr-hydra/FH6-Adjust-Tool/main/cars.json";
    private const string FallbackUrl = "https://raw.githubusercontent.com/super-android/tunelab/main/cars.json";

    public static List<Car> CarsList { get; private set; } = new();
    public static int CurrentVersion { get; private set; } = 0;
    public static string LastUpdated { get; private set; } = "";

    // Load initial cached data on startup
    public static void Initialize(string localSeedJsonPath)
    {
        try
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            // Try reading from cache file first
            if (File.Exists(CacheFilePath))
            {
                string json = File.ReadAllText(CacheFilePath);
                var response = JsonSerializer.Deserialize<CarDatabaseResponse>(json);
                if (response != null && response.cars.Count > 0)
                {
                    CarsList = response.cars;
                    CurrentVersion = response.version;
                    LastUpdated = response.updated;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading cache: {ex.Message}");
        }

        // Fallback to local seed JSON
        try
        {
            if (File.Exists(localSeedJsonPath))
            {
                string json = File.ReadAllText(localSeedJsonPath);
                var response = JsonSerializer.Deserialize<CarDatabaseResponse>(json);
                if (response != null)
                {
                    CarsList = response.cars;
                    CurrentVersion = response.version;
                    LastUpdated = response.updated;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading seed database: {ex.Message}");
        }
    }

    // Fetch update from GitHub asynchronously
    public static async Task<bool> FetchUpdatesAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        
        string? json = null;
        
        // Try Primary URL (User's fork)
        try
        {
            json = await client.GetStringAsync(PrimaryUrl);
        }
        catch
        {
            // Try Fallback URL (Upstream repository)
            try
            {
                json = await client.GetStringAsync(FallbackUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch car database from both sources: {ex.Message}");
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            var response = JsonSerializer.Deserialize<CarDatabaseResponse>(json);
            if (response == null || response.cars.Count == 0) return false;

            // If version is newer or equal, apply update
            if (response.version >= CurrentVersion)
            {
                CarsList = response.cars;
                CurrentVersion = response.version;
                LastUpdated = response.updated;

                // Save to cache
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }
                File.WriteAllText(CacheFilePath, json);
                File.WriteAllText(VersionFilePath, response.version.ToString());
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing updated database: {ex.Message}");
        }

        return false;
    }
}
