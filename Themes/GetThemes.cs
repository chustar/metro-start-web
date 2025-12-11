using MetroStart.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MetroStart.Themes;

public class GetThemes
{
    [Function("themes")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger<GetThemes> log)
    {
        var themes = await ThemeHelpers.GetAllThemes(log);

        return new OkObjectResult(
                themes.Select(t => new
                {
                    t.Author,
                    t.Title,
                    t.Online,
                    t.ThemeContent
                }));
    }
}