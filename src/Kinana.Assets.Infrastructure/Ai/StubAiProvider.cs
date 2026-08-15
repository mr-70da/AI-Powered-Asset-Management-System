using System.Text.Json;
using Kinana.AssetManagement.Application.Ai;

namespace Kinana.AssetManagement.Infrastructure.Ai;

/// <summary>
/// Deterministic local stub used when <c>Ai:Provider = "stub"</c> (the default,
/// so the whole AI path can be exercised without provider credentials — R4.6).
/// It interprets the question with simple keyword rules and returns the exact
/// same strict-JSON <see cref="AssetSearchIntent"/> contract as the real
/// provider, so the rest of the pipeline (parse → resolve → read → answer) is
/// identical regardless of which provider is configured.
/// </summary>
public sealed class StubAiProvider : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => "stub";

    public Task<string> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        var intent = Interpret(request.UserPrompt);
        return Task.FromResult(JsonSerializer.Serialize(intent, JsonOptions));
    }

    private static AssetSearchIntent Interpret(string question)
    {
        var text = question.ToLowerInvariant().Trim();

        if (IsGreeting(text))
        {
            return new AssetSearchIntent
            {
                IntentType = "answer",
                Answer = "Hello! I can answer questions about your asset register — for example: " +
                    "'Show me all laptops assigned to Presales', 'Which assets are currently available?', " +
                    "or 'How many Dell laptops do we have?'."
            };
        }

        if (IsUnrelated(text))
        {
            return new AssetSearchIntent
            {
                IntentType = "answer",
                Answer = "I'm only able to answer questions about the asset register (assets, " +
                    "assignments, departments, statuses and purchase cost). Try asking about one of those."
            };
        }

        var intent = new AssetSearchIntent
        {
            IntentType = IsValueQuestion(text) ? "value" : "assetSearch",
            CountOnly = text.StartsWith("how many", StringComparison.Ordinal) || text.Contains("how many"),
            SearchTerm = FindManufacturer(text),
            Status = FindStatus(text),
            CategoryName = FindCategory(text),
            AssetTypeName = FindAssetType(text),
            DepartmentName = FindDepartment(text),
            AssignedEmployeeName = FindEmployee(text)
        };

        return intent;
    }

    private static bool IsGreeting(string text)
        => text is "hi" or "hello" or "hey" or "good morning" or "good afternoon"
            || text.StartsWith("hi ") || text.StartsWith("hello ") || text.StartsWith("hey ")
            || text.Contains("thank you") || text.Contains("thanks")
            || text.Contains("who are you") || text.Contains("what can you do");

    private static bool IsUnrelated(string text)
        => text.Length <= 2
            || text.Contains("weather")
            || text.Contains("joke")
            || text.Contains("recipe")
            || text.Contains("write code")
            || text.Contains("ignore this prompt");

    private static bool IsValueQuestion(string text)
        => text.Contains("cost") || text.Contains("price") || text.Contains("value")
            || text.Contains("worth") || text.Contains("portfolio") || text.Contains("spend");

    private static string? FindManufacturer(string text)
    {
        if (text.Contains("dell")) return "Dell";
        if (text.Contains("lenovo")) return "Lenovo";
        if (text.Contains("thinkpad")) return "ThinkPad";
        if (text.Contains("elitebook") || text.Contains("probrand") || text.Contains("hp") || text.Contains("elitedesk")) return "HP";
        if (text.Contains("apple") || text.Contains("iphone") || text.Contains("macbook")) return "Apple";
        if (text.Contains("cisco")) return "Cisco";
        if (text.Contains("canon")) return "Canon";
        if (text.Contains("microsoft")) return "Microsoft";
        return null;
    }

    private static string? FindStatus(string text)
    {
        if (text.Contains("retired")) return "Retired";
        if (text.Contains("available")) return "Available";
        if (text.Contains("maintenance")) return "Under Maintenance";
        if (text.Contains("assigned")) return "Assigned";
        return null;
    }

    private static string? FindCategory(string text)
    {
        if (text.Contains("computer")) return "Computers";
        if (text.Contains("network")) return "Networking";
        if (text.Contains("office equipment") || text.Contains("office")) return "Office Equipment";
        if (text.Contains("software")) return "Software";
        return null;
    }

    private static string? FindAssetType(string text)
    {
        if (text.Contains("laptop")) return "Laptop";
        if (text.Contains("desktop")) return "Desktop";
        if (text.Contains("monitor")) return "Monitor";
        if (text.Contains("printer")) return "Printer";
        if (text.Contains("phone")) return "Phone";
        if (text.Contains("switch")) return "Switch";
        if (text.Contains("server")) return "Server";
        if (text.Contains("license") || text.Contains("licence")) return "Software License";
        return null;
    }

    private static string? FindDepartment(string text)
    {
        if (text.Contains("presales")) return "Presales";
        if (text.Contains("delivery")) return "Delivery";
        if (text.Contains("finance")) return "Finance";
        if (text.Contains("human resources") || text.Contains(" hr")) return "Human Resources";
        if (text.Contains("operations")) return "Operations";
        return null;
    }

    private static string? FindEmployee(string text)
    {
        if (text.Contains("ahmed")) return "Ahmed Hassan";
        if (text.Contains("sara")) return "Sara Mohamed";
        if (text.Contains("omar")) return "Omar Khalil";
        if (text.Contains("nour")) return "Nour Adel";
        if (text.Contains("mostafa")) return "Mostafa El-Sayed";
        return null;
    }
}
