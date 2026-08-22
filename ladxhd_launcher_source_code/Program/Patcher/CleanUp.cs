using System.IO;

namespace LADXHD_Launcher
{
    internal class CleanUp
    {
        private static readonly string[] miniMapData =
        {
            "three_1.txt",
            "three_2.txt",
            "three_3.txt"
        };

       private static readonly string[] removeMaps =
        {
            "cave bird.map.data",
            "dungeon 7_2d.map.data",
            "dungeon_end.map.data",
            "dungeon3_1.map",
            "dungeon3_1.map.data",
            "dungeon3_2.map",
            "dungeon3_2.map.data",
            "dungeon3_3.map",
            "dungeon3_3.map.data",
            "dungeon3_4.map",
            "dungeon3_4.map.data"
        };

        private static readonly string[] removeMapPatterns =
        {
            "0 test map*"
        };

        public static void RemoveJunkMapFiles()
        {
            string mapsPath = Path.Combine(Config.DataPath, "Maps");

            foreach (string file in miniMapData)
                Path.Combine(Config.DataPath, "Dungeon", file).RemovePath();

            foreach (string file in removeMaps)
                Path.Combine(mapsPath, file).RemovePath();

            foreach (string pattern in removeMapPatterns)
                foreach (string file in mapsPath.GetFiles(pattern))
                    file.RemovePath();
        }
    }
}
