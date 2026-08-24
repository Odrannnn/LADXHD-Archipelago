using System;

namespace ProjectZ.InGame.Archipelago
{
    public readonly struct MagpieTrackerLocation : IEquatable<MagpieTrackerLocation>
    {
        public MagpieTrackerLocation(string room, double x, double y, bool drawFine)
        {
            Room = room;
            X = x;
            Y = y;
            DrawFine = drawFine;
        }

        public string Room { get; }
        public double X { get; }
        public double Y { get; }
        public bool DrawFine { get; }

        public bool Equals(MagpieTrackerLocation other) =>
            string.Equals(Room, other.Room, StringComparison.Ordinal) &&
            X.Equals(other.X) && Y.Equals(other.Y) && DrawFine == other.DrawFine;

        public override bool Equals(object obj) => obj is MagpieTrackerLocation other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Room, X, Y, DrawFine);
    }

    public static class MagpieTrackerLocationMapper
    {
        private const int FieldWidth = 160;
        private const int FieldHeight = 128;
        private const int TileSize = 16;
        private const string UnknownInteriorRoom = "0x2A3";

        // Public Magpie room metadata (kbranch/Magpie static/js/metadata/mapMetadata.js),
        // encoded as fixed-width room suffixes by tracker-map row. See third_party/Magpie/LICENSE.txt.
        // A missing room is written as ---. This lets the HD engine translate its existing
        // dungeon minimap coordinate directly into the room IDs expected by Magpie GPS.
        private static readonly string[][] DungeonRoomRows =
        {
            new[]
            {
                "300 301 --- --- 302 303",
                "304 305 306 307 308 309",
                "--- 30A 30B 30C 30D ---",
                "--- 30E 30F 310 311 ---",
                "--- 312 313 314 315 ---"
            },
            new[]
            {
                "--- --- --- --- --- --- 102",
                "--- 103 104 105 --- --- 106",
                "11D --- 107 108 109 10A 10B",
                "11C 10C 10D 10E 10F 110 111",
                "101 --- 112 113 114 --- ---",
                "--- 115 116 117 --- --- ---"
            },
            new[]
            {
                "120 121 122 123 124 125",
                "--- 126 --- --- 127 ---",
                "128 129 --- --- 12A 12B",
                "12C --- --- --- --- 12D",
                "12E --- --- --- --- 12F",
                "130 131 132 133 134 135",
                "--- 136 137 138 139 ---"
            },
            new[]
            {
                "140 141 142 143 --- --- ---",
                "144 145 146 147 --- --- ---",
                "148 149 14A 14B --- 154 ---",
                "--- 14C 14D --- 155 156 157",
                "--- 14E --- --- --- 158 ---",
                "--- 14F 150 --- --- 159 ---",
                "--- 151 --- --- --- 15A ---",
                "--- 152 153 --- --- 15B 15C"
            },
            new[]
            {
                "--- --- 160 161 --- ---",
                "162 --- 163 164 --- 165",
                "166 167 168 169 16A 16B",
                "16C 16D 16E 16F 170 171",
                "--- 172 173 174 175 ---",
                "--- 176 177 178 179 ---",
                "--- --- 17A 17B --- ---"
            },
            new[]
            {
                "180 181 182 183 184 --- ---",
                "--- --- 185 186 187 188 ---",
                "189 18A 18B 18C 18D 18E 18F",
                "--- --- --- --- 190 191 192",
                "--- --- 193 194 195 196 ---",
                "--- 197 198 199 19A --- ---",
                "--- --- 19B 19C 19D --- ---",
                "--- --- --- 19E 19F 1A0 1A1"
            },
            new[]
            {
                "1B0 --- --- --- --- --- --- 1B1",
                "1B2 1B3 --- 1B4 1B5 --- 1B6 1B7",
                "1B8 1B9 1BA 1BB 1BC 1BD 1BE 1BF",
                "1C0 1C1 1C2 1C3 1C4 1C5 1C6 1C7",
                "--- 1C8 1C9 1CA 1CB 1CC 1CD ---",
                "--- 1CE 1CF --- --- 1D0 1D1 ---",
                "--- 1D2 1D3 1D4 1D5 1D6 1D7 ---"
            },
            new[]
            {
                "--- 211 212 --- --- 21F 220 ---",
                "213 214 215 216 --- 22B 22C ---",
                "217 218 219 21A --- 22D 22E ---",
                "21B 21C 21D 21E --- --- --- ---",
                "201 202 203 204 --- --- --- ---",
                "205 206 207 208 221 --- --- 224",
                "209 20A 20B 20C 225 226 227 228",
                "20D 20E 20F 210 --- 229 22A ---"
            },
            new[]
            {
                "--- --- --- 230 231 --- --- ---",
                "232 --- --- 234 235 --- --- 237",
                "238 239 23A 23B 23C 23D 23E 23F",
                "--- 240 241 242 243 244 245 ---",
                "--- 246 247 248 249 24A 24B ---",
                "24C 24D 24E 24F 250 251 252 253",
                "254 255 256 257 258 259 25A 25B",
                "25C --- --- 25D 25E --- --- 25F"
            }
        };

        public static bool TryCreate(
            bool isOverworld, bool isDungeon, bool isInterior,
            string dungeonName, string mapName,
            int mapOffsetX, int mapOffsetY, double linkX, double linkY,
            out MagpieTrackerLocation location)
        {
            var relativeX = (int)Math.Floor(linkX) - mapOffsetX * TileSize;
            var relativeY = (int)Math.Floor(linkY) - mapOffsetY * TileSize;
            var fieldX = FloorDivide(relativeX, FieldWidth);
            var fieldY = FloorDivide(relativeY, FieldHeight);
            var tileX = FloorDivide(PositiveModulo(relativeX, FieldWidth), TileSize);
            var tileY = FloorDivide(PositiveModulo(relativeY, FieldHeight), TileSize);

            if (isOverworld && fieldX is >= 0 and <= 15 && fieldY is >= 0 and <= 15)
            {
                location = new MagpieTrackerLocation($"0x{fieldY:X1}{fieldX:X1}", tileX, tileY, true);
                return true;
            }

            if (isDungeon && TryGetDungeonIndex(dungeonName, mapName, out var dungeonIndex))
            {
                if (TryGetDungeonRoom(dungeonIndex, fieldX, fieldY, out var room))
                {
                    location = new MagpieTrackerLocation(room, tileX, tileY, true);
                    return true;
                }

                if (TryGetFirstDungeonRoom(dungeonIndex, out room))
                {
                    location = new MagpieTrackerLocation(room, 4.5, 3.625, false);
                    return true;
                }
            }

            if (isInterior || isDungeon)
            {
                location = new MagpieTrackerLocation(UnknownInteriorRoom, 4.5, 3.625, false);
                return true;
            }

            location = default;
            return false;
        }

        private static bool TryGetDungeonIndex(string dungeonName, string mapName, out int index) =>
            TryGetDungeonIndex(dungeonName, out index) || TryGetDungeonIndex(mapName, out index);

        private static bool TryGetDungeonIndex(string value, out int index)
        {
            var normalized = (value ?? string.Empty).ToLowerInvariant()
                .Replace("_", string.Empty).Replace("-", string.Empty)
                .Replace(" ", string.Empty).Replace(".map", string.Empty);
            index = normalized switch
            {
                "dcolor" or "color" or "colordungeon" or "dungeoncolor" or "dungeon0" => 0,
                "one" or "d1" or "dungeon1" or "tailcave" => 1,
                "two" or "d2" or "dungeon2" or "bottlegrotto" => 2,
                "three" or "d3" or "dungeon3" or "keycavern" => 3,
                "four" or "d4" or "dungeon4" or "anglerstunnel" => 4,
                "five" or "d5" or "dungeon5" or "catfishsmaw" => 5,
                "six" or "d6" or "dungeon6" or "faceshrine" => 6,
                "seven" or "d7" or "dungeon7" or "eaglestower" => 7,
                "eight" or "d8" or "dungeon8" or "turtlerock" => 8,
                _ => -1
            };
            return index >= 0;
        }

        private static bool TryGetDungeonRoom(int dungeon, int x, int y, out string room)
        {
            room = null;
            if (dungeon < 0 || dungeon >= DungeonRoomRows.Length || y < 0 ||
                y >= DungeonRoomRows[dungeon].Length || x < 0)
                return false;

            var row = DungeonRoomRows[dungeon][y];
            var offset = x * 4;
            if (offset + 3 > row.Length)
                return false;

            var suffix = row.Substring(offset, 3);
            if (suffix == "---")
                return false;

            room = "0x" + suffix;
            return true;
        }

        private static bool TryGetFirstDungeonRoom(int dungeon, out string room)
        {
            for (var y = 0; y < DungeonRoomRows[dungeon].Length; y++)
            {
                var width = (DungeonRoomRows[dungeon][y].Length + 1) / 4;
                for (var x = 0; x < width; x++)
                    if (TryGetDungeonRoom(dungeon, x, y, out room))
                        return true;
            }

            room = null;
            return false;
        }

        private static int FloorDivide(int value, int divisor) =>
            value >= 0 ? value / divisor : (value - divisor + 1) / divisor;

        private static int PositiveModulo(int value, int modulus)
        {
            var remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
