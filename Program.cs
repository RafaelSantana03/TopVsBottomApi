using System.Linq;
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

        var top4 = table.Take(4).Select(t => (string)t["team"]?["name"]).ToList();
        var bottom4 = table.Reverse().Take(4).Select(t => (string)t["team"]?["name"]).ToList();

        // 2) pegar partidas
        var matches = await footballService.GetMatches(league);
        var list = matches["matches"] as JArray;
        if (list == null) return Results.BadRequest("Não foi possível carregar os jogos da liga.");

        // 3) filtrar jogos Top4 x Bottom4
        var result = list
            .Where(m =>
            {
                var home = (string)m["homeTeam"]?["name"];
                var away = (string)m["awayTeam"]?["name"];
                return (top4.Contains(home) && bottom4.Contains(away))
                    || (top4.Contains(away) && bottom4.Contains(home));
            })
            .Select(m => new
            {
                Home = (string)m["homeTeam"]?["name"],
                Away = (string)m["awayTeam"]?["name"],
                Status = (string)m["status"],
                UtcDate = (string)m["utcDate"]
            })
            .ToList();

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();