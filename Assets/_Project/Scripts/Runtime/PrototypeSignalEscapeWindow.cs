using System;

namespace KimSurvival
{
    [Serializable]
    public sealed class PrototypeSignalEscapeWindow
    {
        public string EscapeId = string.Empty;
        public int Seed;
        public int Day;
        public string ConditionId = string.Empty;
        public bool Allowed;
        public string ResultCode = string.Empty;
    }

    public static class PrototypeSignalEscapeWindowResolver
    {
        public static PrototypeSignalEscapeWindow Resolve(string escapeId, int seed, int day)
        {
            bool radio = string.Equals(escapeId, "escape.radio", StringComparison.Ordinal);
            string family = radio ? "frequency" : "visibility";
            int offset = PrototypeExpeditionRegionCatalog.PositiveModulo(
                PrototypeExpeditionRegionCatalog.StableHash(seed, escapeId, "signal-window"),
                2);
            bool allowed = PrototypeExpeditionRegionCatalog.PositiveModulo(day + offset, 2) == 0;
            return new PrototypeSignalEscapeWindow
            {
                EscapeId = escapeId ?? string.Empty,
                Seed = seed,
                Day = day,
                ConditionId = radio
                    ? allowed ? "radio.frequency.clear" : "radio.frequency.interference"
                    : allowed ? "smoke.visibility.clear-wind" : "smoke.visibility.bad-wind",
                Allowed = allowed,
                ResultCode = allowed
                    ? "escape." + family + ".ready"
                    : "escape." + family + ".wait"
            };
        }

        public static int NextAllowedDay(string escapeId, int seed, int day)
        {
            for (int candidate = Math.Max(1, day); candidate <= Math.Max(1, day) + 2; candidate += 1)
            {
                if (Resolve(escapeId, seed, candidate).Allowed) return candidate;
            }
            return Math.Max(1, day) + 1;
        }

        public static PrototypeContractProbe VerifyDeterministicRetryWindowContract()
        {
            string[] routes = { "escape.smoke", "escape.radio" };
            int[] seeds = { 180018, 220026, 420042 };
            bool passed = true;
            foreach (string route in routes)
            {
                foreach (int seed in seeds)
                {
                    for (int day = 1; day <= 49; day += 1)
                    {
                        PrototypeSignalEscapeWindow first = Resolve(route, seed, day);
                        PrototypeSignalEscapeWindow repeat = Resolve(route, seed, day);
                        int retryDay = NextAllowedDay(route, seed, day);
                        passed &= string.Equals(first.ConditionId, repeat.ConditionId, StringComparison.Ordinal) &&
                                  first.Allowed == repeat.Allowed && retryDay >= day && retryDay <= day + 1 &&
                                  Resolve(route, seed, retryDay).Allowed;
                    }
                }
            }
            return new PrototypeContractProbe(passed,
                "smoke visibility and radio frequency windows are seed/day deterministic with a retry no later than the next day");
        }
    }
}
