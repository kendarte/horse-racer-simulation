#!/usr/bin/env python3
"""Validate the public Horse Racer production manifest."""

from __future__ import annotations

import argparse
import csv
import itertools
from collections import Counter, defaultdict
from pathlib import Path


EXPECTED_HEADER = [
    "Pair_ID",
    "Lane1_MeshID",
    "Lane2_MeshID",
    "OUTCOME_CODE",
    "FILENAME",
]
EXPECTED_HORSES = [f"H{index:02d}" for index in range(1, 11)]
EXPECTED_PAIRS = list(itertools.combinations(EXPECTED_HORSES, 2))
EXPECTED_OUTCOMES = {
    "HA_long_win",
    "HA_medium_win",
    "HA_short_win",
    "HB_long_win",
    "HB_medium_win",
    "HB_short_win",
    "tie_photo_finish",
}


def parse_args() -> argparse.Namespace:
    default_manifest = (
        Path(__file__).resolve().parents[1]
        / "docs"
        / "data"
        / "race-manifest.csv"
    )
    parser = argparse.ArgumentParser(
        description="Validate Horse Racer pair, outcome and filename coverage."
    )
    parser.add_argument(
        "manifest",
        nargs="?",
        type=Path,
        default=default_manifest,
        help="Path to race-manifest.csv",
    )
    return parser.parse_args()


def validate(manifest_path: Path) -> list[str]:
    errors: list[str] = []

    with manifest_path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        if reader.fieldnames != EXPECTED_HEADER:
            errors.append(
                f"Unexpected header: {reader.fieldnames!r}; expected {EXPECTED_HEADER!r}"
            )
        rows = list(reader)

    if len(rows) != 315:
        errors.append(f"Expected 315 rows, found {len(rows)}")

    filenames = [row.get("FILENAME", "") for row in rows]
    if len(set(filenames)) != len(filenames):
        duplicates = sorted(
            filename
            for filename, count in Counter(filenames).items()
            if count > 1
        )
        errors.append(f"Duplicate filenames: {duplicates}")

    expected_pair_ids = {f"P{index:02d}" for index in range(1, 46)}
    found_pair_ids = {row.get("Pair_ID", "") for row in rows}
    if found_pair_ids != expected_pair_ids:
        errors.append(
            "Pair ID mismatch: "
            f"missing={sorted(expected_pair_ids - found_pair_ids)}, "
            f"unexpected={sorted(found_pair_ids - expected_pair_ids)}"
        )

    rows_by_pair: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        rows_by_pair[row.get("Pair_ID", "")].append(row)

    for index, expected_pair in enumerate(EXPECTED_PAIRS, start=1):
        pair_id = f"P{index:02d}"
        pair_rows = rows_by_pair.get(pair_id, [])
        found_pairs = {
            (row.get("Lane1_MeshID", ""), row.get("Lane2_MeshID", ""))
            for row in pair_rows
        }
        if found_pairs != {expected_pair}:
            errors.append(
                f"{pair_id} competitor mismatch: found={sorted(found_pairs)}, "
                f"expected={[expected_pair]}"
            )

        found_outcomes = {row.get("OUTCOME_CODE", "") for row in pair_rows}
        if len(pair_rows) != 7 or found_outcomes != EXPECTED_OUTCOMES:
            errors.append(
                f"{pair_id} outcome mismatch: rows={len(pair_rows)}, "
                f"missing={sorted(EXPECTED_OUTCOMES - found_outcomes)}, "
                f"unexpected={sorted(found_outcomes - EXPECTED_OUTCOMES)}"
            )

        for row in pair_rows:
            expected_filename = (
                f"{row.get('Lane1_MeshID', '')}_vs_"
                f"{row.get('Lane2_MeshID', '')}__"
                f"{row.get('OUTCOME_CODE', '')}"
            )
            if row.get("FILENAME", "") != expected_filename:
                errors.append(
                    f"{pair_id} filename mismatch: "
                    f"found={row.get('FILENAME', '')!r}, "
                    f"expected={expected_filename!r}"
                )

    outcome_counts = Counter(row.get("OUTCOME_CODE", "") for row in rows)
    for outcome in EXPECTED_OUTCOMES:
        if outcome_counts[outcome] != 45:
            errors.append(
                f"Outcome {outcome!r} should appear 45 times; "
                f"found {outcome_counts[outcome]}"
            )

    return errors


def main() -> int:
    args = parse_args()
    errors = validate(args.manifest)
    if errors:
        print("Manifest validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print("Manifest validation passed")
    print("315 rows | 45 pairings | 7 outcomes | 315 unique filenames")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
