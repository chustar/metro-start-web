using MetroStart.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MetroStart.Themes;

public class LoadThemes
{
    [Function("loadthemes")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger<LoadThemes> log)
    {
        var table = await ThemeHelpers.GetCloudTable(log);
        var themeEntities = (await GetThemesFromGoogle(log)).Select(t => ThemeHelpers.CreateThemeEntity(t, log));
        foreach (var themeEntity in themeEntities)
        {
            try
            {
                await ThemeHelpers.InsertTheme(themeEntity, table, log);
            }
            catch (Exception e)
            {
                log.LogDebug($"Exception saving theme {themeEntity}: {e}");
            }
        }

        return new OkObjectResult(JsonConvert.SerializeObject(themeEntities));
    }

    static async Task<List<Dictionary<string, string>>> GetThemesFromGoogle(ILogger log)
    {
        var themesUrl = $"https://metro-start.appspot.com/themes.json";
        log.LogDebug($"Requesting themesUrl: {themesUrl}");

        var response = await new HttpClient().GetAsync($"{themesUrl}");
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync() ?? throw new InvalidDataException("Response was null");
        return JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(responseText) ?? throw new InvalidDataException("Deserialized object was null");
    }
}