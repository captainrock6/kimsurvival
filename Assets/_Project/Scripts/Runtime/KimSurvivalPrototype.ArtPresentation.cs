using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace KimSurvival
{
    public sealed partial class KimSurvivalPrototype
    {
        private readonly struct ExpeditionRegionPalette
        {
            public ExpeditionRegionPalette(Color sky, Color horizon, Color water, Color sand, Color land, Color ink, Color accent)
            {
                Sky = sky;
                Horizon = horizon;
                Water = water;
                Sand = sand;
                Land = land;
                Ink = ink;
                Accent = accent;
            }

            public Color Sky { get; }
            public Color Horizon { get; }
            public Color Water { get; }
            public Color Sand { get; }
            public Color Land { get; }
            public Color Ink { get; }
            public Color Accent { get; }
        }

        private void CreateSearchRegionPresentation(PrototypeExpeditionRegionId region)
        {
            PrototypeExpeditionRegionProfile profile = PrototypeExpeditionRegionCatalog.Get(region);
            ExpeditionRegionPalette palette = RegionPalette(region);
            worldCamera.backgroundColor = palette.Sky;

            GameObject artRoot = new GameObject("Expedition Region Art · " + profile.StableId + " · " + AssetSearchBackground);
            artRoot.transform.SetParent(worldRoot, false);
            CreateRect(artRoot.transform, "채색 하늘", new Vector2(8f, 1.45f), new Vector2(60f, 8.3f), palette.Sky, -20);
            CreateRect(artRoot.transform, "수평선 빛 띠", new Vector2(8f, -0.12f), new Vector2(60f, 1.2f), palette.Horizon, -19);
            CreateRect(artRoot.transform, "연안 수면", new Vector2(-10f, -1.15f), new Vector2(12f, 3f), palette.Water, -15);
            CreateRect(artRoot.transform, "수면 반사 A", new Vector2(-10.2f, -0.42f), new Vector2(11.4f, 0.12f), ColorWithAlpha(palette.Horizon, 0.72f), -14);
            CreateRect(artRoot.transform, "수면 반사 B", new Vector2(-9.4f, -1.24f), new Vector2(9.6f, 0.08f), new Color(0.82f, 0.96f, 0.98f, 0.56f), -14);
            CreateRect(artRoot.transform, "연안 모래 바닥", new Vector2(-10f, -3.55f), new Vector2(12f, 1.3f), palette.Sand, -12);
            CreateRect(artRoot.transform, "탐사 지면", new Vector2(17f, -3.25f), new Vector2(44f, 1.9f), palette.Land, -10);
            CreateRect(artRoot.transform, "포말 해안선", new Vector2(PrototypePlayerTraversal.CoastlineX, -2.35f), new Vector2(0.30f, 1.25f), new Color(0.90f, 0.98f, 0.94f, 0.92f), -4);

            switch (region)
            {
                case PrototypeExpeditionRegionId.Beach:
                    CreateSun(new Vector2(-1f, 3.6f));
                    CreateDune(artRoot.transform, new Vector2(2.4f, -2.55f), 2.2f, palette.Accent);
                    CreateDune(artRoot.transform, new Vector2(6.8f, -2.48f), 2.9f, palette.Accent);
                    CreatePalm(new Vector2(3.4f, -2.28f), 0.92f);
                    CreatePalm(new Vector2(8.1f, -2.28f), 0.72f);
                    CreateRect(artRoot.transform, "해변 표류목", new Vector2(-0.4f, -2.47f), new Vector2(2.4f, 0.25f), palette.Ink, -2).transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
                    break;
                case PrototypeExpeditionRegionId.Shallows:
                    CreateSun(new Vector2(-2f, 3.3f));
                    CreateRect(artRoot.transform, "얕은물 청록 반사", new Vector2(-2.5f, -1.55f), new Vector2(18f, 1.45f), ColorWithAlpha(palette.Water, 0.88f), -11);
                    for (int index = 0; index < 6; index += 1)
                    {
                        float x = -7.5f + index * 2.7f;
                        CreateCoral(artRoot.transform, new Vector2(x, -2.25f + (index % 2) * 0.12f), 0.65f + (index % 3) * 0.12f, palette.Ink, palette.Accent);
                    }
                    CreateRect(artRoot.transform, "모래톱 능선", new Vector2(5.8f, -2.72f), new Vector2(7.5f, 0.38f), palette.Accent, -3).transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
                    break;
                case PrototypeExpeditionRegionId.Forest:
                    CreateForestCanopy(artRoot.transform, palette);
                    CreateRect(artRoot.transform, "숲 안개", new Vector2(3.8f, -0.35f), new Vector2(19f, 0.42f), ColorWithAlpha(palette.Horizon, 0.42f), -3);
                    break;
                case PrototypeExpeditionRegionId.RidgeHighland:
                    CreateSun(new Vector2(-2.4f, 3.75f));
                    CreateRidge(artRoot.transform, new Vector2(1.5f, -1.25f), 4.6f, 3.7f, palette.Ink, -8);
                    CreateRidge(artRoot.transform, new Vector2(7.4f, -1.15f), 5.8f, 4.8f, palette.Accent, -7);
                    CreateRidge(artRoot.transform, new Vector2(13.3f, -1.45f), 4.2f, 3.4f, palette.Ink, -6);
                    for (int index = 0; index < 4; index += 1)
                    {
                        CreateRect(artRoot.transform, "절벽 층리 " + index, new Vector2(3.1f + index * 3.2f, -2.35f), new Vector2(2.3f, 0.16f), ColorWithAlpha(palette.Horizon, 0.72f), -2);
                    }
                    break;
                case PrototypeExpeditionRegionId.CaveIsland:
                    CreateCaveMouth(artRoot.transform, palette);
                    break;
                case PrototypeExpeditionRegionId.CoveWreck:
                    CreateSun(new Vector2(-2.2f, 3.5f));
                    CreateWreckLandmark(artRoot.transform, new Vector2(4.2f, -1.75f), palette);
                    CreateRect(artRoot.transform, "난파만 물안개", new Vector2(4.5f, -0.65f), new Vector2(18f, 0.34f), ColorWithAlpha(palette.Horizon, 0.52f), -3);
                    break;
                case PrototypeExpeditionRegionId.RuinsRelay:
                    CreateRelayRuins(artRoot.transform, palette);
                    break;
            }
        }

        private static ExpeditionRegionPalette RegionPalette(PrototypeExpeditionRegionId region)
        {
            switch (region)
            {
                case PrototypeExpeditionRegionId.Forest:
                    return new ExpeditionRegionPalette(new Color(0.44f, 0.72f, 0.66f), new Color(0.76f, 0.86f, 0.61f), new Color(0.10f, 0.47f, 0.54f), new Color(0.46f, 0.42f, 0.22f), new Color(0.30f, 0.43f, 0.18f), new Color(0.055f, 0.20f, 0.12f), new Color(0.62f, 0.70f, 0.25f));
                case PrototypeExpeditionRegionId.Shallows:
                    return new ExpeditionRegionPalette(new Color(0.47f, 0.84f, 0.92f), new Color(0.82f, 0.96f, 0.90f), new Color(0.04f, 0.68f, 0.76f), new Color(0.77f, 0.72f, 0.45f), new Color(0.77f, 0.70f, 0.38f), new Color(0.05f, 0.39f, 0.48f), new Color(0.98f, 0.55f, 0.28f));
                case PrototypeExpeditionRegionId.RidgeHighland:
                    return new ExpeditionRegionPalette(new Color(0.55f, 0.68f, 0.78f), new Color(0.84f, 0.76f, 0.62f), new Color(0.15f, 0.43f, 0.58f), new Color(0.48f, 0.43f, 0.36f), new Color(0.42f, 0.38f, 0.31f), new Color(0.20f, 0.25f, 0.29f), new Color(0.69f, 0.52f, 0.34f));
                case PrototypeExpeditionRegionId.CaveIsland:
                    return new ExpeditionRegionPalette(new Color(0.08f, 0.18f, 0.24f), new Color(0.16f, 0.34f, 0.38f), new Color(0.07f, 0.34f, 0.42f), new Color(0.25f, 0.28f, 0.25f), new Color(0.18f, 0.23f, 0.22f), new Color(0.025f, 0.07f, 0.08f), new Color(0.27f, 0.70f, 0.62f));
                case PrototypeExpeditionRegionId.CoveWreck:
                    return new ExpeditionRegionPalette(new Color(0.42f, 0.68f, 0.74f), new Color(0.75f, 0.78f, 0.66f), new Color(0.08f, 0.43f, 0.54f), new Color(0.47f, 0.43f, 0.31f), new Color(0.50f, 0.43f, 0.27f), new Color(0.18f, 0.20f, 0.19f), new Color(0.76f, 0.31f, 0.18f));
                case PrototypeExpeditionRegionId.RuinsRelay:
                    return new ExpeditionRegionPalette(new Color(0.48f, 0.62f, 0.63f), new Color(0.78f, 0.71f, 0.56f), new Color(0.10f, 0.38f, 0.48f), new Color(0.43f, 0.42f, 0.35f), new Color(0.40f, 0.40f, 0.34f), new Color(0.17f, 0.24f, 0.25f), new Color(0.92f, 0.44f, 0.17f));
                default:
                    return new ExpeditionRegionPalette(new Color(0.35f, 0.74f, 0.90f), new Color(0.78f, 0.91f, 0.83f), new Color(0.12f, 0.55f, 0.76f), new Color(0.66f, 0.57f, 0.34f), new Color(0.87f, 0.68f, 0.34f), new Color(0.34f, 0.20f, 0.09f), new Color(0.96f, 0.75f, 0.35f));
            }
        }

        private void CreateDune(Transform parent, Vector2 position, float scale, Color color)
        {
            CreateRect(parent, "사구 왼쪽 면", position + new Vector2(-0.7f * scale, 0f), new Vector2(1.6f * scale, 0.42f * scale), color, -3).transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            CreateRect(parent, "사구 오른쪽 면", position + new Vector2(0.65f * scale, 0.02f), new Vector2(1.5f * scale, 0.36f * scale), ColorWithAlpha(color, 0.78f), -3).transform.localRotation = Quaternion.Euler(0f, 0f, -9f);
        }

        private void CreateCoral(Transform parent, Vector2 position, float scale, Color ink, Color accent)
        {
            CreateRect(parent, "산호 중심", position, new Vector2(0.16f * scale, 1.0f * scale), ink, -2);
            CreateRect(parent, "산호 왼가지", position + new Vector2(-0.24f * scale, 0.18f * scale), new Vector2(0.13f * scale, 0.62f * scale), accent, -2).transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            CreateRect(parent, "산호 오른가지", position + new Vector2(0.24f * scale, 0.28f * scale), new Vector2(0.13f * scale, 0.70f * scale), accent, -2).transform.localRotation = Quaternion.Euler(0f, 0f, 34f);
        }

        private void CreateForestCanopy(Transform parent, ExpeditionRegionPalette palette)
        {
            for (int index = 0; index < 6; index += 1)
            {
                float x = -0.5f + index * 3.1f;
                CreateRect(parent, "원시림 줄기 " + index, new Vector2(x, -0.4f), new Vector2(0.52f, 5.9f), palette.Ink, -7).transform.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? -4f : 5f);
                for (int leaf = 0; leaf < 3; leaf += 1)
                {
                    GameObject crown = CreateRect(parent, "원시림 수관 " + index + "-" + leaf, new Vector2(x + (leaf - 1) * 0.78f, 2.45f + (leaf % 2) * 0.28f), new Vector2(2.25f, 0.72f), leaf == 1 ? palette.Accent : palette.Land, -6);
                    crown.transform.localRotation = Quaternion.Euler(0f, 0f, (leaf - 1) * 18f);
                }
            }
        }

        private void CreateRidge(Transform parent, Vector2 basePosition, float width, float height, Color color, int order)
        {
            GameObject left = CreateRect(parent, "절벽 능선 왼면", basePosition + new Vector2(-width * 0.23f, height * 0.32f), new Vector2(width * 0.72f, height * 0.34f), color, order);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 43f);
            GameObject right = CreateRect(parent, "절벽 능선 오른면", basePosition + new Vector2(width * 0.23f, height * 0.32f), new Vector2(width * 0.72f, height * 0.34f), ColorWithAlpha(color, 0.82f), order);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -43f);
        }

        private void CreateCaveMouth(Transform parent, ExpeditionRegionPalette palette)
        {
            CreateRect(parent, "동굴 천장", new Vector2(4f, 3.75f), new Vector2(36f, 2.9f), palette.Ink, -5);
            CreateRect(parent, "동굴 왼벽", new Vector2(-1.1f, 0.3f), new Vector2(2.6f, 6.8f), palette.Ink, -4).transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            CreateRect(parent, "동굴 오른벽", new Vector2(14.3f, 0.2f), new Vector2(3.2f, 7.2f), palette.Ink, -4).transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            for (int index = 0; index < 7; index += 1)
            {
                float x = 0.6f + index * 2.05f;
                float length = 0.65f + (index % 3) * 0.42f;
                CreateRect(parent, "종유석 " + index, new Vector2(x, 2.25f - length * 0.5f), new Vector2(0.30f, length), index % 2 == 0 ? palette.Ink : palette.Accent, -3).transform.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? -8f : 7f);
            }
            CreateRect(parent, "동굴 출구광", new Vector2(-4.2f, 0.55f), new Vector2(4.8f, 5.3f), ColorWithAlpha(palette.Accent, 0.32f), -9);
        }

        private void CreateWreckLandmark(Transform parent, Vector2 position, ExpeditionRegionPalette palette)
        {
            GameObject hull = CreateRect(parent, "난파선 선체", position, new Vector2(7.4f, 1.12f), palette.Ink, -4);
            hull.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            CreateRect(parent, "난파선 깨진 선수", position + new Vector2(3.25f, 0.48f), new Vector2(1.0f, 1.5f), palette.Accent, -3).transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
            CreateRect(parent, "난파선 돛대", position + new Vector2(-0.45f, 2.25f), new Vector2(0.26f, 5.1f), palette.Ink, -3).transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            CreateRect(parent, "난파선 찢긴 돛", position + new Vector2(0.75f, 2.35f), new Vector2(2.1f, 1.65f), ColorWithAlpha(palette.Accent, 0.86f), -4).transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
            for (int index = 0; index < 4; index += 1)
            {
                CreateRect(parent, "난파선 늑골 " + index, position + new Vector2(-2.1f + index * 1.15f, 0.18f), new Vector2(0.16f, 1.65f), palette.Accent, -3).transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            }
        }

        private void CreateRelayRuins(Transform parent, ExpeditionRegionPalette palette)
        {
            for (int index = 0; index < 4; index += 1)
            {
                float x = 0.6f + index * 3.6f;
                CreateRect(parent, "폐시설 기둥 " + index, new Vector2(x, -0.15f), new Vector2(0.58f, 5.8f - index * 0.45f), palette.Ink, -5);
                CreateRect(parent, "폐시설 보 " + index, new Vector2(x + 1.65f, 1.6f - index * 0.18f), new Vector2(3.5f, 0.34f), palette.Accent, -5).transform.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? -4f : 5f);
            }
            CreateRect(parent, "중계탑 마스트", new Vector2(6.1f, 2.2f), new Vector2(0.25f, 4.5f), palette.Ink, -2);
            GameObject dish = CreateRect(parent, "중계탑 접시", new Vector2(6.85f, 3.5f), new Vector2(2.0f, 0.55f), palette.Accent, -1);
            dish.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            for (int index = 0; index < 3; index += 1)
            {
                CreateRect(parent, "중계 신호 " + index, new Vector2(8.15f + index * 0.7f, 3.75f + index * 0.35f), new Vector2(0.75f + index * 0.35f, 0.10f), ColorWithAlpha(palette.Accent, 0.82f - index * 0.16f), -1).transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
            }
        }

        private void CreateSearchNodeArt(
            Transform parent,
            PrototypeSearchNodeDefinition definition,
            PrototypeSearchNodeSnapshot snapshot,
            bool water)
        {
            float alpha = snapshot.State == PrototypeSearchNodeState.Depleted ? 0.34f : 0.98f;
            Color green = new Color(0.10f, 0.43f, 0.19f, alpha);
            Color greenLight = new Color(0.42f, 0.68f, 0.25f, alpha);
            Color stone = new Color(0.30f, 0.36f, 0.38f, alpha);
            Color stoneLight = new Color(0.58f, 0.62f, 0.59f, alpha);
            Color timber = new Color(0.31f, 0.17f, 0.075f, alpha);
            Color timberLight = new Color(0.68f, 0.42f, 0.16f, alpha);
            Color metal = new Color(0.18f, 0.34f, 0.38f, alpha);
            Color accent = new Color(0.94f, 0.43f, 0.14f, alpha);

            switch (definition.Kind)
            {
                case PrototypeSearchNodeKind.GrassPatch:
                    for (int index = 0; index < 5; index += 1)
                    {
                        float x = (index - 2) * 0.23f;
                        GameObject blade = CreateRect(parent, "수색 풀잎 " + index, new Vector2(x, 0.18f + Math.Abs(index - 2) * -0.025f), new Vector2(0.18f, 1.05f - Math.Abs(index - 2) * 0.10f), index % 2 == 0 ? green : greenLight, 4);
                        blade.transform.localRotation = Quaternion.Euler(0f, 0f, (index - 2) * 14f);
                    }
                    break;
                case PrototypeSearchNodeKind.RockCrevice:
                    CreateRect(parent, "수색 암반 왼면", new Vector2(-0.38f, 0.08f), new Vector2(1.02f, 0.92f), stone, 4).transform.localRotation = Quaternion.Euler(0f, 0f, 19f);
                    CreateRect(parent, "수색 암반 오른면", new Vector2(0.40f, 0.10f), new Vector2(0.90f, 1.10f), stoneLight, 4).transform.localRotation = Quaternion.Euler(0f, 0f, -17f);
                    CreateRect(parent, "수색 바위틈", new Vector2(0.04f, 0.05f), new Vector2(0.17f, 0.82f), new Color(0.025f, 0.045f, 0.05f, alpha), 5).transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
                    break;
                case PrototypeSearchNodeKind.DriftPile:
                    for (int index = 0; index < 4; index += 1)
                    {
                        GameObject log = CreateRect(parent, "수색 표류목 " + index, new Vector2((index % 2) * 0.16f, 0.08f + index * 0.20f), new Vector2(1.70f - index * 0.12f, 0.25f), index % 2 == 0 ? timber : timberLight, 4);
                        log.transform.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? 11f : -10f);
                    }
                    break;
                case PrototypeSearchNodeKind.TreeHollow:
                    CreateRect(parent, "수색 고목 몸통", new Vector2(0f, 0.42f), new Vector2(0.92f, 1.55f), timber, 4);
                    CreateRect(parent, "수색 고목 절단면", new Vector2(0f, 1.18f), new Vector2(1.10f, 0.24f), timberLight, 5).transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
                    CreateRect(parent, "수색 고목 구멍", new Vector2(0f, 0.38f), new Vector2(0.40f, 0.58f), new Color(0.025f, 0.016f, 0.01f, alpha), 5);
                    CreateRect(parent, "수색 고목 가지", new Vector2(0.53f, 0.82f), new Vector2(0.85f, 0.16f), timber, 4).transform.localRotation = Quaternion.Euler(0f, 0f, 31f);
                    break;
                case PrototypeSearchNodeKind.WreckLocker:
                    CreateRect(parent, "수색 난파함 몸체", new Vector2(0f, 0.24f), new Vector2(1.52f, 0.92f), timber, 4);
                    CreateRect(parent, "수색 난파함 덮개", new Vector2(0f, 0.73f), new Vector2(1.65f, 0.20f), timberLight, 5).transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
                    CreateRect(parent, "수색 난파함 금속띠 왼쪽", new Vector2(-0.48f, 0.24f), new Vector2(0.13f, 0.92f), metal, 5);
                    CreateRect(parent, "수색 난파함 금속띠 오른쪽", new Vector2(0.48f, 0.24f), new Vector2(0.13f, 0.92f), metal, 5);
                    CreateRect(parent, "수색 난파함 잠금쇠", new Vector2(0f, 0.28f), new Vector2(0.28f, 0.24f), accent, 6);
                    break;
                case PrototypeSearchNodeKind.FacilityCabinet:
                    CreateRect(parent, "수색 폐시설 캐비닛", new Vector2(0f, 0.55f), new Vector2(1.22f, 1.80f), metal, 4);
                    CreateRect(parent, "수색 캐비닛 문 분할", new Vector2(0f, 0.55f), new Vector2(0.08f, 1.64f), stoneLight, 5);
                    CreateRect(parent, "수색 캐비닛 경고띠", new Vector2(0f, 1.18f), new Vector2(1.02f, 0.16f), accent, 5).transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
                    CreateRect(parent, "수색 캐비닛 손잡이", new Vector2(0.34f, 0.55f), new Vector2(0.09f, 0.32f), new Color(0.88f, 0.92f, 0.86f, alpha), 6);
                    break;
            }

            if (water)
            {
                CreateRect(parent, "수색 물결 앞", new Vector2(0f, -0.34f), new Vector2(2.05f, 0.11f), new Color(0.79f, 0.96f, 1f, 0.90f), 6);
                CreateRect(parent, "수색 물결 뒤", new Vector2(0.14f, -0.48f), new Vector2(1.45f, 0.08f), new Color(0.45f, 0.83f, 0.91f, 0.84f), 6);
            }

            PrototypeSearchLootEntry resource = (snapshot.Remaining ?? Array.Empty<PrototypeSearchLootEntry>())
                .FirstOrDefault(value => value != null && !value.IsProtectedPart);
            if (snapshot.State != PrototypeSearchNodeState.Hidden && resource != null)
            {
                CreateSearchNodeResourceCrest(parent, resource.Resource, alpha);
            }
            string marker = snapshot.State == PrototypeSearchNodeState.Hidden ? "?" : snapshot.State == PrototypeSearchNodeState.Depleted ? "×" : "◆";
            CreateWorldLabel(parent, marker, new Vector3(-0.62f, 0.94f, -0.1f), 27, new Color(1f, 0.91f, 0.35f, alpha));
        }

        private void CreateSearchNodeResourceCrest(Transform parent, ResourceKind kind, float alpha)
        {
            Sprite icon = GetResourceIconSprite(kind);
            if (icon == null) return;
            GameObject crest = CreateRect(parent, "채택 자원 문장 바탕 · " + kind, new Vector2(0.48f, 0.98f), new Vector2(0.72f, 0.72f), new Color(0.025f, 0.12f, 0.14f, 0.88f * alpha), 6);
            crest.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject iconObject = new GameObject("채택 자원 아이콘 · " + kind);
            iconObject.transform.SetParent(parent, false);
            iconObject.transform.localPosition = new Vector3(0.48f, 0.98f, -0.1f);
            SpriteRenderer renderer = iconObject.AddComponent<SpriteRenderer>();
            renderer.sprite = icon;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            renderer.sortingOrder = 7;
            float maximumSize = Mathf.Max(0.01f, Mathf.Max(icon.bounds.size.x, icon.bounds.size.y));
            float scale = 0.52f / maximumSize;
            iconObject.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void CreateStoragePlanningSilhouette(Transform parent)
        {
            Color timber = new Color(0.30f, 0.17f, 0.075f, 0.92f);
            Color timberLight = new Color(0.58f, 0.36f, 0.15f, 0.92f);
            CreateRect(parent, "계획용 보급 상자 몸체", Vector2.zero, new Vector2(0.72f, 0.38f), timber, 3);
            CreateRect(parent, "보급 상자 덮개", new Vector2(0f, 0.22f), new Vector2(0.78f, 0.10f), timberLight, 4);
            CreateRect(parent, "보급 상자 왼 보강띠", new Vector2(-0.25f, 0f), new Vector2(0.06f, 0.38f), timberLight, 4);
            CreateRect(parent, "보급 상자 오른 보강띠", new Vector2(0.25f, 0f), new Vector2(0.06f, 0.38f), timberLight, 4);
            CreateRect(parent, "보급 상자 손잡이", new Vector2(0f, 0.03f), new Vector2(0.20f, 0.06f), new Color(0.12f, 0.22f, 0.22f, 0.90f), 5);
            GameObject tag = CreateRect(parent, "작은 증축 계획 꼬리표", new Vector2(0.34f, 0.28f), new Vector2(0.13f, 0.13f), new Color(0.95f, 0.72f, 0.25f, 0.74f), 5);
            tag.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void CreateExpeditionSignSilhouette(Transform parent)
        {
            Color timber = new Color(0.26f, 0.15f, 0.065f, 0.94f);
            Color mapPaper = new Color(0.74f, 0.68f, 0.45f, 0.88f);
            CreateRect(parent, "지도 표지판 왼 말뚝", new Vector2(-0.29f, -0.30f), new Vector2(0.07f, 0.86f), timber, 3).transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            CreateRect(parent, "지도 표지판 오른 말뚝", new Vector2(0.29f, -0.30f), new Vector2(0.07f, 0.86f), timber, 3).transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            GameObject board = CreateRect(parent, "지도 표지판 목판", new Vector2(0f, 0.19f), new Vector2(0.86f, 0.48f), timber, 4);
            board.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            GameObject inset = CreateRect(parent, "지도 표지판 연안 지도", new Vector2(0f, 0.19f), new Vector2(0.66f, 0.30f), mapPaper, 5);
            inset.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            CreateRect(parent, "지도 연안선", new Vector2(-0.08f, 0.19f), new Vector2(0.46f, 0.045f), new Color(0.08f, 0.42f, 0.48f, 0.86f), 6).transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            CreateRect(parent, "지도 출발 화살표", new Vector2(0.21f, 0.25f), new Vector2(0.18f, 0.05f), new Color(0.88f, 0.38f, 0.13f, 0.92f), 6).transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
        }

        private void CreateEscapeProjectSilhouette(Transform parent, string escapeId, Color bodyColor, Color signalColor)
        {
            if (string.Equals(escapeId, "escape.smoke", StringComparison.Ordinal))
            {
                GameObject leftLog = CreateRect(parent, "연기 신호대 왼 장작", new Vector2(-0.13f, -0.03f), new Vector2(0.11f, 0.64f), bodyColor, 4);
                leftLog.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
                GameObject rightLog = CreateRect(parent, "연기 신호대 오른 장작", new Vector2(0.13f, -0.03f), new Vector2(0.11f, 0.64f), bodyColor, 4);
                rightLog.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
                CreateRect(parent, "연기 신호대 점화 접시", new Vector2(0f, 0.19f), new Vector2(0.46f, 0.10f), signalColor, 5);
                CreateRect(parent, "연기 신호대 작은 연무", new Vector2(0.05f, 0.48f), new Vector2(0.16f, 0.20f), new Color(signalColor.r, signalColor.g, signalColor.b, 0.26f), 3).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                return;
            }

            CreateRect(parent, "무전 작업대 상판", new Vector2(0f, 0.10f), new Vector2(0.70f, 0.10f), bodyColor, 4);
            CreateRect(parent, "무전 작업대 왼 다리", new Vector2(-0.25f, -0.16f), new Vector2(0.07f, 0.54f), bodyColor, 3).transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            CreateRect(parent, "무전 작업대 오른 다리", new Vector2(0.25f, -0.16f), new Vector2(0.07f, 0.54f), bodyColor, 3).transform.localRotation = Quaternion.Euler(0f, 0f, 7f);
            CreateRect(parent, "소형 무전기 몸체", new Vector2(0.04f, 0.28f), new Vector2(0.36f, 0.22f), new Color(0.08f, 0.19f, 0.20f, 0.94f), 5);
            CreateRect(parent, "소형 무전기 주파수창", new Vector2(0.02f, 0.29f), new Vector2(0.18f, 0.06f), signalColor, 6);
            CreateRect(parent, "소형 무전기 안테나", new Vector2(0.20f, 0.53f), new Vector2(0.035f, 0.46f), signalColor, 5).transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
        }

        private void CreateShoreLaunchSilhouette(Transform parent)
        {
            Color timber = new Color(0.38f, 0.22f, 0.095f, 0.92f);
            Color rope = new Color(0.78f, 0.62f, 0.31f, 0.82f);
            CreateRect(parent, "진수대 지지목 왼쪽", new Vector2(-0.34f, -0.05f), new Vector2(0.10f, 0.66f), timber, 4).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            CreateRect(parent, "진수대 지지목 오른쪽", new Vector2(0.34f, -0.05f), new Vector2(0.10f, 0.66f), timber, 4).transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            CreateRect(parent, "진수대 받침", new Vector2(0f, -0.16f), new Vector2(0.90f, 0.10f), timber, 4);
            CreateRect(parent, "진수대 묶음 밧줄", new Vector2(0f, 0.08f), new Vector2(0.62f, 0.05f), rope, 5).transform.localRotation = Quaternion.Euler(0f, 0f, -6f);
        }

        private void CreateModuleFoundationStakeSilhouette(Transform parent, CampModuleArchetype archetype, Color guideColor)
        {
            Color stakeColor = new Color(0.30f, 0.17f, 0.075f, Mathf.Max(guideColor.a, 0.42f));
            CreateRect(parent, "증축 기초 왼 말뚝", new Vector2(-0.24f, -0.06f), new Vector2(0.06f, 0.46f), stakeColor, 5);
            CreateRect(parent, "증축 기초 오른 말뚝", new Vector2(0.24f, -0.06f), new Vector2(0.06f, 0.46f), stakeColor, 5);
            CreateRect(parent, "증축 기초 기준줄", new Vector2(0f, 0.03f), new Vector2(0.52f, 0.045f), guideColor, 6);
            float direction = archetype == CampModuleArchetype.Basement ? -1f : 1f;
            GameObject pennant = CreateRect(parent, "증축 방향 작은 깃발", new Vector2(0.24f, 0.18f * direction), new Vector2(0.20f, 0.055f), guideColor, 6);
            pennant.transform.localRotation = Quaternion.Euler(0f, 0f, direction * 28f);
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
