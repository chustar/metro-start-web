using MetroStart.Helpers;
using MetroStart.Themes.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MetroStart.Themes;

public class NewTheme
{
    [Function("newtheme")]
    public  async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger<NewTheme> log)
    {
        var sharedTheme = await req.ReadFromJsonAsync<SharedTheme>();
        if (sharedTheme == null)
        {
            return new BadRequestResult();
        }

        var themeEntity = ThemeHelpers.CreateThemeEntity(sharedTheme, log);
        if (await ThemeHelpers.ThemeExists(themeEntity.Title, log))
        {
            return new ConflictResult();
        }

        var table = await ThemeHelpers.GetCloudTable(log);
        await ThemeHelpers.InsertTheme(themeEntity, table, log);
        return new OkResult();
    }
}