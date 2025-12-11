using Azure.Data.Tables;
using MetroStart.Entities;
using MetroStart.Themes.Responses;
using Microsoft.Extensions.Logging;

namespace MetroStart.Helpers;

public static class ThemeHelpers
{
    public static async Task<TableClient> GetCloudTable(ILogger log)
    {
        if (System.Environment.GetEnvironmentVariable("METROSTART_TABLE_CONNECTION_STRING", EnvironmentVariableTarget.Process) is var connectionString)
        {
            var table = new TableClient(connectionString, "themes");

            // Create the table if it doesn't exist.
            await table.CreateIfNotExistsAsync();

            return table;
        }

        throw new ApplicationException("Could not find connectionString.");
    }

    public static ThemeEntity CreateThemeEntity(SharedTheme theme, ILogger log) => new ThemeEntity(
            theme.Author,
            theme.Title,
            theme.Online,
            theme.ThemeContent);

    public static ThemeEntity CreateThemeEntity(IDictionary<string, string> flatTheme, ILogger log)
    {
        string author = string.Empty;
        string title = string.Empty;
        bool online = true;
        Dictionary<string, string> themeContent = new Dictionary<string, string>();
        foreach (var (Key, Value) in flatTheme)
        {
            log.LogInformation($"Adding Key={Key}, Value={Value}");
            if (Key.Equals("author", StringComparison.InvariantCultureIgnoreCase))
            {
                author = Value;
            }
            else if (Key.Equals("title", StringComparison.InvariantCultureIgnoreCase))
            {
                title = Value;
            }
            else if (Key.Equals("online", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!bool.TryParse(Value, out online))
                {
                    online = true;
                }
            }
            else
            {
                themeContent.Add(Key, Value);
            }
        }

        return new ThemeEntity(author, title, online, themeContent);
    }

    public static async Task<ThemeEntity> InsertTheme(ThemeEntity themeEntity, TableClient table, ILogger log)
    {
        var response = table.AddEntity(themeEntity);
        return response.IsError ? throw new InvalidDataException($"Theme {themeEntity} was not saved") : themeEntity ;
    }

    public static async Task<List<ThemeEntity>> GetAllThemes(ILogger log)
    {
        var table = await GetCloudTable(log);
        var results = new List<ThemeEntity>();

        var queryResults = table.QueryAsync<ThemeEntity>();
        await foreach (var theme in queryResults.AsPages())
        {
            results.AddRange(theme.Values);
        }

        return results;
    }

    public static async Task<bool> ThemeExists(string title, ILogger log)
    {
        var table = await GetCloudTable(log);
        var results = table.QueryAsync((ThemeEntity t) => t.PartitionKey.Equals(title));
        await foreach (var theme in results)
        {
            return true;
        }
        return false;
    }
}