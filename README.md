# ⚽ TopVsBottom API

API desenvolvida com .NET para identificar partidas onde times do **TOP 4** enfrentam times do **BOTTOM 4** na próxima rodada de uma liga de futebol.

A API utiliza os dados da plataforma **Football-Data.org**, com cache automático para reduzir chamadas externas e melhorar performance.

---

## 🚀 Funcionalidades

| Endpoint | Função |
|---------|--------|
| `/topvsbottom/{league}` | Retorna apenas os confrontos da próxima rodada entre times do Top 4 vs Bottom 4 |
| `/leagues` | Lista todas as ligas/competições disponíveis na API |

---

## 🔍 Exemplo de resposta (`/topvsbottom/PL`)

```json
{
  "upcomingMatchday": 21,
  "matches": [
    {
      "home": "Manchester City",
      "away": "Burnley",
      "status": "SCHEDULED",
      "utcDate": "2025-02-02T17:30:00Z",
      "matchday": 21
    }
  ]
}
```

🧠 Regras aplicadas no cálculo

Para cada liga:

Busca a tabela de classificação

Seleciona Top 4 e Bottom 4

Busca todas as partidas da liga

Determina a próxima rodada com base no horário atual

Filtra apenas partidas:

Não finalizadas

Que acontecem na próxima rodada

Com confronto Top 4 x Bottom 4


## 🕒 Cache automático
| Tipo                      | Tempo de cache |
| ------------------------- | -------------- |
| Standings (classificação) | 12 horas       |
| Matches (partidas)        | 10 minutos     |
| Leagues (competições)     | 24 horas       |


# 🔧 Como executar o projeto localmente
## 1️⃣ Clonar o repositório
git clone https://github.com/RafaelSantana03/TopVsBottomApi.git

## 2️⃣ Acessar o projeto
cd TopVsBottomApi

## 3️⃣ Configurar a chave da API
Inserir no appsettings.json:

{
  "FOOTBALL_DATA_API_KEY": "SUA_CHAVE_AQUI"
}

## 4️⃣ Executar a aplicação
dotnet run
A API ficará disponível em:
https://localhost:7241/swagger


🧱 Tecnologias utilizadas
.NET 8
Minimal API
HttpClient
MemoryCache
Swagger

📌 Objetivo do projeto
Este projeto foi criado com fins educacionais, para praticar:
Consumo de APIs externas
Tratamento e filtragem de dados
Organização de regras de negócio no backend
Cache com MemoryCache
Publicação de API com documentação

🧑‍💻 Autor
Rafael Santana
🔗 https://github.com/RafaelSantana03

