namespace LADXHD_Launcher
{
    internal class LADXHD_IniFile
    {
        public static string Path;
        public static IniFile Class;

        public static void Initialize(string IniPath)
        {
            LADXHD_IniFile.Path = IniPath;
            LADXHD_IniFile.Class = new IniFile(LADXHD_IniFile.Path);
        }

        public static void WriteINIValues()
        {
            LADXHD_IniFile.Class.Write("ModName", Config.ModName, "LAHDMOD");
            LADXHD_IniFile.Class.Write("Description", Config.Description, "LAHDMOD");
        }

        public static void LoadINIValues()
        {
            Config.ModName = LADXHD_IniFile.Class.Read("ModName", "LAHDMOD");
            Config.Description = LADXHD_IniFile.Class.Read("Description", "LAHDMOD");
        }
    }
}
