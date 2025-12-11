using Azure;
using Azure.Data.Tables;
using MetroStart.Weather.Respnoses;
using Microsoft.Extensions.Logging;

namespace MetroStart.Entities
{
    public class WeatherEntity : ITableEntity
    {
        public WeatherEntity()
        {
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Location => RowKey;
        public string Units => PartitionKey;

        public DateTime CurrentWeatherModified { get; set; }
        public TimeSpan CurrentWeatherAge => DateTime.Now - CurrentWeatherModified;

        public DateTime WeatherForecastModified { get; set; }
        public TimeSpan WeatherForecastAge => DateTime.Now - WeatherForecastModified;

        public string CurrentWeatherJson { get; set; }
        public CurrentWeatherResponse? CurrentWeather
        {
            get => CurrentWeatherResponse.FromJson(CurrentWeatherJson);
            set
            {
                CurrentWeatherJson = value?.ToJson() ?? string.Empty;
                CurrentWeatherModified = DateTime.Now;
            }
        }

        public string WeatherForecastJson { get; set; }
        public WeatherForecastResponse? WeatherForecast
        {
            get => WeatherForecastResponse.FromJson(WeatherForecastJson);
            set
            {
                WeatherForecastJson = value?.ToJson() ?? string.Empty;
                WeatherForecastModified = DateTime.Now;
            }
        }

        public WeatherEntity(string location, string units, CurrentWeatherResponse currentWeather, WeatherForecastResponse weatherForecast)
        {
            PartitionKey = units;
            RowKey = location;

            CurrentWeather = currentWeather;
            CurrentWeatherModified = DateTime.Now;

            WeatherForecast = weatherForecast;
            WeatherForecastModified = DateTime.Now;
        }

        public static async Task<TableClient> GetCloudTable(ILogger log)
        {
            _ = log;
            if (Environment.GetEnvironmentVariable("METROSTART_TABLE_CONNECTION_STRING", EnvironmentVariableTarget.Process) is var connectionString)
            {
                var table = new TableClient(connectionString, "weather");

                // Create the table if it doesn't exist.
                await table.CreateIfNotExistsAsync();

                return table;
            }

            throw new ApplicationException("Could not find connectionString.");
        }
    }
}