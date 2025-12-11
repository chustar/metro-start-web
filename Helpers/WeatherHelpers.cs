using MetroStart.Entities;
using MetroStart.Weather.Respnoses;
using Microsoft.Extensions.Logging;

namespace MetroStart.Helpers;

public static class WeatherHelpers
{
    public static int ForecastDays = 3;
    public static HttpClient Client { get; } = new HttpClient();

    // Gets the weather entity cached with the provided location and units.
    public static async Task<WeatherEntity> GetCachedWeather(string location, string units, ILogger log)
    {
        var table = await WeatherEntity.GetCloudTable(log);
        var retrievedResult = await table.GetEntityAsync<WeatherEntity>(units, location);
        return retrievedResult.Value;
    }

    // Save the weather entity.
    public static async Task CacheWeather(WeatherEntity weather, ILogger log)
    {
        var table = await WeatherEntity.GetCloudTable(log);
        var result = table.AddEntity(weather);
        log.LogInformation($"Caching current weather with location: {weather.Location}, units: {weather.Units} and mod dates:({weather.WeatherForecastModified.ToLongTimeString()}, {weather.CurrentWeatherModified.ToLongTimeString()}) with result: {result.Status}");
    }

    // Checks if the provided weatherEntity's currnet weather should be updated.
    // returns: True if the current wether should be updated, false if it does not require updating.
    public static bool CurrentWeatherRequiresUpdate(WeatherEntity weather) => weather.CurrentWeather == null || weather.CurrentWeatherAge > TimeSpan.FromHours(3);

    // Checks if the provided weatherEntity's forecast should be updated.
    // returns: True if the weather forecast should be updated, false if it does not require updating.
    public static bool WeatherForecastRequiresUpdate(WeatherEntity weather) => weather.WeatherForecast == null || weather.WeatherForecastAge > TimeSpan.FromHours(9);

    // Gets the current weather from the remote service.
    // returns: The current weather response.
    public static async Task<CurrentWeatherResponse> GetCurrentWeather(string location, string units, ILogger log)
    {
        var responseText = await MakeOpenWeatherResponse($"weather?q={location}&units={units}", log);
        return CurrentWeatherResponse.FromJson(responseText) ?? throw new Exception($"No current weather response was found for location: {location}, units: {units}.");
    }

    // Gets the weather forecast from the remote service.
    // returns: The weather forecast response.
    public static async Task<WeatherForecastResponse> GetWeatherForecast(string location, string units, ILogger log)
    {
        var responseText = await MakeOpenWeatherResponse($"forecast?q={location}&units={units}", log);
        return WeatherForecastResponse.FromJson(responseText) ?? throw new Exception($"No weather forecast response was found for location: {location}, units: {units}.");
    }

    // Make a request for weather data from OpenWeatherMap.
    public static async Task<string> MakeOpenWeatherResponse(string urlAction, ILogger log)
    {
        var weatherKey = System.Environment.GetEnvironmentVariable("WEATHER_API_KEY", EnvironmentVariableTarget.Process);
        var weatherUrl = $"https://api.openweathermap.org/data/2.5/{urlAction}";
        log.LogInformation($"Requesting weatherUrl: {weatherUrl}");

        var response = await Client.GetAsync($"{weatherUrl}&APPID={weatherKey}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync() ?? throw new InvalidDataException("Response was null");
    }
}
