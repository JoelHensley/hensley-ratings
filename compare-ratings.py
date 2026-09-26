#!/usr/bin/env python3
"""
compare-ratings.py  --  Compare expected vs actual ratings output.

Usage:
  python compare-ratings.py <expected_dir> <actual_dir> [--week N] [--tolerance 0.005] [--all-groups]

  expected_dir   Path to the "expected" week folder (e.g. ~/Dropbox/.../Week 4)
  actual_dir     Path to the "actual" week folder   (e.g. BuildFiles/Output/Week 4)
  --week N       Label for the week in output (optional)
  --tolerance    Max allowed difference before flagging (default 0.005)
  --all-groups   Compare all TeamResults_Raw_*.csv groups (default: largest group only)

The script reads TeamResults_Raw_1.csv (largest group) by default and compares
HensleyRating (col 4) and ScheduleStrength (col 5) for each team.
"""

import argparse
import csv
import os
import sys
from pathlib import Path


def load_raw_results(directory: str, all_groups: bool = False) -> dict[str, dict]:
    """Load TeamResults_Raw_*.csv files from a directory.

    By default loads only TeamResults_Raw_1.csv (the largest group).
    Pass all_groups=True to load all group files.

    Returns a dict keyed by team name with fields:
      wins, losses, hensley_rating, schedule_strength
    """
    path = Path(directory).expanduser()
    teams = {}

    if all_groups:
        raw_files = sorted(path.glob("TeamResults_Raw_*.csv"))
    else:
        f = path / "TeamResults_Raw_1.csv"
        raw_files = [f] if f.exists() else []

    if not raw_files:
        print(f"ERROR: No TeamResults_Raw_*.csv files found in {path}", file=sys.stderr)
        sys.exit(1)

    for csv_file in raw_files:
        with open(csv_file, newline="") as f:
            for row in csv.reader(f):
                if len(row) < 5:
                    continue
                name = row[0].strip()
                try:
                    wins = int(row[1])
                    losses = int(row[2])
                    hr = float(row[3])
                    ss = float(row[4])
                except ValueError:
                    continue
                teams[name] = {
                    "wins": wins,
                    "losses": losses,
                    "hensley_rating": hr,
                    "schedule_strength": ss,
                }

    return teams


def compare(expected: dict, actual: dict, tolerance: float) -> list[dict]:
    """Return per-team comparison rows."""
    all_teams = sorted(set(expected) | set(actual))
    rows = []

    for team in all_teams:
        if team not in expected:
            rows.append({"team": team, "status": "MISSING_IN_EXPECTED"})
            continue
        if team not in actual:
            rows.append({"team": team, "status": "MISSING_IN_ACTUAL"})
            continue

        exp = expected[team]
        act = actual[team]
        hr_diff = act["hensley_rating"] - exp["hensley_rating"]
        ss_diff = act["schedule_strength"] - exp["schedule_strength"]
        hr_match = abs(hr_diff) <= tolerance
        ss_match = abs(ss_diff) <= tolerance

        rows.append({
            "team": team,
            "status": "OK" if (hr_match and ss_match) else "DIFF",
            "exp_hr": exp["hensley_rating"],
            "act_hr": act["hensley_rating"],
            "hr_diff": hr_diff,
            "hr_match": hr_match,
            "exp_ss": exp["schedule_strength"],
            "act_ss": act["schedule_strength"],
            "ss_diff": ss_diff,
            "ss_match": ss_match,
        })

    return rows


def print_report(rows: list[dict], week_label: str, tolerance: float):
    diffs = [r for r in rows if r["status"] == "DIFF"]
    missing = [r for r in rows if r["status"] in ("MISSING_IN_EXPECTED", "MISSING_IN_ACTUAL")]
    ok = [r for r in rows if r["status"] == "OK"]

    total = len([r for r in rows if r["status"] in ("OK", "DIFF")])
    match_pct = 100 * len(ok) / total if total else 0.0

    header = f"Ratings Comparison — {week_label}  (tolerance ±{tolerance})"
    print(header)
    print("=" * len(header))
    print(f"  Teams compared : {total}")
    print(f"  Within tolerance: {len(ok)}  ({match_pct:.1f}%)")
    print(f"  Differences    : {len(diffs)}")
    if missing:
        print(f"  Missing entries: {len(missing)}")
    print()

    if diffs:
        hr_diffs = [abs(r["hr_diff"]) for r in diffs]
        ss_diffs = [abs(r["ss_diff"]) for r in diffs]

        print(f"{'Team':<35} {'Exp HR':>8} {'Act HR':>8} {'ΔHR':>8}  {'Exp SS':>8} {'Act SS':>8} {'ΔSS':>8}")
        print("-" * 95)
        for r in sorted(diffs, key=lambda x: -abs(x["hr_diff"])):
            hr_flag = "!" if not r["hr_match"] else " "
            ss_flag = "!" if not r["ss_match"] else " "
            print(
                f"{r['team']:<35} {r['exp_hr']:>8.3f} {r['act_hr']:>8.3f} {r['hr_diff']:>+8.3f}{hr_flag}"
                f" {r['exp_ss']:>8.3f} {r['act_ss']:>8.3f} {r['ss_diff']:>+8.3f}{ss_flag}"
            )
        print()
        print(f"  Max |ΔHR|: {max(hr_diffs):.4f}   Avg |ΔHR|: {sum(hr_diffs)/len(hr_diffs):.4f}")
        print(f"  Max |ΔSS|: {max(ss_diffs):.4f}   Avg |ΔSS|: {sum(ss_diffs)/len(ss_diffs):.4f}")
        print()

    if missing:
        print("Missing teams:")
        for r in missing:
            print(f"  {r['team']} — {r['status']}")
        print()

    if not diffs and not missing:
        print("All teams match within tolerance.")


def main():
    parser = argparse.ArgumentParser(description="Compare expected vs actual ratings CSVs.")
    parser.add_argument("expected_dir", help="Directory containing expected TeamResults_Raw_*.csv")
    parser.add_argument("actual_dir", help="Directory containing actual TeamResults_Raw_*.csv")
    parser.add_argument("--week", default="", help="Week label for the report header")
    parser.add_argument("--tolerance", type=float, default=0.005,
                        help="Max allowed difference before flagging a value (default 0.005)")
    parser.add_argument("--all-groups", action="store_true",
                        help="Compare all group files instead of just the largest (group 1)")
    args = parser.parse_args()

    week_label = args.week or f"{args.expected_dir} vs {args.actual_dir}"

    expected = load_raw_results(args.expected_dir, args.all_groups)
    actual = load_raw_results(args.actual_dir, args.all_groups)

    rows = compare(expected, actual, args.tolerance)
    print_report(rows, week_label, args.tolerance)

    diffs = [r for r in rows if r["status"] in ("DIFF", "MISSING_IN_ACTUAL", "MISSING_IN_EXPECTED")]
    sys.exit(1 if diffs else 0)


if __name__ == "__main__":
    main()
