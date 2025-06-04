using Newtonsoft.Json;
using TShockAPI;

namespace AutoAirItem;

internal class Configuration
{
    #region 实例变量
    [JsonProperty("插件指令权限", Order = -16)]
    public string Text { get; set; } = "指令菜单：/air 或 /垃圾，权限名【AutoAir.use】，给玩家权限：/group addperm default AutoAir.use";

    [JsonProperty("插件开关", Order = -15)]
    public bool Enabled { get; set; } = true;

    [JsonProperty("保存数据", Order = -14)]
    public bool SaveDatabase { get; set; } = true;

    [JsonProperty("冷却秒数", Order = -13)]
    public float FirstCoolingTime { get; set; } = 0.5f;

    [JsonProperty("一行显示多少个物品", Order = -12)]
    public int ListLine { get; set; } = 7;

    [JsonProperty("排除垃圾表", Order = -11)]
    public int[] Exclude = new int[] { 71,72,73,74 };
    #endregion

    #region 读取与创建配置文件方法
    public static readonly string FilePath = Path.Combine(TShock.SavePath, "自动垃圾桶.json");

    public void Write()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }

    public static Configuration Read()
    {
        if (!File.Exists(FilePath))
        {
            var NewConfig = new Configuration();
            new Configuration().Write();
            return NewConfig;
        }
        else
        {
            var jsonContent = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
        }
    }
    #endregion

}