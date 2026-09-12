# Web Backend — HensleyRatings.Api

ASP.NET Core minimal API that serves ratings data from the SQLite database produced by the console pipeline.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A populated `collegefootball.db` (run `src/console/compute-ratings.sh` first)

## Running

```bash
cd src/web-backend
dotnet run --project HensleyRatings.Api --launch-profile http
```

The API starts at **http://localhost:5000**.

## Configuration

The database path defaults to `../../console/BuildFiles/collegefootball.db` (relative to the project file). Override it with:

- `appsettings.json` → `"DbPath"`
- Environment variable `DB_PATH`

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/meta/years` | Available season years |
| GET | `/api/meta/weeks?year=` | Weeks for a given year |
| GET | `/api/meta/divisions` | All divisions |
| GET | `/api/meta/conferences?year=&divisionId=` | Conferences for a year/division |
| GET | `/api/ratings/teams` | Team ratings (filterable by year, week, division, conference) |
| GET | `/api/ratings/conferences` | Conference ratings |
| GET | `/api/schedule` | Game schedule/results |
| GET | `/api/teams` | Team list |

## CORS

Allows requests from `http://localhost:5173` (Vite dev) and `http://localhost:4173` (Vite preview).

## Tests

```bash
dotnet test HensleyRatings.Api.Tests
```
