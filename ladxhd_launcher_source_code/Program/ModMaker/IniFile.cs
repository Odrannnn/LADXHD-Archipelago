using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LADXHD_Launcher
{
    internal class IniFile
    {
        string Path;

        public IniFile(string iniPath = null)
        {
            this.Path = iniPath ?? "";
        }

        private Dictionary<string, Dictionary<string, string>> ReadAll()
        {
            var data = new Dictionary<string, Dictionary<string, string>>(System.StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(Path)) return data;

            string currentSection = "";
            foreach (string raw in File.ReadAllLines(Path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.StartsWith(";") || line.StartsWith("#") || line == "") continue;
                if (line.StartsWith("[") && line.Contains("]"))
                {
                    currentSection = line.Substring(1, line.IndexOf(']') - 1).Trim();
                    if (!data.ContainsKey(currentSection))
                        data[currentSection] = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
                }
                else if (line.Contains("="))
                {
                    int eq = line.IndexOf('=');
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    foreach (char c in new[] { ';', '#' })
                        if (val.Contains(c))
                            val = val.Substring(0, val.IndexOf(c)).Trim();
                    if (!data.ContainsKey(currentSection))
                        data[currentSection] = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
                    data[currentSection][key] = val;
                }
            }
            return data;
        }

        private void WriteAll(Dictionary<string, Dictionary<string, string>> data)
        {
            var sb = new StringBuilder();
            foreach (var section in data)
            {
                sb.AppendLine($"[{section.Key}]");
                foreach (var kvp in section.Value)
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                sb.AppendLine();
            }
            File.WriteAllText(Path, sb.ToString(), Encoding.UTF8);
        }

        public string Read(string Key, string Section = "")
        {
            var data = ReadAll();
            if (data.TryGetValue(Section, out var section))
                if (section.TryGetValue(Key, out var val))
                    return val;
            return "";
        }

        public void Write(string Key, string Value, string Section = "")
        {
            var data = ReadAll();
            if (!data.ContainsKey(Section))
                data[Section] = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            if (Value == null)
                data[Section].Remove(Key);
            else
                data[Section][Key] = Value;
            WriteAll(data);
        }

        public void DeleteKey(string Key, string Section = "")
        {
            Write(Key, null, Section);
        }

        public void ConditionalWriteDelete(string Key, string Value, string Section = "", bool DoWrite = true)
        {
            if (DoWrite) Write(Key, Value, Section);
            else DeleteKey(Key, Section);
        }

        public void DeleteSection(string Section = "")
        {
            var data = ReadAll();
            data.Remove(Section);
            WriteAll(data);
        }

        public bool KeyExists(string Key, string Section = "")
        {
            return Read(Key, Section).Length > 0;
        }

        public List<string> GetSections()
        {
            return new List<string>(ReadAll().Keys);
        }

        public List<string> GetSectionKeys(string Section)
        {
            var data = ReadAll();
            if (data.TryGetValue(Section, out var section))
                return new List<string>(section.Keys);
            return new List<string>();
        }
    }
}