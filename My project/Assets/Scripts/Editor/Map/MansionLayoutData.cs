using UnityEngine;

namespace ProjectRac.Editor.Map
{
    public enum OpeningKind
    {
        None,
        Door,
        Archway
    }

    public readonly struct WallDef
    {
        public readonly string Name;
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public readonly OpeningKind Opening;
        public readonly float OpeningOffsetFromStart;
        public readonly float OpeningWidth;
        public readonly string DoorName;

        public WallDef(
            string name,
            float x1,
            float z1,
            float x2,
            float z2,
            OpeningKind opening = OpeningKind.None,
            float openingOffset = 0f,
            float openingWidth = 0f,
            string doorName = null)
        {
            Name = name;
            Start = new Vector2(x1, z1);
            End = new Vector2(x2, z2);
            Opening = opening;
            OpeningOffsetFromStart = openingOffset;
            OpeningWidth = openingWidth;
            DoorName = doorName;
        }
    }

    public readonly struct SlabRect
    {
        public readonly string Name;
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinZ;
        public readonly float MaxZ;

        public SlabRect(string name, float minX, float maxX, float minZ, float maxZ)
        {
            Name = name;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
        }
    }

    public static class MansionLayoutData
    {
        public const float MapWidth = 30.0f;
        public const float MapDepth = 21.2f;
        public const float FloorHeight = 3.6f;
        public const float SlabThickness = 0.25f;
        public const float Floor2SurfaceY = FloorHeight + SlabThickness; // 3.85m
        public const float WallThickness = 0.28f;
        public const float DoorWidth = 1.50f;
        public const float DoorHeight = 2.50f;

        // Shared architectural grid lines calibrated to the 500x353 blueprint per floor (0.06 m/px).
        public const float XWest = -15.0f;
        public const float XEast = 15.0f;
        public const float ZSouth = -10.6f;
        public const float ZNorth = 10.6f;
        public const float ZMidWall = 2.20f;
        public const float ZUpperCorr = 4.60f;
        public const float ZF2SouthCorr = -8.68f;

        public const float XHallWest = -4.60f;
        public const float XHallEast = 3.00f;
        public const float XVoidEast = 3.20f;

        // Staircase in HALL aligned directly to Floor 2 north stairwell archway.
        public const float StairCenterX = -3.60f;
        public const float StairWidth = 1.70f;
        public const float StairStartZ = -5.40f;
        public const float StairEndZ = 2.06f;
        public const int StairStepCount = 18;

        public static readonly Vector3 PlayerHallSpawn = new Vector3(-0.80f, 0.10f, -7.80f);
        public static readonly Vector3 TestArenaRelocation = new Vector3(75f, 0f, 0f);

        public static readonly SlabRect[] Floor1Slabs =
        {
            new SlabRect("Floor1_Slab", XWest, XEast, ZSouth, ZNorth)
        };

        // Four non-overlapping Floor 2 slab panels framing the open stairwell void over the HALL.
        public static readonly SlabRect[] Floor2Slabs =
        {
            new SlabRect("Floor2_Slab_North", XWest, XEast, StairEndZ, ZNorth),
            new SlabRect("Floor2_Slab_South", XWest, XEast, ZSouth, ZF2SouthCorr),
            new SlabRect("Floor2_Slab_West", XWest, XHallWest, ZF2SouthCorr, StairEndZ),
            new SlabRect("Floor2_Slab_East", XVoidEast, XEast, ZF2SouthCorr, StairEndZ)
        };

        public static readonly SlabRect[] RoofSlabs =
        {
            new SlabRect("Roof_Slab", XWest, XEast, ZSouth, ZNorth)
        };

        public static readonly WallDef[] Floor1Walls =
        {
            // Outer perimeter
            new WallDef("F1_Ext_North", XWest, ZNorth, XEast, ZNorth),
            new WallDef("F1_Ext_West", XWest, ZSouth, XWest, ZNorth),
            new WallDef("F1_Ext_East", XEast, ZSouth, XEast, ZNorth),
            new WallDef("F1_Ext_South", XWest, ZSouth, XEast, ZSouth,
                OpeningKind.Door, openingOffset: 14.20f, openingWidth: 1.60f, doorName: "Door_MainEntrance"),

            // Horizontal dividers
            new WallDef("F1_Cozinha_SalaJantar", XWest, ZMidWall, XHallWest, ZMidWall,
                OpeningKind.Door, openingOffset: 7.80f, openingWidth: DoorWidth, doorName: "Door_Cozinha_SalaDeJantar"),
            new WallDef("F1_Hall_North", XHallWest, ZMidWall, XHallEast, ZMidWall,
                OpeningKind.Archway, openingOffset: 4.00f, openingWidth: 2.00f),
            new WallDef("F1_Corr_SalaEstar", XHallEast, ZMidWall, 7.10f, ZMidWall,
                OpeningKind.Door, openingOffset: 2.60f, openingWidth: DoorWidth, doorName: "Door_SalaDeEstar_Corredor"),
            new WallDef("F1_Biblioteca_SalaEstar", 7.10f, ZMidWall, XEast, ZMidWall),
            new WallDef("F1_Lavanderia_South", XHallWest, ZUpperCorr, 3.65f, ZUpperCorr,
                OpeningKind.Door, openingOffset: 6.55f, openingWidth: DoorWidth, doorName: "Door_Lavanderia"),
            new WallDef("F1_Biblioteca_SouthStub", 3.65f, ZUpperCorr, 7.10f, ZUpperCorr),

            // Vertical dividers
            new WallDef("F1_Hall_West", XHallWest, ZSouth, XHallWest, ZMidWall,
                OpeningKind.Door, openingOffset: 3.00f, openingWidth: DoorWidth, doorName: "Door_SalaDeJantar_Hall"),
            new WallDef("F1_Hall_East", XHallEast, ZSouth, XHallEast, ZMidWall,
                OpeningKind.Door, openingOffset: 3.00f, openingWidth: DoorWidth, doorName: "Door_SalaDeEstar_Hall"),
            new WallDef("F1_Cozinha_Corr", XHallWest, ZMidWall, XHallWest, ZUpperCorr,
                OpeningKind.Door, openingOffset: 1.20f, openingWidth: DoorWidth, doorName: "Door_Cozinha_Corredor"),
            new WallDef("F1_Cozinha_Lavanderia", XHallWest, ZUpperCorr, XHallWest, ZNorth),
            new WallDef("F1_Lavanderia_Biblioteca", 3.65f, ZUpperCorr, 3.65f, ZNorth),
            new WallDef("F1_Biblioteca_Entrance", 7.10f, ZMidWall, 7.10f, ZUpperCorr,
                OpeningKind.Door, openingOffset: 1.20f, openingWidth: DoorWidth, doorName: "Door_Biblioteca")
        };

