using Newtonsoft.Json;
using TShockAPI;

namespace AutoAirItem;

internal class Configuration
{
    #region 实例变量
    [JsonProperty("插件指令权限", Order = -16)]
    public string Text { get; set; } = "指令菜单：/air 或 /垃圾，权限名【AutoAir.use】，给玩家权限：/group addperm default AutoAir.use";

    [JsonProperty("使用说明", Order = -15)]
    public string Text2 { get; set; } = "玩家每次进出服都会更新【记录时间】，玩家A离线时间与玩家B登录时间相差超过【清理周期】所设定的时间，则自动清理该玩家A的数据";

    [JsonProperty("插件开关", Order = -14)]
    public bool Open { get; set; } = true;

    [JsonProperty("清理数据周期/小时", Order = 1)]
    public long timer { get; set; } = 24;

    [JsonProperty("自动清理提示", Order = 2)]
    public bool Mess { get; set; } = true;

    [JsonProperty("清理垃圾速度", Order = 3)]
    public int UpdateRate = 60;

    [JsonProperty("玩家数据表", Order = 4)]
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
        [JsonProperty("记录时间", Order = 2)]
        public DateTime LoginTime { get; set; }
        [JsonProperty("垃圾桶开关", Order = 3)]
        public bool Enabled { get; set; } = false;
        [JsonProperty("垃圾桶物品", Order = 4)]
        public List<string> ItemName { get; set; } = new List<string>();

        public ItemData(string name = "", bool enabled = false, DateTime time = default, List<string> item = null!)
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