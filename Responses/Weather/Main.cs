
namespace MetroStart.Weather.Responses;

using Newtonsoft.Json;

public class Main
{
    [JsonProperty("temp")]
    public double Temp { get; set; }

    [JsonProperty("pressure")]
    public long Pressure { get; set; }

    [JsonProperty("humidity")]
    public long Humidity { get; set; }

    [JsonProperty("temp_min")]
    public double TempMin { get; set; }

    [JsonProperty("temp_max")]
    public double TempMax { get; set; }
}
