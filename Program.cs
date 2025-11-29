using System;
using System.Linq;
using System.Globalization;
using TopVsBottom.Api.Services;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<FootballDataService>();



var app = builder.Build();

// Swagger config
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/topvsbottom/{league}", async (string league, FootballDataService footballService) =>
{
    try
    {
        // 1) tabela
        var standings = await footballService.GetStandings(league);
        var table = standings["standings"]?[0]?["table"] as JArray;
        if (table == null) return Results.BadRequest("Não foi possível carregar a tabela da liga.");

        var top4 = table.Take(4).Select(t => (string?)t["team"]?["name"]).ToList();
        var bottom4 = table.Reverse().Take(4).Select(t => (string?)t["team"]?["name"]).ToList();

        // 2) partidas
        var matches = await footballService.GetMatches(league);
        var list = matches["matches"] as JArray;
        if (list == null) return Results.BadRequest("Não foi possível carregar os jogos da liga.");

        DateTime now = DateTime.UtcNow;

        // 2.1) descobrir próxima matchday
        int? upcomingMatchday = list
            .Select(m =>
            {
                DateTime dt;
                var utc = (string?)m["utcDate"];
                bool parsed = DateTime.TryParse(
                    utc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt);

                return new
                {
                    Match = m,
                    Date = parsed ? dt : DateTime.MinValue,
                    Matchday = (int?)m["matchday"]
                };
            })
            .Where(x => x.Matchday != null && x.Date >= now)
            .OrderBy(x => x.Matchday)
            .Select(x => x.Matchday)
            .FirstOrDefault();

        if (upcomingMatchday == null)
            return Results.Ok(new { message = "Nenhuma próxima rodada disponível." });

        // 3) pegar somente jogos da matchday e futuros
        var nextRoundMatches = list
            .Select(m =>
            {
                DateTime dt;
                var utc = (string?)m["utcDate"];
                bool parsed = DateTime.TryParse(
                    utc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt);

                return new { Match = m, Date = parsed ? dt : DateTime.MinValue };
            })
            .Where(x =>
                (int?)x.Match["matchday"] == upcomingMatchday &&
                ((string?)x.Match["status"]) != "FINISHED" &&
                x.Date >= now)
            .ToList();

        // 4) filtrar Top4 x Bottom4 e converter horário
        var result = nextRoundMatches
            .Where(x =>
            {
                var match = x.Match;
                var home = (string?)match["homeTeam"]?["name"];
                var away = (string?)match["awayTeam"]?["name"];
                return (top4.Contains(home) && bottom4.Contains(away)) ||
                       (top4.Contains(away) && bottom4.Contains(home));
            })
            .Select(x =>
            {
                string? utc = (string?)x.Match["utcDate"];
                DateTime utcDate = DateTime.Parse(
                    utc!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                DateTime brDate = utcDate.AddHours(-3);

                return new
                {
                    Home = (string?)x.Match["homeTeam"]?["name"],
                    Away = (string?)x.Match["awayTeam"]?["name"],
                    Status = (string?)x.Match["status"],
                    Matchday = (int?)x.Match["matchday"],
                    UtcDate = utc,
                    LocalDateTimeBr = brDate.ToString("dd/MM/yyyy HH:mm")
                };
            })
            .OrderBy(m => m.LocalDateTimeBr) // ordenar por horário Brasil
            .ToList();

        return Results.Ok(new
        {
            upcomingMatchday,
            ranking = new { top4, bottom4 },
            matches = result
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// endpoint para listar ligas
app.MapGet("/leagues", async (FootballDataService footballService) =>
{
    try
    {
        var comps = await footballService.GetCompetitions();
        var competitions = comps["competitions"] as JArray;
        if (competitions == null) return Results.BadRequest("Não foi possível carregar as competições.");

        var result = competitions
            .Select(c => new
            {
                id = (int?)c["id"],
                name = (string?)c["name"],
                code = (string?)c["code"],
                area = (string?)c["area"]?["name"],
                plan = (string?)c["plan"]
            })
            .ToList();

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetLeagues")
.WithOpenApi();

app.Run();
