using System.Collections.Generic;
using FPSParkour.Core;
using FPSParkour.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public static class CityGeometry
    {
        static readonly Color Road = new Color(0.19f, 0.19f, 0.22f);
        static readonly Color Kerb = new Color(0.30f, 0.30f, 0.34f);
        static readonly Color Wall = new Color(0.32f, 0.34f, 0.40f);
        static readonly Color Trim = new Color(0.42f, 0.44f, 0.50f);
        static readonly Color Prop = new Color(0.46f, 0.42f, 0.36f);
        static readonly Color Steel = new Color(0.50f, 0.52f, 0.56f);

        public static void Build(Transform root, List<Transform> fleeNodes)
        {
            Random.InitState(20260904);

            BuildGround(root);
            BuildBlocks(root);
            BuildHubs(root);
            BuildCatwalks(root);
            BuildStreetClutter(root);
            BuildHazards(root);
            BuildFleeNodes(root, fleeNodes);
        }

        static void BuildGround(Transform root)
        {
            Transform ground = new GameObject("Ground").transform;
            ground.SetParent(root, false);

            float span = CityLayout.SectorHalf * 2f;
            PlayerRigBuilder.Box(ground, "Slab", new Vector3(0f, -0.5f, 0f), new Vector3(span, 1f, span), Road);

            foreach (int sign in new[] { -1, 1 })
            {
                PlayerRigBuilder.Box(ground, $"Wall_X{sign}", new Vector3(sign * CityLayout.SectorHalf, 6f, 0f),
                    new Vector3(2f, 12f, span), Kerb);
                PlayerRigBuilder.Box(ground, $"Wall_Z{sign}", new Vector3(0f, 6f, sign * CityLayout.SectorHalf),
                    new Vector3(span, 12f, 2f), Kerb);
            }
        }

        static void BuildBlocks(Transform root)
        {
            Transform blocks = new GameObject("Blocks").transform;
            blocks.SetParent(root, false);

            for (int col = 0; col < CityLayout.Grid; col++)
            {
                for (int row = 0; row < CityLayout.Grid; row++)
                {
                    if (CityLayout.IsHub(col, row))
                        continue;

                    BuildBlock(blocks, col, row);
                }
            }
        }

        static void BuildBlock(Transform parent, int col, int row)
        {
            Transform block = new GameObject($"Block_{col}{row}").transform;
            block.SetParent(parent, false);
            block.localPosition = CityLayout.BlockCentre(col, row);

            float size = CityLayout.SubHalf * 2f;
            int lowest = CityLayout.LowestSub(col, row);

            for (int sub = 0; sub < 4; sub++)
            {
                float height = CityLayout.Height(col, row, sub);
                Vector2Int offset = CityLayout.SubOffsets[sub];
                Vector3 centre = new Vector3(offset.x * CityLayout.SubOffset, 0f, offset.y * CityLayout.SubOffset);

                PlayerRigBuilder.Box(block, $"Tower_{sub}", centre + Vector3.up * (height * 0.5f),
                    new Vector3(size, height, size), Wall);

                if (CityLayout.BandOf(col, row, sub) == 0)
                    BuildParapet(block, sub, centre, height, size);

                BuildRoofHut(block, sub, centre, height);
                BuildFireEscape(block, sub, centre, height, offset);
                BuildBooms(block, sub, centre, height);
            }

            BuildAlleyPlanks(block, col, row);
            BuildMast(block, col, row, lowest);
            BuildChimney(block, col, row);
        }

        static void BuildBooms(Transform block, int sub, Vector3 centre, float height)
        {
            int index = 0;

            for (float y = CityLayout.BoomLowest; y < height - 4f; y += CityLayout.BoomSpacing)
            {
                PlayerRigBuilder.Box(block, $"Boom_{sub}_{index}", centre + new Vector3(CityLayout.SubHalf + 3f, y, 0f),
                    new Vector3(7f, 0.5f, 0.5f), Steel);
                PlayerRigBuilder.Box(block, $"BoomStay_{sub}_{index}", centre + new Vector3(CityLayout.SubHalf + 0.6f, y + 1.2f, 0f),
                    new Vector3(0.4f, 2.6f, 0.4f), Steel);
                index++;
            }
        }

        static void BuildChimney(Transform block, int col, int row)
        {
            float tallest = 0f;

            for (int sub = 0; sub < 4; sub++)
                tallest = Mathf.Max(tallest, CityLayout.Height(col, row, sub));

            foreach (int side in new[] { -1, 1 })
            {
                PlayerRigBuilder.Box(block, $"ChimneyFin_{side}", new Vector3(side * 2.1f, tallest * 0.5f, 0f),
                    new Vector3(0.8f, tallest, 9f), Wall);
            }

            for (int i = 1; i * 12f < tallest; i++)
            {
                PlayerRigBuilder.Box(block, $"ChimneyLedge_{i}", new Vector3((i % 2 == 0 ? 1f : -1f) * 3.2f, i * 12f, 0f),
                    new Vector3(3f, 0.5f, 5f), Trim);
            }
        }

        static void BuildParapet(Transform block, int sub, Vector3 centre, float height, float size)
        {
            foreach (Vector3 edge in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Vector3 offset = edge * (CityLayout.SubHalf - 0.3f) + Vector3.up * (height + 0.5f);
                Vector3 scale = edge.x != 0f ? new Vector3(0.6f, 1f, size) : new Vector3(size, 1f, 0.6f);
                PlayerRigBuilder.Box(block, $"Parapet_{sub}_{edge}", centre + offset, scale, Trim);
            }
        }

        static void BuildRoofHut(Transform block, int sub, Vector3 centre, float height)
        {
            PlayerRigBuilder.Box(block, $"RoofHut_{sub}", centre + new Vector3(6f, height + 1.6f, -5f),
                new Vector3(7f, 3.2f, 5f), Trim);

            // A 2.1 m step next to a 3.2 m hut: the mantle limit made into a decision.
            PlayerRigBuilder.Box(block, $"RoofCrate_{sub}", centre + new Vector3(-7f, height + 1f, 6f),
                new Vector3(3f, 2f, 3f), Prop);
        }

        static void BuildFireEscape(Transform block, int sub, Vector3 centre, float height, Vector2Int offset)
        {
            Vector3 face = new Vector3(-offset.x, 0f, 0f) * CityLayout.SubHalf;
            bool lowBand = height <= CityLayout.LowBandCeiling;
            int steps = Mathf.FloorToInt(height / 1.5f);

            if (!lowBand)
                steps = Mathf.Min(6, steps);

            for (int i = 1; i <= steps; i++)
            {
                Vector3 position = centre + face + new Vector3(offset.x * -0.9f, i * 1.5f, (i % 2 == 0 ? 4f : -4f));
                PlayerRigBuilder.Box(block, $"Escape_{sub}_{i}", position, new Vector3(2.6f, 0.3f, 3f), Steel);
            }
        }

        static void BuildAlleyPlanks(Transform block, int col, int row)
        {
            BuildPlank(block, col, row, 0, 1, true);
            BuildPlank(block, col, row, 0, 2, false);
        }

        static void BuildPlank(Transform block, int col, int row, int a, int b, bool alongX)
        {
            float height = Mathf.Min(CityLayout.Height(col, row, a), CityLayout.Height(col, row, b));
            Vector3 midpoint = (LocalSub(a) + LocalSub(b)) * 0.5f;
            Vector3 size = alongX ? new Vector3(CityLayout.SubOffset * 2f, 0.4f, 2.5f)
                                  : new Vector3(2.5f, 0.4f, CityLayout.SubOffset * 2f);

            PlayerRigBuilder.Box(block, $"Plank_{a}{b}", midpoint + Vector3.up * height, size, Steel);
        }

        static Vector3 LocalSub(int sub)
        {
            Vector2Int offset = CityLayout.SubOffsets[sub];
            return new Vector3(offset.x * CityLayout.SubOffset, 0f, offset.y * CityLayout.SubOffset);
        }

        static void BuildMast(Transform block, int col, int row, int sub)
        {
            float height = CityLayout.Height(col, row, sub);
            Vector3 centre = LocalSub(sub);

            PlayerRigBuilder.Box(block, "Mast", centre + Vector3.up * (height + 5f), new Vector3(0.6f, 10f, 0.6f), Steel);
            PlayerRigBuilder.Box(block, "Boom", centre + new Vector3(3f, height + 9.6f, 0f), new Vector3(7f, 0.5f, 0.5f), Steel);
        }

        static void BuildHubs(Transform root)
        {
            Transform hubs = new GameObject("Hubs").transform;
            hubs.SetParent(root, false);

            for (int i = 0; i < CityLayout.HubCount; i++)
            {
                HubKind kind = CityLayout.HubKindAt(i);

                Transform hub = new GameObject($"Hub_{kind}").transform;
                hub.SetParent(hubs, false);
                hub.localPosition = CityLayout.HubCentre(i);

                PlayerRigBuilder.Box(hub, "Paving", new Vector3(0f, 0.05f, 0f),
                    new Vector3(CityLayout.BlockHalf * 2f, 0.1f, CityLayout.BlockHalf * 2f), Kerb);

                switch (kind)
                {
                    case HubKind.Plaza: BuildPlaza(hub); break;
                    case HubKind.Market: BuildMarket(hub); break;
                    case HubKind.Transit: BuildTransit(hub); break;
                    case HubKind.Yard: BuildYard(hub); break;
                }
            }
        }

        static void BuildPlaza(Transform plaza)
        {

            PlayerRigBuilder.Box(plaza, "Stage", new Vector3(0f, 0.6f, 0f), new Vector3(16f, 1.2f, 16f), Trim);
            PlayerRigBuilder.Box(plaza, "Monument", new Vector3(0f, 8f, 0f), new Vector3(3f, 16f, 3f), Steel);
            PlayerRigBuilder.Box(plaza, "MonumentBoom", new Vector3(0f, 15.4f, 0f), new Vector3(9f, 0.5f, 0.5f), Steel);

            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                Vector3 at = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 34f;

                PlayerRigBuilder.Box(plaza, $"StallCounter_{i}", at + Vector3.up * 0.5f, new Vector3(4f, 1f, 1.6f), Prop);
                PlayerRigBuilder.Box(plaza, $"StallCanopy_{i}", at + Vector3.up * 2.6f, new Vector3(4.6f, 0.2f, 3f), Trim);
                PlayerRigBuilder.Box(plaza, $"StallPost_{i}", at + new Vector3(2f, 1.3f, 1.2f), new Vector3(0.2f, 2.6f, 0.2f), Steel);
            }

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                    BuildScaffold(plaza, new Vector3(sx * 44f, 0f, sz * 44f), $"Scaffold_{sx}{sz}");
            }

            // Free-standing wall-run runs, so the plaza is not a flat arena you can only walk across.
            PlayerRigBuilder.Box(plaza, "RunWall_A", new Vector3(-22f, 3.5f, 20f), new Vector3(26f, 7f, 1.2f), Wall);
            PlayerRigBuilder.Box(plaza, "RunLedge_A", new Vector3(-6f, 7.3f, 22f), new Vector3(6f, 0.6f, 4f), Trim);
            PlayerRigBuilder.Box(plaza, "RunWall_B", new Vector3(24f, 3.5f, -18f), new Vector3(1.2f, 7f, 26f), Wall);
            PlayerRigBuilder.Box(plaza, "RunLedge_B", new Vector3(22f, 7.3f, -2f), new Vector3(4f, 0.6f, 6f), Trim);
        }

        static void BuildMarket(Transform market)
        {
            for (int lane = -2; lane <= 2; lane++)
            {
                float x = lane * 18f;

                for (int i = 0; i < 7; i++)
                {
                    float z = -42f + i * 14f;

                    PlayerRigBuilder.Box(market, $"Stall_{lane}_{i}", new Vector3(x, 1.1f, z), new Vector3(9f, 2.2f, 5f), Prop);
                    PlayerRigBuilder.Box(market, $"Awning_{lane}_{i}", new Vector3(x, 3.1f, z), new Vector3(11f, 0.2f, 7f), Trim);
                    PlayerRigBuilder.Box(market, $"Crate_{lane}_{i}", new Vector3(x + 5.4f, 0.6f, z + 4f), new Vector3(1.4f, 1.2f, 1.4f), Prop);
                }
            }

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    PlayerRigBuilder.Box(market, $"RunWall_{sx}{sz}", new Vector3(sx * 48f, 3.5f, sz * 26f),
                        new Vector3(1.2f, 7f, 30f), Wall);
                }
            }

            BuildScaffold(market, new Vector3(0f, 0f, 48f), "Scaffold_Market");
        }

        static void BuildTransit(Transform transit)
        {
            PlayerRigBuilder.Box(transit, "Canopy", new Vector3(0f, 12f, 0f), new Vector3(86f, 0.6f, 52f), Trim);

            for (int i = 0; i < 6; i++)
            {
                float x = -35f + i * 14f;
                PlayerRigBuilder.Box(transit, $"Column_N{i}", new Vector3(x, 6f, -24f), new Vector3(1.6f, 12f, 1.6f), Steel);
                PlayerRigBuilder.Box(transit, $"Column_S{i}", new Vector3(x, 6f, 24f), new Vector3(1.6f, 12f, 1.6f), Steel);
            }

            PlayerRigBuilder.Box(transit, "Platform", new Vector3(0f, 0.7f, -14f), new Vector3(80f, 1.4f, 12f), Kerb);
            PlayerRigBuilder.Box(transit, "PlatformEdge", new Vector3(0f, 1.5f, -20.4f), new Vector3(80f, 0.3f, 0.8f), Trim);

            for (int i = 0; i < 8; i++)
            {
                float x = -31.5f + i * 9f;
                PlayerRigBuilder.Box(transit, $"Turnstile_{i}", new Vector3(x, 0.6f, 12f), new Vector3(5f, 1.2f, 1f), Steel);
            }

            PlayerRigBuilder.Box(transit, "Stair", new Vector3(-44f, 3f, 34f), new Vector3(10f, 6f, 6f), Kerb);
            PlayerRigBuilder.Box(transit, "StairTop", new Vector3(-44f, 6.3f, 26f), new Vector3(10f, 0.6f, 12f), Trim);
            BuildScaffold(transit, new Vector3(44f, 0f, 34f), "Scaffold_Transit");
        }

        static void BuildYard(Transform yard)
        {
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    float x = -40f + col * 20f;
                    float z = -33f + row * 22f;
                    int stack = 1 + (col + row) % 3;

                    for (int level = 0; level < stack; level++)
                    {
                        PlayerRigBuilder.Box(yard, $"Container_{col}{row}_{level}",
                            new Vector3(x, 1.4f + level * 2.8f, z), new Vector3(12f, 2.8f, 5f),
                            level % 2 == 0 ? Prop : Wall);
                    }
                }
            }

            PlayerRigBuilder.Box(yard, "GantryLegA", new Vector3(-46f, 9f, 0f), new Vector3(2f, 18f, 2f), Steel);
            PlayerRigBuilder.Box(yard, "GantryLegB", new Vector3(46f, 9f, 0f), new Vector3(2f, 18f, 2f), Steel);
            PlayerRigBuilder.Box(yard, "GantryBeam", new Vector3(0f, 18.4f, 0f), new Vector3(96f, 1.2f, 4f), Steel);
            PlayerRigBuilder.Box(yard, "GantryWalk", new Vector3(0f, 19.2f, 3.4f), new Vector3(96f, 0.4f, 2.4f), Trim);

            BuildScaffold(yard, new Vector3(-46f, 0f, 44f), "Scaffold_Yard");
        }

        static void BuildScaffold(Transform parent, Vector3 at, string name)
        {
            Transform scaffold = new GameObject(name).transform;
            scaffold.SetParent(parent, false);
            scaffold.localPosition = at;

            for (int i = 0; i < 8; i++)
            {
                float y = 1.8f + i * 1.7f;
                float offset = i % 2 == 0 ? 1.6f : -1.6f;
                PlayerRigBuilder.Box(scaffold, $"Deck_{i}", new Vector3(offset, y, 0f), new Vector3(5f, 0.3f, 4f), Steel);
            }

            PlayerRigBuilder.Box(scaffold, "Top", new Vector3(0f, CityLayout.CatwalkY + 0.3f, 0f), new Vector3(7f, 0.4f, 7f), Steel);
        }

        static void BuildCatwalks(Transform root)
        {
            Transform catwalks = new GameObject("Catwalks").transform;
            catwalks.SetParent(root, false);

            for (int row = 0; row < CityLayout.Grid; row++)
            {
                for (int col = 0; col < CityLayout.Grid - 1; col++)
                    BuildCrossing(catwalks, col, row, col + 1, row, true);
            }

            for (int col = 0; col < CityLayout.Grid; col++)
            {
                for (int row = 0; row < CityLayout.Grid - 1; row++)
                    BuildCrossing(catwalks, col, row, col, row + 1, false);
            }
        }

        static void BuildCrossing(Transform parent, int colA, int rowA, int colB, int rowB, bool alongX)
        {
            if (CityLayout.IsHub(colA, rowA) || CityLayout.IsHub(colB, rowB))
                return;

            foreach (int lane in new[] { -1, 1 })
            {
                int subA = alongX ? CityLayout.SubIndex(1, lane) : CityLayout.SubIndex(lane, 1);
                int subB = alongX ? CityLayout.SubIndex(-1, lane) : CityLayout.SubIndex(lane, -1);

                Vector3 a = CityLayout.SubCentre(colA, rowA, subA);
                Vector3 b = CityLayout.SubCentre(colB, rowB, subB);
                float height = Mathf.Min(CityLayout.Height(colA, rowA, subA), CityLayout.Height(colB, rowB, subB));

                Vector3 centre = (a + b) * 0.5f + Vector3.up * height;
                float span = Vector3.Distance(a, b);
                Vector3 size = alongX ? new Vector3(span, 0.5f, 4f) : new Vector3(4f, 0.5f, span);

                PlayerRigBuilder.Box(parent, $"Catwalk_{colA}{rowA}_{colB}{rowB}_{lane}", centre, size, Steel);
            }
        }

        static void BuildStreetClutter(Transform root)
        {
            Transform clutter = new GameObject("Clutter").transform;
            clutter.SetParent(root, false);

            foreach (float street in CityLayout.StreetCentres)
            {
                for (int i = 0; i < 14; i++)
                {
                    float along = Random.Range(-CityLayout.SectorHalf + 30f, CityLayout.SectorHalf - 30f);
                    float across = Random.Range(-16f, 16f);

                    PlayerRigBuilder.Box(clutter, $"Container_X{i}", new Vector3(street + across, 1.3f, along),
                        new Vector3(2.8f, 2.6f, 7f), Prop);
                    PlayerRigBuilder.Box(clutter, $"Container_Z{i}", new Vector3(along, 1.3f, street + across),
                        new Vector3(7f, 2.6f, 2.8f), Prop);
                }
            }

            float reach = CityLayout.SectorHalf - 30f;
            int barricades = Mathf.RoundToInt(26f * (CityLayout.Grid / 3f) * (CityLayout.Grid / 3f));

            for (int i = 0; i < barricades; i++)
            {
                Vector3 at = new Vector3(Random.Range(-reach, reach), 0.55f, Random.Range(-reach, reach));
                PlayerRigBuilder.Box(clutter, $"Barricade_{i}", at, new Vector3(Random.Range(3f, 6f), 1.1f, 1f), Trim);
            }
        }

        static void BuildHazards(Transform root)
        {
            Transform hazards = new GameObject("Hazards").transform;
            hazards.SetParent(root, false);

            for (int i = 0; i < CityLayout.StreetCentres.Length; i += 2)
                BuildTramLine(hazards, CityLayout.StreetCentres[i], i);

            for (int i = 1; i < CityLayout.StreetCentres.Length; i += 2)
                BuildSkiffLane(hazards, CityLayout.StreetCentres[i], i);
            BuildSteamVents(hazards);
            BuildSpills(hazards);
        }

        static void BuildTramLine(Transform parent, float x, int index)
        {
            float span = CityLayout.SectorHalf * 2f - 40f;

            PlayerRigBuilder.Box(parent, $"TramRail_{index}", new Vector3(x, 0.06f, 0f), new Vector3(5f, 0.12f, span), Steel);

            GameObject rail = Volume(parent, $"TramRail_Live_{index}", new Vector3(x, 0.7f, 0f), new Vector3(5f, 1.4f, span));
            Hazard live = rail.AddComponent<Hazard>();
            new AssetAuthoring(live)
                .Enum("mode", (int)HazardMode.Continuous)
                .Enum("damageType", (int)DamageType.Energy)
                .Float("damagePerSecond", 11f)
                .Save();

            GameObject tram = new GameObject("Tram");
            tram.transform.SetParent(parent, false);

            NavMeshModifier modifier = tram.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;

            PlayerRigBuilder.Box(tram.transform, "Body", new Vector3(0f, 2.6f, 0f), new Vector3(4.6f, 4.4f, 18f),
                new Color(0.55f, 0.28f, 0.24f));

            GameObject strike = Volume(tram.transform, "Strike", new Vector3(0f, 2.6f, 0f), new Vector3(5.4f, 5f, 19f));
            Hazard hit = strike.AddComponent<Hazard>();
            new AssetAuthoring(hit)
                .Enum("mode", (int)HazardMode.OnEnter)
                .Enum("damageType", (int)DamageType.Kinetic)
                .Float("damagePerHit", 65f)
                .Bool("raisesAlarm", true)
                .Save();

            HazardMover mover = tram.AddComponent<HazardMover>();
            new AssetAuthoring(mover)
                .Vectors("points", new Vector3(x, 0f, -span * 0.5f), new Vector3(x, 0f, span * 0.5f))
                .Float("speed", 24f)
                .Bool("pingPong", true)
                .Bool("faceTravel", false)
                .Float("waitAtPoint", 3f)
                .Save();
        }

        static void BuildSkiffLane(Transform parent, float z, int index)
        {
            float[] speeds = { 17f, 21f, 14f };

            for (int i = 0; i < speeds.Length; i++)
            {
                GameObject skiff = new GameObject($"Skiff_{index}_{i}");
                skiff.transform.SetParent(parent, false);

                NavMeshModifier modifier = skiff.AddComponent<NavMeshModifier>();
                modifier.ignoreFromBuild = true;
                modifier.applyToChildren = true;

                float lane = z + (i - 1) * 9f;
                PlayerRigBuilder.Box(skiff.transform, "Hull", new Vector3(0f, 3.4f, 0f), new Vector3(7f, 2f, 3.4f),
                    new Color(0.3f, 0.42f, 0.5f));

                GameObject strike = Volume(skiff.transform, "Strike", new Vector3(0f, 3.4f, 0f), new Vector3(7.6f, 2.6f, 4f));
                Hazard hit = strike.AddComponent<Hazard>();
                new AssetAuthoring(hit)
                    .Enum("mode", (int)HazardMode.OnEnter)
                    .Enum("damageType", (int)DamageType.Kinetic)
                    .Float("damagePerHit", 38f)
                    .Save();

                HazardMover mover = skiff.AddComponent<HazardMover>();
                new AssetAuthoring(mover)
                    .Vectors("points",
                        new Vector3(-CityLayout.SectorHalf + 15f, 0f, lane),
                        new Vector3(CityLayout.SectorHalf - 15f, 0f, lane))
                    .Float("speed", speeds[i])
                    .Bool("pingPong", false)
                    .Save();
            }
        }

        static void BuildSteamVents(Transform parent)
        {
            int index = 0;

            for (int col = 0; col < CityLayout.Grid; col++)
            {
                for (int row = 0; row < CityLayout.Grid; row++)
                {
                    if (CityLayout.IsHub(col, row))
                        continue;

                    Vector3 centre = CityLayout.BlockCentre(col, row);

                    foreach (float along in new[] { -CityLayout.SubOffset, CityLayout.SubOffset })
                    {
                        Vector3 at = centre + new Vector3(along, 0f, index % 2 == 0 ? 0f : CityLayout.SubOffset);
                        BuildVent(parent, $"Vent_{index}", at, index * 0.73f);
                        index++;
                    }
                }
            }
        }

        static void BuildVent(Transform parent, string name, Vector3 at, float phase)
        {
            Transform vent = new GameObject(name).transform;
            vent.SetParent(parent, false);
            vent.localPosition = at;

            GameObject grate = PlayerRigBuilder.Box(vent, "Grate", new Vector3(0f, 0.08f, 0f),
                new Vector3(2.6f, 0.16f, 2.6f), new Color(0.35f, 0.32f, 0.2f));

            GameObject plume = PlayerRigBuilder.Box(vent, "Plume", new Vector3(0f, 3f, 0f),
                new Vector3(2.2f, 6f, 2.2f), new Color(0.8f, 0.8f, 0.85f));
            Object.DestroyImmediate(plume.GetComponent<Collider>());

            // Without this the plume is baked as a pillar and the runner routes around thin air.
            NavMeshModifier modifier = plume.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;

            GameObject volume = Volume(vent, "Volume", new Vector3(0f, 3f, 0f), new Vector3(2.6f, 6f, 2.6f));
            Hazard hazard = volume.AddComponent<Hazard>();
            new AssetAuthoring(hazard)
                .Enum("mode", (int)HazardMode.Pulsed)
                .Enum("damageType", (int)DamageType.Energy)
                .Float("damagePerSecond", 34f)
                .Float("cycleSeconds", 4.2f)
                .Float("activeSeconds", 1.3f)
                .Float("phaseOffset", phase)
                .Ref("tell", grate.GetComponent<Renderer>())
                .Save();
        }

        static void BuildSpills(Transform parent)
        {
            List<Vector3> places = new List<Vector3>();

            for (int i = 0; i < CityLayout.StreetCentres.Length; i++)
            {
                float lane = CityLayout.StreetCentres[i];

                foreach (float along in CityLayout.Axis)
                {
                    if (i % 2 == 0)
                        places.Add(new Vector3(lane + 12f, 0f, along));
                    else
                        places.Add(new Vector3(along, 0f, lane - 12f));
                }
            }

            for (int i = 0; i < places.Count; i++)
            {
                Transform spill = new GameObject($"Spill_{i}").transform;
                spill.SetParent(parent, false);
                spill.localPosition = places[i];

                GameObject decal = PlayerRigBuilder.Box(spill, "Pool", new Vector3(0f, 0.07f, 0f),
                    new Vector3(7f, 0.14f, 7f), new Color(0.4f, 0.55f, 0.3f));

                GameObject volume = Volume(spill, "Volume", new Vector3(0f, 0.8f, 0f), new Vector3(7f, 1.6f, 7f));
                Hazard hazard = volume.AddComponent<Hazard>();
                new AssetAuthoring(hazard)
                    .Enum("mode", (int)HazardMode.Continuous)
                    .Enum("damageType", (int)DamageType.Corrosive)
                    .Float("damagePerSecond", 15f)
                    .Ref("tell", decal.GetComponent<Renderer>())
                    .Save();
            }
        }

        static GameObject Volume(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            GameObject volume = new GameObject(name);
            volume.transform.SetParent(parent, false);
            volume.transform.localPosition = centre;

            BoxCollider collider = volume.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = true;

            return volume;
        }

        static void BuildFleeNodes(Transform root, List<Transform> into)
        {
            Transform nodes = new GameObject("FleeNodes").transform;
            nodes.SetParent(root, false);

            float[] lanes = new float[CityLayout.StreetCentres.Length + 2];
            lanes[0] = -CityLayout.RingCentre;
            lanes[lanes.Length - 1] = CityLayout.RingCentre;
            System.Array.Copy(CityLayout.StreetCentres, 0, lanes, 1, CityLayout.StreetCentres.Length);

            foreach (float x in lanes)
            {
                foreach (float z in lanes)
                    Add(nodes, into, new Vector3(x, 0f, z));
            }

            for (int col = 0; col < CityLayout.Grid; col++)
            {
                for (int row = 0; row < CityLayout.Grid; row++)
                {
                    if (!CityLayout.IsHub(col, row))
                        Add(nodes, into, CityLayout.BlockCentre(col, row));
                }
            }

            for (int i = 0; i < CityLayout.HubCount; i++)
            {
                Vector3 centre = CityLayout.HubCentre(i);

                foreach (int sx in new[] { -1, 1 })
                {
                    foreach (int sz in new[] { -1, 1 })
                        Add(nodes, into, centre + new Vector3(sx * 46f, 0f, sz * 46f));
                }
            }

            for (int col = 0; col < CityLayout.Grid; col++)
            {
                for (int row = 0; row < CityLayout.Grid; row++)
                {
                    if (CityLayout.IsHub(col, row))
                        continue;

                    int sub = CityLayout.LowestSub(col, row);
                    Add(nodes, into, CityLayout.SubCentre(col, row, sub)
                                     + Vector3.up * (CityLayout.Height(col, row, sub) + 0.2f));
                }
            }
        }

        public static void BuildLinks(Transform root)
        {
            Transform links = new GameObject("NavLinks").transform;
            links.SetParent(root, false);

            for (int col = 0; col < CityLayout.Grid; col++)
            {
                for (int row = 0; row < CityLayout.Grid; row++)
                {
                    if (CityLayout.IsHub(col, row))
                        continue;

                    int sub = CityLayout.LowestSub(col, row);
                    Vector2Int offset = CityLayout.SubOffsets[sub];
                    Vector3 centre = CityLayout.SubCentre(col, row, sub);
                    float height = CityLayout.Height(col, row, sub);

                    Vector3 face = new Vector3(-offset.x, 0f, 0f);
                    Vector3 street = centre + face * (CityLayout.SubHalf + 3f) + Vector3.up * 0.2f;
                    Vector3 roof = centre + face * (CityLayout.SubHalf - 4f) + Vector3.up * (height + 0.2f);

                    Link(links, $"Climb_{col}{row}", street, roof, bidirectional: true, cost: 10f);

                    Vector3 farRoof = centre - face * (CityLayout.SubHalf - 4f) + Vector3.up * (height + 0.2f);
                    Vector3 farStreet = centre - face * (CityLayout.SubHalf + 3f) + Vector3.up * 0.2f;

                    Link(links, $"Drop_{col}{row}", farRoof, farStreet, bidirectional: false, cost: 1f);
                }
            }
        }

        static void Link(Transform parent, string name, Vector3 from, Vector3 to, bool bidirectional, float cost)
        {
            GameObject linkObject = new GameObject(name);
            linkObject.transform.SetParent(parent, false);
            linkObject.transform.position = from;

            NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
            link.startPoint = Vector3.zero;          // Local to the GameObject origin, which is `from`
            link.endPoint = to - from;
            link.width = 2f;
            link.bidirectional = bidirectional;
            link.costModifier = cost;
            link.UpdateLink();
        }

        static void Add(Transform parent, List<Transform> into, Vector3 at)
        {
            GameObject node = new GameObject($"Node_{into.Count:00}");
            node.transform.SetParent(parent, false);
            node.transform.localPosition = at;
            into.Add(node.transform);
        }
    }
}
