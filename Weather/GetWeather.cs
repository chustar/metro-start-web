using MetroStart;
using MetroStart.Entities;
using MetroStart.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace metro_start_web.Weather;

public class GetWeather
{
    [Function("weather")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger<GetWeather> log)
    {
        string location = req.Query["location"].ToString();
        string units = req.Query["units"].ToString();

        List<Task<bool>> updateTasks = [];
        WeatherEntity weather = await WeatherHelpers.GetCachedWeather(location, units, log)
                                ?? new WeatherEntity(location, units, null, null);

        updateTasks.Add(Task.Run(async () =>
        {
            if (WeatherHelpers.CurrentWeatherRequiresUpdate(weather))
            {
                try
                {
                    weather.CurrentWeather = await WeatherHelpers.GetCurrentWeather(location, units, log);
                    weather.CurrentWeatherModified = DateTime.Now;
                    return true;
                }
                catch (Exception e)
                {
                    log.LogError(e, $"Could not update the current weather for: {location}, {units}");
                }
            }
            else
            {
                log.LogInformation($"Current weather update not required for: {location}, {units}");
            }
            return false;
        }));

        updateTasks.Add(Task.Run(async () =>
        {
            if (WeatherHelpers.WeatherForecastRequiresUpdate(weather))
            {
                try
                {
                    weather.WeatherForecast = await WeatherHelpers.GetWeatherForecast(location, units, log);
                    weather.WeatherForecastModified = DateTime.Now;
                    return true;
                }
                catch (Exception e)
                {
                    log.LogError(e, $"Could not update the weather forecast for: {location}, {units}");
                }
            }
            else
            {
                log.LogInformation($"Weather forecast update not required for: {location}, {units}");
            }
            return false;
        }));

        if ((await Task.WhenAll(updateTasks)).Any(res => res))
        {
            await WeatherHelpers.CacheWeather(weather, log);
        }

        var firstForecast = weather.WeatherForecast?.WeatherList?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(firstForecast, nameof(firstForecast));
        ArgumentNullException.ThrowIfNull(weather.CurrentWeather, nameof(weather.CurrentWeather));

        return new OkObjectResult(new
        {
            // Location
            weather.CurrentWeather.Name,
            weather.CurrentWeather.Sys.Country,

            // Temperatures
            weather.CurrentWeather.Main.Temp,
            firstForecast.Main.TempMax,
            firstForecast.Main.TempMin,
            weather.Units,

            // Conditions
            weather.CurrentWeather.Weather?.FirstOrDefault()?.Description,
            WindSpeed = firstForecast?.Wind?.Speed ?? 0,
            WindDeg = firstForecast?.Wind?.Deg ?? 0,
            Rain3h = firstForecast?.Rain?.ThreeHours ?? 0,
            Snow3h = firstForecast?.Snow?.ThreeHours ?? 0,

            // Ages
            EntityAge = (DateTime.Now - weather.Timestamp?.LocalDateTime)?.ToHumanReadableString(),
            CurrentWeatherAge = weather.CurrentWeatherAge.ToHumanReadableString(),
            WeatherForecastAge = weather.WeatherForecastAge.ToHumanReadableString()
        });
    }
}
