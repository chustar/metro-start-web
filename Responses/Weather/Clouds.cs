namespace MetroStart.Weather.Responses;

using Newtonsoft.Json;

public class Clouds
{
    [JsonProperty("all")]
    public long All { get; set; }
}
