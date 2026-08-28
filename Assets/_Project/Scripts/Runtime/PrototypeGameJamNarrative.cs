using System;
using System.Collections.Generic;

namespace KimSurvival
{
    public readonly struct PrototypeBilingualCopy
    {
        public PrototypeBilingualCopy(string ko, string en)
        {
            Korean = ko ?? string.Empty;
            English = en ?? string.Empty;
        }

        public string Korean { get; }
        public string English { get; }

        public string Resolve(string localeCode)
        {
            return string.Equals(localeCode, PrototypeLocalization.KoreanLocaleCode, StringComparison.Ordinal)
                ? Korean
                : English;
        }
    }

    public readonly struct PrototypeOpeningBeat
    {
        public PrototypeOpeningBeat(string stableId, PrototypeBilingualCopy chapter, PrototypeBilingualCopy body)
        {
            StableId = stableId ?? string.Empty;
            Chapter = chapter;
            Body = body;
        }

        public string StableId { get; }
        public PrototypeBilingualCopy Chapter { get; }
        public PrototypeBilingualCopy Body { get; }
    }

    /// <summary>
    /// O9/O10 submission copy. Stable IDs are deliberately independent from display copy so
    /// additional locales can replace the bilingual fallback without changing save or QA data.
    /// </summary>
    public static class PrototypeGameJamNarrative
    {
        public const string StoryCauseId = "story.stranding.storm-drift";
        public const string ArtDirectionId = "art.ink-kim.simplified-world";

        public static readonly PrototypeOpeningBeat[] Opening =
        {
            new PrototypeOpeningBeat(
                "opening.storm.departure",
                new PrototypeBilingualCopy("01 · 출항", "01 · DEPARTURE"),
                new PrototypeBilingualCopy(
                    "평범한 귀갓길. 김씨가 탄 작은 배는 평범하지 않은 먹구름을 만났다.",
                    "An ordinary trip home. Mr. Kim's little boat met a very unordinary cloud.")),
            new PrototypeOpeningBeat(
                "opening.storm.impact",
                new PrototypeBilingualCopy("02 · 폭풍", "02 · THE STORM"),
                new PrototypeBilingualCopy(
                    "파도는 배를 접고, 짐을 풀고, 김씨의 계획도 깔끔하게 접어 버렸다.",
                    "The waves folded the boat, unpacked the cargo, and neatly folded Kim's plans.")),
            new PrototypeOpeningBeat(
                "opening.storm.drift",
                new PrototypeBilingualCopy("03 · 표류", "03 · ADRIFT"),
                new PrototypeBilingualCopy(
                    "구명조끼 하나, 젖은 배낭 하나. 김씨는 밤새 어느 방향인지도 모르고 떠밀렸다.",
                    "One life vest, one soaked pack. Kim drifted all night without knowing where.")),
            new PrototypeOpeningBeat(
                "opening.storm.shore",
                new PrototypeBilingualCopy("04 · 이름 모를 해변", "04 · AN UNKNOWN SHORE"),
                new PrototypeBilingualCopy(
                    "아침이 되자 발밑에는 모래가 있었고, 머리 위에는 영수증보다 긴 야자잎이 있었다.",
                    "Morning brought sand underfoot and palm leaves longer than any receipt.")),
            new PrototypeOpeningBeat(
                "opening.storm.resolve",
                new PrototypeBilingualCopy("05 · 김씨의 결론", "05 · KIM'S CONCLUSION"),
                new PrototypeBilingualCopy(
                    "살고, 뒤지고, 만들고, 신호를 보낸다. 구조가 늦으면 섬을 먼저 고치면 된다.",
                    "Survive, search, build, signal. If rescue is late, fix the island first."))
        };

        public static readonly string[] CoreEndingIds =
        {
            "ending.escape.raft.open-water",
            "ending.escape.smoke.seen-from-afar",
            "ending.escape.radio.clear-signal",
            "ending.gamejam.stay.natural-kim",
            "ending.gamejam.stay.island-engineer"
        };

        public static readonly string[] RequiredItemIds =
        {
            "resource.wood",
            "resource.stone",
            "resource.fiber",
            "resource.fabric",
            "resource.food",
            "resource.medicine",
            "resource.salvage",
            "resource.metal",
            "resource.wire",
            "resource.electronics",
            "resource.fuel",
            "resource.chemicals",
            "part.raft.sailcloth",
            "part.smoke.flint",
            "part.radio.transceiver",
            "part.radio.circuit-board",
            "part.radio.transistor",
            "tool.stone-axe",
            "tool.rope"
        };

        public static readonly string[] RegionIds =
        {
            "region.coast.beach",
            "region.forest.grove",
            "region.sea.shallows",
            "region.ridge.highland",
            "region.cave.island",
            "region.cove.wreck",
            "region.ruins.relay"
        };

        public static PrototypeBilingualCopy Objective(string stableId)
        {
            if (Objectives.TryGetValue(stableId ?? string.Empty, out PrototypeBilingualCopy value))
            {
                return value;
            }

            return Objectives["objective.camp.map"];
        }

        private static readonly Dictionary<string, PrototypeBilingualCopy> Objectives =
            new Dictionary<string, PrototypeBilingualCopy>(StringComparer.Ordinal)
            {
                {
                    "objective.camp.map",
                    new PrototypeBilingualCopy(
                        "첫 목표 · 오른쪽의 수집 지도 표지판으로 이동해 [E]로 해변을 선택하세요.",
                        "FIRST GOAL · Move to the expedition map on the right and press [E] to choose the beach.")
                },
                {
                    "objective.map.choose",
                    new PrototypeBilingualCopy(
                        "해변은 장비 없이 시작할 수 있습니다. 남은 자원과 위험을 확인한 뒤 출발하세요.",
                        "The beach needs no equipment. Check remaining resources and risk, then depart.")
                },
                {
                    "objective.search.inspect",
                    new PrototypeBilingualCopy(
                        "◇ 표시가 있는 풀숲·표류물·바위틈에 다가가 [E]로 뒤지세요.",
                        "Approach brush, drift piles, or crevices marked ◇ and press [E] to search.")
                },
                {
                    "objective.search.choose",
                    new PrototypeBilingualCopy(
                        "발견물과 왼쪽 아래 가방을 비교해 가져갈 물건을 고르세요.",
                        "Compare the finds with your bag at bottom-left and choose what to carry.")
                },
                {
                    "objective.search.return",
                    new PrototypeBilingualCopy(
                        "필요한 만큼 챙겼다면 해변 왼쪽 귀환 깃발에서 돌아가세요.",
                        "When you have enough, return at the flag on the far left.")
                },
                {
                    "objective.camp.invest",
                    new PrototypeBilingualCopy(
                        "귀환 완료 · 설비의 흰 원 표식에 다가가 제작·연구·건설에 투자하세요.",
                        "RETURN COMPLETE · Approach a facility's white marker to craft, research, or build.")
                },
                {
                    "objective.escape",
                    new PrototypeBilingualCopy(
                        "생존 루프 가동 중 · 지역을 열고 재료를 모아 뗏목·연기·무전 중 하나를 완성하세요.",
                        "SURVIVAL LOOP ACTIVE · Unlock regions and complete the raft, smoke, or radio route.")
                },
                {
                    "objective.result",
                    new PrototypeBilingualCopy(
                        "결말은 탈출 방법과 김씨가 섬에서 반복한 행동을 함께 기록합니다.",
                        "Your ending records both the escape route and the habits Kim formed on the island.")
                }
            };
    }
}
