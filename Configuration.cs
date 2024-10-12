using Newtonsoft.Json;
using TShockAPI;

namespace AutoAirItem;

    internal class Configuration
    {
        #region 实例变量
        [JsonProperty("插件指令权限", Order = -1)]
        public string Text { get; set; } = "指令菜单：/air 或 /垃圾，权限名【AutoAir.use】，给玩家权限：/group addperm default AutoAir.use";

        [JsonProperty("插件开关", Order = 0)]
        public bool Open { get; set; } = true;

        [JsonProperty("自动清理提示", Order = 0)]
        public bool Mess { get; set; } = true;

        [JsonProperty("清理垃圾速度", Order = 1)]
        public int UpdateRate = 60;

        [JsonProperty("玩家数据表", Order = 2)]
        public List<ItemData> Items { get; set; } = new List<ItemData>();
        #endregion

        #region 预设参数方法
        public void Ints()
        {
            Items = new List<ItemData>
            {
                new ItemData("羽学",false,default,new List<string>()),
            };
        }
        #endregion

        #region 数据结构
        public class ItemData
        {
            [JsonProperty("玩家名字", Order = 1)]
            public string Name { get; set; }
            [JsonProperty("垃圾桶开关", Order = 2)]
            public bool Enabled { get; set; } = false;
            [JsonProperty("登录时间", Order = 3)]
            public DateTime LoginTime { get; set; }
            [JsonProperty("垃圾桶物品", Order = 5)]
            public List<string> ItemName { get; set; } = new List<string>();

            public ItemData(string name = "", bool enabled = false, DateTime time = default,List<string> item = null!)
            {
                Name = name ?? "";
                Enabled = enabled;
                LoginTime = time;
                ItemName = item;
            }
        }
        #endregion

        #region 读取与创建配置文件方法
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "自动垃圾桶.json");

        public void Write()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        public static Configuration Read()
        {
            if (!File.Exists(FilePath))
            {
                var NewConfig = new Configuration();
                NewConfig.Ints();
                new Configuration().Write();
                return NewConfig;
            }
            else
            {
                string jsonContent = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
            }
        }
        #endregion

    }