using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace QING.Core;

public class ChatMessage
{
    public string Role { get; set; } = "user"; // "user" or "model"/"assistant"
    public string Content { get; set; } = "";
}

public interface IAiClient
{
    Task<string> EnhanceTuneAsync(string apiKey, TuningState s, TuningResult r, string model, string customUrl);
    Task<string> ChatAsync(string apiKey, List<ChatMessage> history, string model, string customUrl, string systemPrompt);
    Task<bool> ValidateKeyAsync(string apiKey, string model, string customUrl);
}

public static class AiClientFactory
{
    public static IAiClient GetClient(string provider)
    {
        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) || 
            provider.Equals("OpenAI-Compatible", StringComparison.OrdinalIgnoreCase))
        {
            return new OpenAiClient();
        }
        return new GoogleGeminiClient();
    }
}

public class GoogleGeminiClient : IAiClient
{
    public async Task<string> EnhanceTuneAsync(string apiKey, TuningState s, TuningResult r, string model, string customUrl)
    {
        var (sys, usr) = GeminiClient.BuildEnhancePrompt(s, r);
        
        string activeModel = string.IsNullOrWhiteSpace(model) || model.Equals("default", StringComparison.OrdinalIgnoreCase)
            ? "3.1-flash"
            : model;

        string rawResponse = await CallGeminiInternalAsync(apiKey, sys, usr, activeModel, customUrl);
        return ExtractJson(rawResponse);
    }

    public async Task<string> ChatAsync(string apiKey, List<ChatMessage> history, string model, string customUrl, string systemPrompt)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        string activeModel = string.IsNullOrWhiteSpace(model) || model.Equals("default", StringComparison.OrdinalIgnoreCase)
            ? "3.1-flash"
            : model;

        string baseUrl = string.IsNullOrWhiteSpace(customUrl) 
            ? "https://generativelanguage.googleapis.com" 
            : customUrl.TrimEnd('/');

        string url = $"{baseUrl}/v1beta/models/{activeModel}:generateContent?key={apiKey}";

        var contentsList = new List<object>();
        foreach (var msg in history)
        {
            string role = msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
            contentsList.Add(new
            {
                role = role,
                parts = new[] { new { text = msg.Content } }
            });
        }

        var requestBody = new
        {
            contents = contentsList.ToArray(),
            systemInstruction = string.IsNullOrWhiteSpace(systemPrompt) ? null : new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048
            }
        };

        string jsonPayload = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseContent = await response.Content.ReadAsStringAsync();
        return ParseGeminiResponse(responseContent);
    }

    public async Task<bool> ValidateKeyAsync(string apiKey, string model, string customUrl)
    {
        try
        {
            string baseUrl = string.IsNullOrWhiteSpace(customUrl) 
                ? "https://generativelanguage.googleapis.com" 
                : customUrl.TrimEnd('/');
            string url = $"{baseUrl}/v1beta/models?key={apiKey}&pageSize=2";
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> CallGeminiInternalAsync(string apiKey, string sys, string usr, string model, string customUrl)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        string baseUrl = string.IsNullOrWhiteSpace(customUrl) 
            ? "https://generativelanguage.googleapis.com" 
            : customUrl.TrimEnd('/');
        string url = $"{baseUrl}/v1beta/models/{model}:generateContent?key={apiKey}";
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = $"{sys}\n\n{usr}" }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.4,
                maxOutputTokens = 4096
            }
        };

        string jsonPayload = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseContent = await response.Content.ReadAsStringAsync();
        return ParseGeminiResponse(responseContent);
    }

    private string ParseGeminiResponse(string responseContent)
    {
        string trimmed = responseContent.Trim();
        if (trimmed.StartsWith("<"))
        {
            throw new Exception("API 接口返回了非 JSON 格式的 HTML 响应（通常是网络代理错误或认证拦截页面）。请检查您的 API 地址或网络配置。");
        }
        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content))
                {
                    if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var textProp = parts[0].GetProperty("text");
                        return textProp.GetString() ?? "";
                    }
                }
            }
            throw new Exception("Unexpected response structure from Gemini API.");
        }
        catch (JsonException)
        {
            throw new Exception($"无法解析 API 返回的 JSON 响应。这通常是因为 API 接口地址 (Base URL) 不正确或服务异常。原始响应为: {(responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent)}");
        }
    }

    private string ExtractJson(string rawResponse)
    {
        int firstBrace = rawResponse.IndexOf('{');
        int lastBrace = rawResponse.LastIndexOf('}');
        if (firstBrace != -1 && lastBrace > firstBrace)
        {
            string extracted = rawResponse.Substring(firstBrace, lastBrace - firstBrace + 1);
            extracted = Regex.Replace(extracted, @"```json\s*", "", RegexOptions.IgnoreCase);
            extracted = Regex.Replace(extracted, @"```\s*", "");
            return extracted.Trim();
        }
        return rawResponse.Trim();
    }
}

