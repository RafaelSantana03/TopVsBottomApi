using Newtonsoft.Json.Linq;

namespace TopVsBottom.Api.Services
{
    public class FootballDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public FootballDataService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _config["FootballData:ApiKey"]);
        }

        public async Task<JObject> GetStandings(string leagueCode)
        {
            var url = $"{_config["FootballData:BaseUrl"]}/competitions/{leagueCode}/standings";
            var json = await _httpClient.GetStringAsync(url);
            return JObject.Parse(json);
        }

        public async Task<JObject> GetMatches(string leagueCode)
        {
            var url = $"{_config["FootballData:BaseUrl"]}/competitions/{leagueCode}/matches";
            var json = await _httpClient.GetStringAsync(url);
            return JObject.Parse(json);
        }
    }
}
