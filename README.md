# Hensley Ratings

A college football team rating system built on linear algebra. Computes four distinct ratings for every team, conference, and division using Gauss-Jordan elimination.

**Live ratings:** [hensleyratings.com](https://www.hensleyratings.com)

---

## Background

This project originated as a graduate research effort. The mathematical foundation is documented in the included paper:

> [`Advanced_Computational_Ratings_for_College_Football_Teams.pdf`](Advanced_Computational_Ratings_for_College_Football_Teams.pdf)

The core idea: treat each team as an unknown in a system of linear equations where each game is a constraint. Solving the system via Gauss-Jordan elimination yields a rating for every team simultaneously — accounting for the full web of opponents, not just direct wins and losses.

---

## Rating Methods

| Method | Description |
|---|---|
| **Standard** | Uses raw point differential as the right-hand side of the linear system |
| **Max Point Differential** | Caps per-game margin at 14 points to reduce blowout inflation |
| **Home Field Advantage** | Adds home-field advantage as an additional unknown, solved simultaneously with team ratings |
| **Hensley** | Extends HFA with a nonlinear score function: `√((1 − loserScore/winnerScore) × (winnerScore − loserScore))`, clamped to [1.0, 4.0]. Compresses blowouts while ensuring even close losses contribute positively. |

Each method also computes **schedule strength** (average opponent rating) for every entity.

---

## Pipeline

```
src/console/BuildFiles/raw-teams.txt   src/console/BuildFiles/raw-games.txt
        │                                          │
        ▼                                          ▼
  TeamsParser  ──►  src/console/BuildFiles/teams.csv
                                   │
                                   │         DataConverter  ──►  converted-games.csv
                                   │                                   │
                                   └──────────────────────────────────►▼
                                                               DatabaseImport  ──►  collegefootball.db
                                                                           │
                                                                           ▼
                                                                   RatingSystem
                                                                           │
                                                              ┌────────────┼────────────┐
                                                              ▼            ▼            ▼
                                                        TeamResults  ConferenceResults  DivisionResults
                                                        (CSV + DB)
                                                                           │
                                                                           ▼
                                                                  RatingEvaluator
                                                                           │
                                                                           ▼
                                                                      results.csv
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- bash (for the pipeline script)

---

## Running

### 1. Build the projects

```bash
dotnet build src/console/HensleyRatings.sln
```

### 2. Configure `.env`

Copy `src/console/.env.example` to `src/console/.env` and set:

| Variable | Description |
|---|---|
| `RATINGS_OUTPUT_DIR` | Folder where weekly output is archived |
| `TEAMS_DATA_FILE` | Path to the current season's teams CSV |
| `PREVIOUS_TEAMS_DATA_FILE` | Path to the previous season's teams CSV (used by TeamsParser for match validation) |
| `SEASON_YEAR` | Four-digit year for the current season (used by TeamsParser `--persist` and the ratings pipeline) |
| `DB_PATH` | Path to the SQLite database (used by TeamsParser `--persist` and the ratings pipeline) |

### 3. Populate `src/console/BuildFiles/`

The `BuildFiles/` directory is not tracked by git. You need to supply:

| File | Format | Description |
|---|---|---|
| `raw-teams.txt` | Hierarchical text | Team list export (see below) |
| `raw-games.txt` | Fixed-width text | Game results (see format below) |

### 4. Generate `teams.csv` (start of each season)

```bash
./src/console/parse-teams.sh
```

This parses `raw-teams.txt` against the previous season's teams CSV and writes `BuildFiles/teams.csv`. Teams not found in the previous year are printed to stdout for review.

#### Persist to the database

Pass `--persist` to also write the parsed teams into the SQLite database. The persister finds or creates each Division, Conference, and Team, then writes `ConferenceAffiliation` and `TeamAffiliation` rows for the target year. Running with `--persist` is idempotent — rows that already exist for the year are skipped.

```bash
./src/console/parse-teams.sh --persist
```

Requires `SEASON_YEAR` and `DB_PATH` to be set in `.env` (see the full variable table below).

Output at the end of the run summarises what was added:

```
DB persist (2026): +0 divisions, +3 conferences, +7 teams, +854 affiliations
```

### 5. Run the ratings pipeline

```bash
./src/console/compute-ratings.sh <week_number>
```

This runs DataConverter → DatabaseImport → RatingSystem in sequence. Output CSVs are written to `src/console/BuildFiles/Output/` and copied to `$RATINGS_OUTPUT_DIR/Week <week_number>/`.

---

## Input File Formats

### `raw-teams.txt` (hierarchical text)

A website export listing all college football teams, structured by division → conference → team. Section headers identify divisions (`NCAA Division I - Football Bowl Subdivision`, etc.), lettered entries (`A.`, `B.`, …) identify conferences, and indented lines below them are team names. Conferences with internal divisions (`i) East Division`, etc.) are flattened — all teams are emitted under the parent conference.

`TeamsParser` converts this file into `teams.csv`, resolving known name differences between the export and the previous season's data via `KnownMappings.cs`. Teams not found in the previous year are printed to stdout for review.

### `raw-games.txt` (fixed-width)

Each line encodes one game:

| Columns | Content |
|---|---|
| 0–8 | Date (`dd-Mon-yy`) |
| 10–37 | Away team name (28 chars, space-padded) |
| 38–39 | Away score |
| 41–68 | Home team name (28 chars, space-padded) |
| 69–70 | Home score |
| 72+ | Neutral site location (empty = not neutral) |

### `teams.csv`

```
Division,Conference,Team
FBS,ACC,Clemson
FBS,SEC,Alabama
...
```

---

## Evaluating Rating Quality

The `RatingEvaluator` project benchmarks the four rating systems against external references. To use it, supply these two files in `src/console/BuildFiles/`:

| File | Format | Description |
|---|---|---|
| `top25.csv` | `Rank,Team` | Final AP Top 25 poll |
| `avgfbsranking.csv` | `Rank,Team` | Average FBS computer ranking |

The evaluator outputs a `results.csv` with mistake count, total error (Potemkin's equation), and Pearson correlation against each benchmark for all four rating methods.

---

## Deploying to AWS

The site runs on an EC2 instance provisioned via OpenTofu (`infra/`). The deploy script builds the frontend and backend locally, packages them with the SQLite database, and hot-swaps the running service on the server.

### Prerequisites

- SSH key at `~/.ssh/hensley-ratings` (matches the key pair in `infra/`)
- AWS profile `terraform-deploy` configured (used by OpenTofu; not needed for the deploy script itself)

### One-command deploy

```bash
EC2_HOST=100.62.134.10 bash scripts/deploy.sh
```

`EC2_HOST` defaults to nothing — always pass it explicitly, or export it. The current elastic IP comes from:

```bash
cd infra && tofu output elastic_ip
```

### What it does

1. Builds the React frontend (`npm run build`)
2. Publishes the ASP.NET Core API as a self-contained linux-x64 binary
3. Copies the SQLite database (`src/console/BuildFiles/collegefootball.db`)
4. Packages all three into a tarball and uploads it via `scp`
5. Runs `scripts/swap-deploy.sh` on the server — stops the systemd service, unpacks, restarts, and verifies

### Infrastructure changes

```bash
cd infra
tofu plan   # review
tofu apply  # apply
```

---

## Web Layer

A React frontend and ASP.NET Core API for browsing ratings in the browser.

| Component | Path | README |
|-----------|------|--------|
| API (ASP.NET Core) | `src/web-backend/` | [web-backend/README.md](src/web-backend/README.md) |
| UI (React + Vite) | `src/web-frontend/` | [web-frontend/README.md](src/web-frontend/README.md) |

Quick start (after running the console pipeline at least once):

```bash
# terminal 1 — API (port 5000)
cd src/web-backend/HensleyRatings.Api && dotnet run --urls http://localhost:5000

# terminal 2 — UI (port 5173, proxies /api to port 5000)
cd src/web-frontend && npm install && npm run dev
```

Then open **http://localhost:5173**.

---

## Project Structure

All source lives under `src/console/`.

| Project | Type | Description |
|---|---|---|
| `TeamsParser` | Console | Parses `raw-teams.txt` into `teams.csv`; validates against prior season; `--persist` writes teams and affiliations to the DB |
| `DataConverter` | Console | Converts `raw-games.txt` to `converted-games.csv` |
| `DatabaseImport` | Console | Imports teams and games into SQLite; computes connectivity groups |
| `DatabaseLayer` | Library | EF Core data access layer (SQLite); shared by all projects |
| `RatingSystem` | Console | Solves rating linear systems; writes results to CSV and DB |
| `RatingEvaluator` | Console | Benchmarks rating systems against AP poll and average FBS rankings |

---

## License

This project is made available for educational and research purposes.
