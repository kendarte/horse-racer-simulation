# Horse Racer production specification

This document defines the final public data contract for the deterministic Unity race-production pipeline.

## Scope

| Item | Count |
| --- | ---: |
| Configured horses | 10 |
| Unique unordered pairings | 45 |
| Outcome profiles per pairing | 7 |
| Deterministic race configurations | 315 |

The matrix is derived as:

```text
10 × 9 / 2 = 45 unique pairings
45 pairings × 7 outcomes = 315 race configurations
```

The implementation uses one bounded race ID instead of 315 hand-authored scenes.

## Data contract

Every row in [`race-manifest.csv`](data/race-manifest.csv) contains:

| Field | Responsibility |
| --- | --- |
| `Pair_ID` | Stable identifier for one of the 45 unordered horse pairings. |
| `Lane1_MeshID` | Competitor assigned to lane A. |
| `Lane2_MeshID` | Competitor assigned to lane B. |
| `OUTCOME_CODE` | Deterministic finish profile applied to the pair. |
| `FILENAME` | Reproducible export name for the recorded result. |

## Outcome profiles

| Code | Intended result |
| --- | --- |
| `HA_long_win` | Horse A wins with a clear lead. |
| `HA_medium_win` | Horse A wins after a mid-race advantage. |
| `HA_short_win` | Horse A wins by a narrow margin. |
| `HB_long_win` | Horse B wins with a clear lead. |
| `HB_medium_win` | Horse B wins after a mid-race advantage. |
| `HB_short_win` | Horse B wins by a narrow margin. |
| `tie_photo_finish` | Both competitors reach the photo-finish condition together. |

## Runtime flow

1. Validate a race ID in the supported `1–315` range.
2. Resolve the corresponding pair and outcome.
3. Activate the two configured competitors.
4. Assign lane durations that produce the selected finish profile.
5. Run movement, animation, camera direction and result presentation against the same race state.
6. Export the recording with the deterministic filename and advance the batch.

## Manifest validation

The published manifest was checked for completeness:

- 315 data rows.
- 315 unique filenames.
- 45 unique pair IDs.
- Exactly seven rows for every pair.
- Every outcome code appears exactly 45 times.
- No missing or duplicated pair/outcome configuration.

The same checks can be reproduced locally with [`tools/validate_manifest.py`](../tools/validate_manifest.py) and run automatically through the repository's GitHub Actions workflow.

The human-readable [`pair-matrix.csv`](data/pair-matrix.csv) lists the 45 pairings independently of the outcome expansion.

## Evidence chain

- **Working result:** the [portfolio case study and race video](https://kendarte.github.io/projects/horse-racer/) show the complete pipeline in motion.
- **Coverage:** the production manifest shows every pairing/outcome configuration and deterministic export name.
- **Implementation:** the repository's C# samples show orchestration, movement, cameras, presentation, track generation and automated recording.

## Repository boundary

This repository publishes focused Unity C# samples and technical documentation. Commercial assets, project scenes, third-party packages and the 315 rendered MP4 files are intentionally excluded.
