using System.Text;
using IL.ReLogic.Peripherals.RGB;
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
    public override Version Version => new Version(1, 1, 2);
    public override string Description => "涡轮增压不蒸鸭";
    #endregion

    #region 注册与释放
    public AutoAirItem(Main game) : base(game) { }
    internal static Configuration Config = new();
    internal static MyData data = new();
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += LoadConfig;
        ServerApi.Hooks.GameUpdate.Register(this, this.OnGameUpdate);
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        TShockAPI.Commands.ChatCommands.Add(new Command("AutoAir.use", Commands.AirCmd, "air", "垃圾"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= LoadConfig;
            ServerApi.Hooks.GameUpdate.Deregister(this, this.OnGameUpdate);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.AirCmd);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    private static void LoadConfig(ReloadEventArgs args = null!)
    {
        Config = Configuration.Read();
        Config.Write();
        TShock.Log.ConsoleInfo("[自动垃圾桶]重新加载配置完毕。");
    }
    #endregion

    #region 玩家更新配置方法（计入记录时间并创建配置结构）
    private static int ClearCount = 0; //需要清理的玩家计数
    private void OnJoin(JoinEventArgs args)
    {
        if (args == null || !Config.Open)
        {
            return;
        }

        var plr = TShock.Players[args.Who];

        if (plr == null)
        {
            return;
        }

        // 查找玩家数据
        var list = data.Items.FirstOrDefault(x => x.Name == plr.Name);

        // 如果玩家不在数据表中，则创建新的数据条目
        if (list == null || plr.Name != list.Name)
        {
            data.Items.Add(new MyData.ItemData()
            {
                Name = plr.Name,
                Enabled = true,
                IsActive = true,
                Auto = true,
                LogTime = DateTime.Now,
                Mess = true,
                ItemName = new List<string>()
            });
        }
        else
        {
            // 更新玩家的登录时间和活跃状态
            list.LogTime = DateTime.Now;
            list.IsActive = true;
        }

        //清理数据方法
        if (Config.ClearData && list != null)
        {
            // 获取当前在线玩家的名字列表
            var OnlinePlayers = Enumerable.Range(0, TShock.Players.Length).Select(i => TShock.Players[i])
                .Where(p => p != null && p.Active && p.Name != null).Select(p => p.Name).ToList();

            // 遍历数据列表，将不在当前在线名单中的玩家设置为非活跃
            foreach (var item in data.Items)
            {
                if (item != null && !OnlinePlayers.Contains(item.Name))
                {
                    item.IsActive = false;
                }
            }

            var Remove = data.Items.Where(list => list != null && list.LogTime != default &&
            (DateTime.Now - list.LogTime).TotalHours >= Config.timer).ToList();

            //数据清理的播报内容
            var mess = new StringBuilder();
            mess.AppendLine($"[i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:垃][c/E5A894:圾][c/E5BE94:桶][i:3454]");
            mess.AppendLine($"以下玩家离线时间 与 [c/ABD6C7:{plr.Name}] 加入时间\n【[c/A1D4C2:{DateTime.Now}]】\n" +
                $"超过 [c/E17D8C:{Config.timer}] 小时 已清理 [c/76D5B4:自动垃圾桶] 数据：");

            foreach (var plr2 in Remove)
            {
                //只显示小时数 F0整数 F1保留1位小数 F2保留2位 如：24.01小时
                var hours = (DateTime.Now - plr2.LogTime).TotalHours;
                FormattableString Hours = $"{hours:F0}";

                //更新时间超过Config预设的时间，并该玩家更新状态为false则添加计数并移除数据
                if (hours >= Config.timer && !plr2.IsActive)
                {
                    ClearCount++;
                    mess.AppendFormat("[c/A7DDF0:{0}]:[c/74F3C9:{1}小时], ", plr2.Name, Hours);
                    data.Items.Remove(plr2);
                }
            }

            //确保有一个玩家计数，只播报一次
            if (ClearCount > 0 && mess.Length > 0)
            {
                //广告开关
                if (Config.Enabled)
                {
                    //自定义广告内容
                    mess.AppendLine(Config.Advertisement);
                }

                TShock.Utils.Broadcast(mess.ToString(), 247, 244, 150);
                ClearCount = 0;
            }
        }
    }
    #endregion

    #region 玩家离开服务器更新记录时间
    private void OnLeave(LeaveEventArgs args)
    {
        if (args == null || !Config.Open)
        {
            return;
        }

        var plr = TShock.Players[args.Who];
        var list = data.Items.FirstOrDefault(x => x != null && x.Name == plr.Name);
        if (plr == null || list == null) 
        { 
            return; 
        }

        if (Config.ClearData)
        {
            //离开服务器更新记录时间与活跃状态
            if (!plr.Active && plr.Name == list.Name )
            {
                if (list.IsActive)
                {
                    list.LogTime = DateTime.Now;
                    list.IsActive = false;
                }
            }
        }
    }
    #endregion

    #region 触发自动垃圾桶
    public static long Timer = 0L;
    private void OnGameUpdate(EventArgs args)
    {
        Timer++;

        if (!Config.Open)
        {
            return;
        }

        foreach (var plr in TShock.Players.Where(plr => plr != null && plr.Active && plr.IsLoggedIn))
        {
            var list = data.Items.FirstOrDefault(x => x.Name == plr.Name);
            if (list != null && list.Enabled && Timer % Config.UpdateRate == 0)
            {
                AutoAirItems(plr, list.ItemName, list.Auto, list.Mess);
            }
        }
    }
    #endregion

    #region 自动清理物品方法
    public static bool AutoAirItems(TSPlayer player, List<string> List, bool Auto, bool mess)
    {
        var plr = player.TPlayer;

        for (int i = 0; i < plr.inventory.Length; i++)
        {
            var item = plr.inventory[i];
            var id = TShock.Utils.GetItemById(item.type).netID;

            if (Auto)
            {
                if (!plr.trashItem.IsAir && !List.Contains(plr.trashItem.Name))
                {
                    List.Add(plr.trashItem.Name);
                    player.SendMessage($"已将 '[c/92C5EC:{plr.trashItem.Name}]'添加到自动垃圾桶|指令菜单:[c/A1D4C2:/air]", 255, 246, 158);
                }
            }

            if (item != null && List.Contains(item.Name) && item.Name != plr.inventory[plr.selectedItem].Name)
            {
                item.TurnToAir();
                player.SendData(PacketTypes.PlayerSlot, null, player.Index, PlayerItemSlotID.Inventory0 + i);

                if (mess)
                {
                    var itemName = Lang.GetItemNameValue(id);
                    player.SendMessage($"【自动垃圾桶】已将 '[c/92C5EC:{itemName}]'从您的背包中移除", 255, 246, 158);
                }
                return true;
            }
        }
        return false;
    }
    #endregion
}
