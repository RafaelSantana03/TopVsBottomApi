using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace TopVsBottom.Api.Services
{
    public class FootballDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;

        public FootballDataService(HttpClient httpClient, IConfiguration config, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _config = config;
            _cache = cache;
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _config["FootballData:ApiKey"]);
        }

        private async Task<JObject> GetFromApi(string cacheKey, string url, TimeSpan expiration)
        {
            // Se estiver no cache → retorna
            if (_cache.TryGetValue(cacheKey, out JObject cached))
                return cached;

            // Se não estiver no cache → busca da API
            var json = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(json);

            // Salva no cache
            _cache.Set(cacheKey, data, expiration);

            return data;
        }

        public Task<JObject> GetStandings(string leagueCode)
        {
            string url = $"{_config["FootballData:BaseUrl"]}/competitions/{leagueCode}/standings";
            return GetFromApi(
                cacheKey: $"standings_{leagueCode}",
                url: url,
                expiration: TimeSpan.FromHours(12)
            );
        }

        public Task<JObject> GetMatches(string leagueCode)
        {
            string url = $"{_config["FootballData:BaseUrl"]}/competitions/{leagueCode}/matches";
            return GetFromApi(
                cacheKey: $"matches_{leagueCode}",
                url: url,
                expiration: TimeSpan.FromMinutes(5)
            );
        }

        public Task<JObject> GetCompetitions()
        {
            string url = $"{_config["FootballData:BaseUrl"]}/competitions";
            return GetFromApi(
                cacheKey: "competitions_all",
                url: url,
                expiration: TimeSpan.FromHours(24)
            );
        }
    }
}