        public static readonly WallDef[] Floor2Walls =
        {
            // Outer perimeter
            new WallDef("F2_Ext_North", XWest, ZNorth, XEast, ZNorth),
            new WallDef("F2_Ext_South", XWest, ZSouth, XEast, ZSouth),
            new WallDef("F2_Ext_West", XWest, ZSouth, XWest, ZNorth),
            new WallDef("F2_Ext_East", XEast, ZSouth, XEast, ZNorth),

            // Upper row rooms (Quarto Maid, Suite, Quarto Pais upper wing)
            new WallDef("F2_QuartoMaid_South", XWest, ZUpperCorr, -0.40f, ZUpperCorr,
                OpeningKind.Door, openingOffset: 1.90f, openingWidth: DoorWidth, doorName: "Door_QuartoMaid"),
            new WallDef("F2_Suite_South", -0.40f, ZUpperCorr, 8.15f, ZUpperCorr),
            new WallDef("F2_Maid_Suite_Div", -0.40f, ZUpperCorr, -0.40f, ZNorth),
            new WallDef("F2_Suite_Pais_Div", 8.15f, ZUpperCorr, 8.15f, ZNorth,
                OpeningKind.Door, openingOffset: 3.70f, openingWidth: DoorWidth, doorName: "Door_QuartoPais_Suite"),
            new WallDef("F2_Pais_NorthStub", 8.15f, ZMidWall, 8.15f, ZUpperCorr),

            // Mid horizontal band
            new WallDef("F2_QuartoFilha_North", XWest, ZMidWall, -6.50f, ZMidWall),
            new WallDef("F2_Void_North", XHallWest, ZMidWall, XVoidEast, ZMidWall,
                OpeningKind.Archway, openingOffset: 1.00f, openingWidth: 1.85f),
            new WallDef("F2_QuartoPais_North", 5.15f, ZMidWall, 8.15f, ZMidWall,
                OpeningKind.Door, openingOffset: 1.50f, openingWidth: DoorWidth, doorName: "Door_QuartoPais"),

            // Lower horizontal band (South corridor / balcony divider)
            new WallDef("F2_QuartoFilha_South", XWest, ZF2SouthCorr, -6.50f, ZF2SouthCorr,
                OpeningKind.Door, openingOffset: 4.20f, openingWidth: DoorWidth, doorName: "Door_QuartoFilha_Sul"),
            new WallDef("F2_Void_South", XHallWest, ZF2SouthCorr, XVoidEast, ZF2SouthCorr),
            new WallDef("F2_QuartoPais_South", 5.15f, ZF2SouthCorr, XEast, ZF2SouthCorr),

            // Vertical dividers for Quarto Filha, Stairwell Void, and Quarto Pais
            new WallDef("F2_QuartoFilha_East", -6.50f, ZSouth, -6.50f, ZMidWall,
                OpeningKind.Door, openingOffset: 6.00f, openingWidth: DoorWidth, doorName: "Door_QuartoFilha_Corredor"),
            new WallDef("F2_Void_West", XHallWest, ZF2SouthCorr, XHallWest, ZMidWall),
            new WallDef("F2_Void_East", XVoidEast, ZF2SouthCorr, XVoidEast, ZMidWall),
            new WallDef("F2_QuartoPais_West", 5.15f, ZSouth, 5.15f, ZMidWall)
        };

        public static readonly Vector3[] Floor1LightPositions =
        {
            new Vector3(-9.8f, 3.25f, 6.4f),   // Cozinha
            new Vector3(-0.5f, 3.25f, 7.6f),   // Lavanderia
            new Vector3(9.3f, 3.25f, 6.4f),    // Biblioteca
            new Vector3(-9.8f, 3.25f, -4.2f),  // Sala de Jantar
            new Vector3(-0.8f, 3.25f, -4.5f),  // Hall
            new Vector3(9.0f, 3.25f, -4.2f),   // Sala de Estar
            new Vector3(1.2f, 3.25f, 3.4f)     // Corredor superior F1
        };

        public static readonly Vector3[] Floor2LightPositions =
        {
            new Vector3(-7.7f, 7.10f, 7.6f),   // Quarto Maid
            new Vector3(3.9f, 7.10f, 7.6f),    // Suite anexa
            new Vector3(10.5f, 7.10f, -2.5f),  // Quarto Pais
            new Vector3(-10.8f, 7.10f, -3.2f), // Quarto Filha
            new Vector3(-0.5f, 7.10f, 3.4f),   // Corredor Norte F2
            new Vector3(-0.7f, 7.10f, -9.6f)   // Corredor Sul F2
        };
    }
}
