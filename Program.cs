using System;
using System.Linq;
using System.Globalization;
using TopVsBottom.Api.Services;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<FootballDataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/topvsbottom/{league}", async (string league, FootballDataService footballService) =>
{
    try
    {
        // 1) pegar tabela
        var standings = await footballService.GetStandings(league);
        var table = standings["standings"]?[0]?["table"] as JArray;
        if (table == null) return Results.BadRequest("Não foi possível carregar a tabela da liga.");

        var top4 = table.Take(4).Select(t => (string?)t["team"]?["name"]).ToList();
        var bottom4 = table.Reverse().Take(4).Select(t => (string?)t["team"]?["name"]).ToList();

        // 2) pegar partidas
        var matches = await footballService.GetMatches(league);
        var list = matches["matches"] as JArray;
        if (list == null) return Results.BadRequest("Não foi possível carregar os jogos da liga.");

        // 2.1) determinar próxima rodada (matchday)
        // tentativa 1: procurar jogos futuros (utcDate >= agora) e pegar o menor matchday dentre eles
        DateTime now = DateTime.UtcNow;
        int? upcomingMatchday = list
            .Select(m =>
            {
                // safe parsing of utcDate and matchday
                DateTime dt;
                var utc = (string?)m["utcDate"];
                bool parsed = DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt);
                return new
                {
                    Match = m,
                    Date = parsed ? dt : DateTime.MinValue,
                    Matchday = (int?)m["matchday"],
                    Status = (string?)m["status"]
                };
            })
            .Where(x => x.Matchday != null && x.Date >= now)
            .OrderBy(x => x.Matchday)
            .Select(x => x.Matchday)
            .FirstOrDefault();

        // tentativa 2 (fallback): se não houver partidas futuras (p. ex. temporada finalizada, ou todas com datas antigas),
        // procurar pelo menor matchday que contenha partidas com status diferente de FINISHED (por exemplo SCHEDULED/POSTPONED)
        if (upcomingMatchday == null)
        {
            upcomingMatchday = list
                .Where(m => ((string?)m["status"]) != "FINISHED")
                .Select(m => (int?)m["matchday"])
                .Where(md => md != null)
                .OrderBy(md => md)
                .FirstOrDefault();
        }

        if (upcomingMatchday == null)
        {
            // se ainda assim for nulo, significa que não conseguimos determinar uma próxima rodada
            return Results.Ok(new { message = "Não há próxima rodada disponível (todos os jogos podem estar finalizados ou dados insuficientes)." });
        }

        // 3) filtrar apenas jogos da próxima rodada
        var nextRoundMatches = list
            .Where(m => (int?)m["matchday"] == upcomingMatchday)
            .ToList();

        // 4) filtrar jogos Top4 x Bottom4 dentro dessa rodada
        var result = nextRoundMatches
            .Where(m =>
            {
                var home = (string?)m["homeTeam"]?["name"];
                var away = (string?)m["awayTeam"]?["name"];
                return (top4.Contains(home) && bottom4.Contains(away))
                    || (top4.Contains(away) && bottom4.Contains(home));
            })
            .Select(m => new
            {
                Home = (string?)m["homeTeam"]?["name"],
                Away = (string?)m["awayTeam"]?["name"],
                Status = (string?)m["status"],
                UtcDate = (string?)m["utcDate"],
                Matchday = (int?)m["matchday"]
            })
            .ToList();

        return Results.Ok(new
        {
            upcomingMatchday,
            matches = result
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Novo endpoint: listar todas as ligas/competições disponíveis na API
app.MapGet("/leagues", async (FootballDataService footballService) =>
{
    try
    {
        var comps = await footballService.GetCompetitions();
        var competitions = comps["competitions"] as JArray;
        if (competitions == null) return Results.BadRequest("Não foi possível carregar as competições.");

        var result = competitions.Select(c => new
        {
            id = (int?)c["id"],
            name = (string?)c["name"],
            code = (string?)c["code"],
            area = (string?)c["area"]?["name"],
            plan = (string?)c["plan"]
        }).ToList();

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).WithName("GetLeagues").WithOpenApi();

app.Run();