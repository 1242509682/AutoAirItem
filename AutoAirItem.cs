using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AutoAirItem;

[ApiVersion(2, 1)]
public class AutoAirItem : TerrariaPlugin
{
    #region 插件信息
    public override string Name => "自动垃圾桶";
    public override string Author => "羽学";
    public override Version Version => new Version(1, 2, 5);
    public override string Description => "自动垃圾桶,只在SSC开启时有效";
    #endregion

    #region 全局变量
    internal static MyData Data = new();
    public static Database DB = new();
    internal static Configuration Config = new();
    #endregion

    #region 注册与释放
    public AutoAirItem(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();

        if (Config.SaveDatabase)
        {
            LoadAllPlayerData();
        }

        GeneralHooks.ReloadEvent += ReloadConfig;
        GetDataHandlers.PlayerSlot.Register(this.OnPlayerSlot);
        ServerApi.Hooks.NetGreetPlayer.Register(this, this.OnGreetPlayer);
        TShockAPI.Commands.ChatCommands.Add(new Command("AutoAir.use", Commands.AirCmd, "air", "垃圾"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.PlayerSlot.UnRegister(this.OnPlayerSlot);
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, this.OnGreetPlayer);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.AirCmd);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[自动垃圾桶]重新加载配置完毕。");
    }
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
    }
    #endregion

    #region 创建玩家数据方法
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr == null || !Config.Enabled)
        {
            return;
        }

        // 如果玩家不在数据表中，则创建新的数据条目
        if (!Data.Items.Any(item => item.Name == plr.Name))
        {
            Data.Items.Add(new MyData.PlayerData()
            {
                Name = plr.Name,
                Enabled = true,
                Mess = true,
                TrashList = new Dictionary<int, int> (),
            });
        }
    }
    #endregion

    #region 触发自动垃圾桶
    private Dictionary<string, DateTime> Cooldown = new();
    private void OnPlayerSlot(object? sender, GetDataHandlers.PlayerSlotEventArgs e)
    {
        var plr = e.Player;
        if (!Config.Enabled || e == null || plr == null || !plr.IsLoggedIn || !plr.Active || !plr.HasPermission("AutoAir.use")) return;

        var data = Data.Items.FirstOrDefault(x => x.Name == plr.Name);

        if (data == null) //如果没有获取到的玩家数据
        {
            if (TSPlayer.Server != plr)
            {
                Data.Items.Add(new MyData.PlayerData()
                {
                    Name = plr.Name,
                    Enabled = true,
                    Mess = true,
                    TrashList = new Dictionary<int, int> (),
                });
            }
            return;
        }

        if (!data.Enabled)
        {
            return;
        }

        //获取玩家垃圾桶格子
        var trash = plr.TPlayer.trashItem;

        //排除钱币 与 玩家自己指定排除物品
        if (Config.Exclude.Contains(trash.type) || data.ExcluItem.Contains(trash.type))
        {
            return;
        }

        //垃圾桶的物品不在 “物品表”里
        if (!data.TrashList.ContainsKey(trash.type))
        {
            if (trash.type != 0)
            {
                //添加垃圾桶的物品和对应格子数量 到 “自动垃圾桶物品表”
                data.TrashList.Add(trash.type, trash.stack);

                if (data.Mess)
                {
                    plr.SendMessage($"\n检测到首次将[i/s{1}:{trash.type}][c/A1D4C2:({trash.type})]放入垃圾桶", 240, 250, 150);
                    plr.SendMessage($"如果背包有相同物品,请等待{Config.FirstCoolingTime}秒:", 222, 250, 222);
                    plr.SendMessage($"手动点击[c/92C5EC:背包相同物品]或[c/92C5EC:物品栏排序]进行回收", 222, 250, 222);
                }

                DB.UpdateData(data); //更新玩家自己的数据库
                plr.TPlayer.trashItem.stack = 0;
                plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, PlayerItemSlotID.TrashItem);

                // 设置首次存储的冷却时间
                Cooldown[plr.Name] = DateTime.UtcNow.AddSeconds(Config.FirstCoolingTime);
                e.Handled = true;
            }
        }

        // 检查冷却是否结束（仅用于首次解决可能产生的BUG：当玩家拿着物品使用鼠标连点器，点击垃圾桶格子时:手上依旧有这个物品,就会被插件回收2倍物品数量）
        // 所以当检测到其他格子内还有相同物品:即便已经写入到了玩家的物品表里也会被不计数强制清除,根据自身服务器的网络延迟自行修改“首存冷却秒数”配置项
        if (Cooldown.TryGetValue(plr.Name, out var over) && over > DateTime.UtcNow)
        {
            var timer = (DateTime.UtcNow - over).TotalSeconds;
            if (data.TrashList.ContainsKey(e.Type))
            {
                if (data.Mess)
                {
                    plr.SendInfoMessage($"正在进行首次存储冷却中…剩余{timer:F2}秒");
                    plr.SendErrorMessage("警告:冷却时间内点击该物品,其数量不计入存储内!");
                }

                plr.TPlayer.inventory[e.Slot].stack = 0;
                plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, e.Slot);
                e.Handled = true;
            }
            return; // 冷却中，跳过回收逻辑
        }

        //如果格子里的物品ID在“自动垃圾桶物品表”里 
        if (data.TrashList.ContainsKey(e.Type))
        {
            if (data.Mess) //触发回馈信息
            {
                plr.SendMessage($"已从背包存入:{e.Stack}个[i/s{1}:{e.Type}]|[c/92C5EC:返还]: [c/A1D4C2:/air del {e.Type}]", 240, 250, 150);
            }

            //将该格子的物品数量 添加到“自动垃圾桶物品表”
            data.TrashList[e.Type] += e.Stack;
            plr.TPlayer.inventory[e.Slot].stack = 0;
            plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, e.Slot);
            DB.UpdateData(data); //更新玩家自己的数据库
            e.Handled = true;
        }
    }
    #endregion

    #region 加载所有玩家数据
    private void LoadAllPlayerData()
    {
        foreach (var data in DB.GetAllData())
        {
            Data.Items.Add(data);
        }
    }
    #endregion
}