public class OpenAiClient : IAiClient
{
    public async Task<string> EnhanceTuneAsync(string apiKey, TuningState s, TuningResult r, string model, string customUrl)
    {
        var (sys, usr) = GeminiClient.BuildEnhancePrompt(s, r);

        string activeModel = string.IsNullOrWhiteSpace(model) || model.Equals("default", StringComparison.OrdinalIgnoreCase)
            ? "gpt-5.4"
            : model;

        string rawResponse = await CallOpenAiInternalAsync(apiKey, sys, usr, activeModel, customUrl);
        return ExtractJson(rawResponse);
    }

    public async Task<string> ChatAsync(string apiKey, List<ChatMessage> history, string model, string customUrl, string systemPrompt)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        string activeModel = string.IsNullOrWhiteSpace(model) || model.Equals("default", StringComparison.OrdinalIgnoreCase)
            ? "gpt-5.4"
            : model;

        string baseUrl = string.IsNullOrWhiteSpace(customUrl) 
            ? "https://api.openai.com/v1" 
            : customUrl.TrimEnd('/');
        string url = $"{baseUrl}/chat/completions";

        var messagesList = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messagesList.Add(new { role = "system", content = systemPrompt });
        }

        foreach (var msg in history)
        {
            messagesList.Add(new { role = msg.Role.ToLower(), content = msg.Content });
        }

        var requestBody = new
        {
            model = activeModel,
            messages = messagesList.ToArray(),
            temperature = 0.7,
            max_tokens = 2048
        };

        string jsonPayload = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseContent = await response.Content.ReadAsStringAsync();
        return ParseOpenAiResponse(responseContent);
    }

    public async Task<bool> ValidateKeyAsync(string apiKey, string model, string customUrl)
    {
        try
        {
            string activeModel = string.IsNullOrWhiteSpace(model) || model.Equals("default", StringComparison.OrdinalIgnoreCase)
                ? "gpt-5.4"
                : model;

            string baseUrl = string.IsNullOrWhiteSpace(customUrl) 
                ? "https://api.openai.com/v1" 
                : customUrl.TrimEnd('/');
            string url = $"{baseUrl}/chat/completions";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var requestBody = new
            {
                model = activeModel,
                messages = new[] { new { role = "user", content = "ping" } },
                max_tokens = 5
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> CallOpenAiInternalAsync(string apiKey, string sys, string usr, string model, string customUrl)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        string baseUrl = string.IsNullOrWhiteSpace(customUrl) 
            ? "https://api.openai.com/v1" 
            : customUrl.TrimEnd('/');
        string url = $"{baseUrl}/chat/completions";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = sys },
                new { role = "user", content = usr }
            },
            temperature = 0.4,
            max_tokens = 4096
        };

        string jsonPayload = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseContent = await response.Content.ReadAsStringAsync();
        return ParseOpenAiResponse(responseContent);
    }

    private string ParseOpenAiResponse(string responseContent)
    {
        string trimmed = responseContent.Trim();
        if (trimmed.StartsWith("<"))
        {
            throw new Exception("API 接口返回了非 JSON 格式的 HTML 响应（通常是网络代理错误或认证拦截页面）。请检查您的 API 地址或网络配置。");
        }
        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? "";
                    }
                }
            }
            throw new Exception("Unexpected response structure from OpenAI API.");
        }
        catch (JsonException)
        {
            throw new Exception($"无法解析 API 返回的 JSON 响应。这通常是因为 API 接口地址 (Base URL) 不正确或服务异常。原始响应为: {(responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent)}");
        }
    }

    private string ExtractJson(string rawResponse)
    {
        int firstBrace = rawResponse.IndexOf('{');
        int lastBrace = rawResponse.LastIndexOf('}');
        if (firstBrace != -1 && lastBrace > firstBrace)
        {
            string extracted = rawResponse.Substring(firstBrace, lastBrace - firstBrace + 1);
            extracted = Regex.Replace(extracted, @"```json\s*", "", RegexOptions.IgnoreCase);
            extracted = Regex.Replace(extracted, @"```\s*", "");
            return extracted.Trim();
        }
        return rawResponse.Trim();
    }
}

public static class GeminiClient
{
    private static readonly string[] Models = new[]
    {
        "gemini-2.5-flash",
        "gemini-2.5-flash-lite",
        "gemini-flash-latest",
        "gemini-2.0-flash"
    };

    public static async Task<string> CallGeminiAsync(string apiKey, string sys, string usr)
    {
        var client = new GoogleGeminiClient();
        return await client.EnhanceTuneAsync(apiKey, new TuningState(), new TuningResult(), "3.1-flash", "");
    }

