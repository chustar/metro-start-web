namespace MetroStart.Weather.Respnoses;

using Newtonsoft.Json;

public class Coord
{
    [JsonProperty("lon")]
    public double Lon { get; set; }

    [JsonProperty("lat")]
    public double Lat { get; set; }
}
