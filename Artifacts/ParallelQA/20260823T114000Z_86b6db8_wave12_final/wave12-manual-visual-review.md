# Wave 12 manual 1:1 visual review

- Run ID: `20260823T114000Z_86b6db8_wave12_final`
- Baseline: `86b6db8d5bc628aa7cb9cdb0d3e59539b6633c91`
- Unity: `6000.4.9f1`
- Review resolution: native `1280×800` PNGs at 1:1
- Reviewed UTC: `2026-08-23T11:10:00Z`

## Prompt and direct-slot geometry

- PASS: upper/side/basement approach prompt is visually above the world walking band and does not cover Kim or the approached slot silhouette.
- PASS: machine evidence records `capturePixelRect=x=0,y=0,w=1280,h=800`, prompt `x=420,y=464,w=440,h=48`, walking band `y=183.1..264.9`, and no overlap reason for all three slots.
- EXPECTED_FAIL: the prompt is still a plain `Image.Type.Simple` panel with no compact-a sprite/GUID and combines `[E]` with the TMP action name.
- EXPECTED_FAIL: prompt height is `48px` rather than compact-a's `40..44px` contract, and narration gap is `8px` rather than `>=12px`.

Reviewed screenshots:

- `wave11-slot-upper-ko-approach-1280x800.png`
- `wave11-slot-side-en-approach-1280x800.png`
- `wave11-slot-basement-qps-long-approach-1280x800.png`
- `wave12-ko-near-1280x800.png`
- `wave12-en-near-1280x800.png`

## Localization and overflow

- PASS: ko/en near/far/popup/direct-slot captures exist at `1280×800`; normal ko/en Wave 12 state captures have zero active TMP overflow.
- EXPECTED_FAIL: qps-long popup visibly overlaps/wraps beyond the intended layout; automated count is `4` active overflowing TMP elements in popup state.
- EXPECTED_FAIL: compact-a is not runtime-connected, so the adopted locale-width frame behavior cannot yet be playtested.

Reviewed screenshots:

- `wave12-qps-long-near-1280x800.png`
- `wave12-qps-long-popup-1280x800.png`
- `wave12-qps-long-direct-slot-1280x800.png`

## Fresh Wave 3 regression facts

- P1 PRODUCT_REGRESSION: the static `설비·증축 계획` / `Facilities · Expansion` world label renders at `13.6px` / `12.9px`, below the unchanged `16px` placement-world threshold in all four normal ko/en placement frames (`4/24` failures).
- P1 PRODUCT_REGRESSION: Korean `표류물 ×2` renders at `16.5px`, below the unchanged `18px` exploration-world threshold (`1/10` normal exploration/swimming failure).
- EXPECTED_FAIL: actual qps-long placement reports `6/10` failures from height, bounds, overflow, and overlap.
- The screenshots visually agree with the TSV/report metrics; no threshold was changed or relaxed.

Reviewed screenshots:

- `playmode-ko-placement-valid-1280x800.png`
- `playmode-en-placement-valid-1280x800.png`
- `playmode-ko-day1-swimming-1280x800.png`
- `playmode-ko-day2-exploration-1280x800.png`

## Human-only gates

- Physical gamepad: `UNVERIFIED`; Unity exposed no non-empty joystick name and no human actuation was performed.
- Steam: `NOT_READY`; no App ID, SDK entitlement, depot, Input, Cloud, or Achievements release evidence was introduced.
