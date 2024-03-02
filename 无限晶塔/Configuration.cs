using Newtonsoft.Json;
using System;
using System.IO;
using TShockAPI;

namespace AdditionalPylons
{
    internal class Configuration
    {
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "无限晶塔.json");
        public int 丛林晶塔数量上限 = 2;
        public int 森林晶塔数量上限 = 2;
        public int 神圣晶塔数量上限 = 2;
        public int 洞穴晶塔数量上限 = 2;
        public int 海洋晶塔数量上限 = 2;
        public int 沙漠晶塔数量上限 = 2;
        public int 雪原晶塔数量上限 = 2;
        public int 蘑菇晶塔数量上限 = 2;
        public int 万能晶塔数量上限 = 2;


        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                var str = JsonConvert.SerializeObject(this, Formatting.Indented);
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(str);
                }
            }
        }

        public static Configuration Read(string path)
        {
            if (!File.Exists(path))
                return new Configuration();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    var cf = JsonConvert.DeserializeObject<Configuration>(sr.ReadToEnd());
                    return cf;
                }
            }
        }
    }
}