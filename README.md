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
src/console/BuildFiles/raw-games.txt
        │
        ▼
  DataConverter  ──►  src/console/BuildFiles/converted-games.csv
                                   │
  src/console/BuildFiles/teams.csv │
        │                          ▼
        └──────────►  DatabaseImport  ──►  src/console/BuildFiles/collegefootball.db
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
| `TEAMS_DATA_FILE` | Path to your teams CSV |

### 3. Populate `src/console/BuildFiles/`

The `BuildFiles/` directory is not tracked by git. You need to supply:

| File | Format | Description |
|---|---|---|
| `raw-games.txt` | Fixed-width text | Game results (see format below) |

### 4. Run the pipeline

```bash
./src/console/compute-ratings.sh <week_number>
```

This runs DataConverter → DatabaseImport → RatingSystem in sequence. Output CSVs are written to `src/console/BuildFiles/Output/` and copied to `$RATINGS_OUTPUT_DIR/Week <week_number>/`.

---

## Input File Formats

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

## Project Structure

All source lives under `src/console/`.

| Project | Type | Description |
|---|---|---|
| `DataConverter` | Console | Converts `raw-games.txt` to `converted-games.csv` |
| `DatabaseImport` | Console | Imports teams and games into SQLite; computes connectivity groups |
| `DatabaseLayer` | Library | EF Core data access layer (SQLite); shared by all projects |
| `RatingSystem` | Console | Solves rating linear systems; writes results to CSV and DB |
| `RatingEvaluator` | Console | Benchmarks rating systems against AP poll and average FBS rankings |

---

## License

This project is made available for educational and research purposes.
