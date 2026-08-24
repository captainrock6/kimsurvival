# Wave 17 1280×800 capture review

- Reviewer mode: original 1:1 PNG inspection
- Captures: `wave17-ko-ending-state-1280x800.png`, `wave17-en-ending-state-1280x800.png`, `wave17-qps-long-ending-state-1280x800.png`
- Locale application: PASS (`ko→ko`, `en→en`, `qps-long→qps-long`)
- Existing camp/qps global layout regression: PASS is independently locked by `wave14-qps-global-layout-gate.json` (`10/10`)
- Ending comic presentation: RED_EXPECTED_GAP (`0/3` core panels in every locale)
- Ending TMP geometry claim: not applicable until the ending presentation exists; zero overflow/offscreen/overlap on the unrelated camp surface is not treated as an ending PASS
- Keyboard/synthetic-gamepad meaning: RED_EXPECTED_GAP because the ending semantic state is absent
- Physical gamepad: UNVERIFIED

The captures are truthful absence evidence, not synthetic ending mockups and not a claim that qps-long ending layout has passed.