    public static async Task<string> EnhanceTuneAsync(string apiKey, TuningState s, TuningResult r)
    {
        var client = new GoogleGeminiClient();
        return await client.EnhanceTuneAsync(apiKey, s, r, "3.1-flash", "");
    }

    public static async Task<bool> ValidateKeyAsync(string apiKey)
    {
        var client = new GoogleGeminiClient();
        return await client.ValidateKeyAsync(apiKey, "3.1-flash", "");
    }

    public static (string sys, string usr) BuildEnhancePrompt(TuningState s, TuningResult r)
    {
        var allValues = new List<string>();
        
        Action<string, TuningCategory?> addCategory = (name, cat) =>
        {
            if (cat == null) return;
            foreach (var val in cat.Values)
            {
                allValues.Add($"{name}/{val.Key}: {val.Value}");
            }
        };

        addCategory("Tires", r.Tires);
        addCategory("Gearing", r.Gearing);
        addCategory("Alignment", r.Alignment);
        addCategory("Suspension", r.Suspension);
        addCategory("ARB", r.ARB);
        addCategory("Damping", r.Damping);
        addCategory("Braking", r.Braking);
        addCategory("Diff", r.Diff);
        addCategory("Aero", r.Aero);

        var presentSections = new List<string>();
        if (r.Tires != null) presentSections.Add("Tires");
        if (r.Gearing != null) presentSections.Add("Gearing");
        if (r.Alignment != null) presentSections.Add("Alignment");
        if (r.Suspension != null) presentSections.Add("Suspension");
        if (r.ARB != null) presentSections.Add("ARB");
        if (r.Damping != null) presentSections.Add("Damping");
        if (r.Braking != null) presentSections.Add("Braking");
        if (r.Diff != null) presentSections.Add("Diff");
        if (r.Aero != null) presentSections.Add("Aero");
        string sections = string.Join(",", presentSections);

        string sys = @"You are a Forza Horizon 6 tuning expert. Return ONLY a raw JSON object. No markdown, no backticks, no text before or after. Start with { and end with }.

FH6 META KNOWLEDGE - apply this when evaluating tunes:
TIRES: Slick 28-32.5psi, Semi-slick 27-29.5psi, Street/Rally 24-26.5psi, Off-road 15.5-21psi. D/C class = stock tires. B = stock/street. A = street/semi-slick. S1/S2 = semi-slick/slick.
ARB: Start both at max, soften to reach mechanical balance 0.55-0.65. Stiffer front = understeer. Stiffer rear = oversteer. Off-road = both near minimum. High-power RWD often needs softer rear.
SPRINGS: Set at 1/3-1/2 slider range. Heavier end gets stiffer spring. Stiffer front = understeer, stiffer rear = oversteer. Off-road = soft both ends.
DAMPING: Bump should be 30-55% of rebound (target ~40%). Heavier springs = stiffer dampers. Softer front damping reduces understeer. Softer rear reduces oversteer.
DIFF AWD: Front 85%/0%, Rear 55-75%/10-15%, Center 70-80% rear bias. FWD: 85%/0%. RWD: 55-75%/10-18%.
GEARING: Final drive so car just hits rev limiter in top gear at end of longest straight. 1st gear for controllable wheelspin at launch. Logarithmic spread - more gap between lower gears, less at top.
ALIGNMENT: Camber 0 to -1.0° front/rear as baseline. Caster 6.5-7.0°. Rear toe-in only for snap oversteer on RWD.
RIDE HEIGHT: Start at minimum for road. Off-road start at maximum. Raise only if bottoming out.
AERO: Balance stat 0.40-0.45. RWD slight rear bias. FWD/AWD slight front bias. Only matters at high speed.

Structure:
{
  ""notes"": { ""Section/Key"": ""note including specific adjustment if needed e.g. try reducing by 2"" },
  ""tips"":  { ""Section"": ""one concrete actionable tip"" },
  ""summary"": ""2-3 sentences: what this tune does well, what to adjust first, why""
}

Rules:
- Output all notes, tips, and summary in Simplified Chinese (简体中文).
- notes keys MUST match Section/Key exactly as provided (e.g. ""Gearing/Final Drive"" not just ""Gearing"")
- Each gear in Gearing gets its own specific note - do not repeat the same note for every gear
- Where a value seems wrong for the mode or meta, suggest a specific change (e.g. ""减少 3"", ""尝试 28.5 psi"")
- Flag values outside expected FH6 ranges
- Keep each note under 12 words
- Be specific to the car, drivetrain, class, and tune mode given";

        string usr = $"Car: {s.Make} {s.Model} | {s.DriveType} | {s.TuneId} mode | {s.Surface} | {s.WeightDist}%F weight | {s.Pi}{s.CarClass} | {s.InputDevice}\n\nTune values:\n{string.Join("\n", allValues)}\n\nSections present: {sections}";

        return (sys, usr);
    }
}
