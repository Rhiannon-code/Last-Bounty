using UnityEngine;

namespace FPSParkour.EditorTools
{
    public enum HubKind { None, Plaza, Market, Transit, Yard }

    public static class CityLayout
    {
        public const int Grid = 7;

        public const float BlockPitch = 160f;
        public const float BlockHalf = 56f;
        public const float SubOffset = 28f;
        public const float SubHalf = 22f;
        public const float AlleyHalf = SubOffset - SubHalf; // 6 m each side of the block centreline
        public const float RingGap = 44f;
        public const float CatwalkY = 14f;
        public const float SectorHalf = (Grid - 1) * 0.5f * BlockPitch + BlockHalf + RingGap;
        public const float RingCentre = SectorHalf - RingGap * 0.5f;

        public static readonly float[] Axis = BuildAxis();
        public static readonly float[] StreetCentres = BuildStreetCentres();  
        public static readonly Vector2Int[] SubOffsets =
        {
            new Vector2Int(-1, -1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(1, 1)
        };

        static readonly (int Col, int Row, HubKind Kind)[] Hubs =
        {
            (3, 3, HubKind.Plaza),
            (1, 4, HubKind.Market),
            (4, 1, HubKind.Transit),
            (5, 5, HubKind.Yard),
        };

        public static HubKind HubAt(int col, int row)
        {
            foreach ((int Col, int Row, HubKind Kind) hub in Hubs)
            {
                if (hub.Col == col && hub.Row == row)
                    return hub.Kind;
            }

            return HubKind.None;
        }

        public static bool IsHub(int col, int row) => HubAt(col, row) != HubKind.None;

        public static int HubCount => Hubs.Length;

        public static HubKind HubKindAt(int index) => Hubs[index].Kind;

        public static Vector3 HubCentre(int index) => BlockCentre(Hubs[index].Col, Hubs[index].Row);

        public static Vector3 BlockCentre(int col, int row) => new Vector3(Axis[col], 0f, Axis[row]);

        public static Vector3 SubCentre(int col, int row, int sub)
        {
            Vector2Int offset = SubOffsets[sub];
            return BlockCentre(col, row) + new Vector3(offset.x * SubOffset, 0f, offset.y * SubOffset);
        }

        public const float LowBandCeiling = 22f;
        public const float BoomSpacing = 18f;
        public const float BoomLowest = 20f;
        public const float BoomRise = 9.6f;

        static readonly (float Base, int Spread)[] Bands =
        {
            (18f, 5),   // Low  18-22, the runner's tier
            (34f, 7),   // Mid  34-40
            (52f, 9),   // High 52-60
            (70f, 15),  // Top  70-84
        };

        public static float Height(int col, int row, int sub)
        {
            int band = BandOf(col, row, sub);
            return Bands[band].Base + Hash(col, row, sub) % Bands[band].Spread;
        }

        public static int BandOf(int col, int row, int sub)
        {
            return (sub + Hash(col, row, 97)) % Bands.Length;
        }

        public static int LowestSub(int col, int row)
        {
            for (int sub = 0; sub < SubOffsets.Length; sub++)
            {
                if (BandOf(col, row, sub) == 0)
                    return sub;
            }

            return 0;
        }

        public static int SubIndex(int xSign, int zSign)
        {
            for (int i = 0; i < SubOffsets.Length; i++)
            {
                if (SubOffsets[i].x == xSign && SubOffsets[i].y == zSign)
                    return i;
            }

            return 0;
        }

        static float[] BuildAxis()
        {
            float[] axis = new float[Grid];

            for (int i = 0; i < Grid; i++)
                axis[i] = (i - (Grid - 1) * 0.5f) * BlockPitch;

            return axis;
        }

        static float[] BuildStreetCentres()
        {
            float[] centres = new float[Grid - 1];

            for (int i = 0; i < centres.Length; i++)
                centres[i] = (Axis[i] + Axis[i + 1]) * 0.5f;

            return centres;
        }

        static int Hash(int col, int row, int sub)
        {
            int h = col * 73856093 ^ row * 19349663 ^ sub * 83492791;
            h ^= h >> 13;
            return h & 0x7FFFFFFF;
        }
    }
}
