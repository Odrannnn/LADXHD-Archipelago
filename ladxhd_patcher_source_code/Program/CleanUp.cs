using System.IO;
using static LADXHD_Patcher.Config;

namespace LADXHD_Patcher
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
            // Set the path to "Data" based on platform selected.
            string dataPath = Config.SelectedPlatform == Platform.Android
                ? dataPath = Path.Combine(Config.TempFolder, "android", "com.zelda.ladxhd", "assets", "Data")
                : Path.Combine(Config.BaseFolder, "Data");

            string mapsPath = Path.Combine(dataPath, "Maps");

            foreach (string file in miniMapData)
                Path.Combine(dataPath, "Dungeon", file).RemovePath();

            foreach (string file in removeMaps)
                Path.Combine(mapsPath, file).RemovePath();

            foreach (string pattern in removeMapPatterns)
                foreach (string file in mapsPath.GetFiles(pattern))
                    file.RemovePath();
        }
    }
}
