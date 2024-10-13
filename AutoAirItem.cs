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
    public override Version Version => new Version(1, 0, 2);
    public override string Description => "涡轮增压不蒸鸭";
    #endregion

    #region 注册与释放
    public AutoAirItem(Main game) : base(game) { }
    private GeneralHooks.ReloadEventD _reloadHandler;
    public override void Initialize()
    {
        LoadConfig();
        this._reloadHandler = (_) => LoadConfig();
        GeneralHooks.ReloadEvent += this._reloadHandler;
        ServerApi.Hooks.GameUpdate.Register(this, this.OnGameUpdate);
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        TShockAPI.Commands.ChatCommands.Add(new Command("AutoAir.use", Commands.AirCmd, "air", "垃圾"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= this._reloadHandler;
            ServerApi.Hooks.GameUpdate.Deregister(this, this.OnGameUpdate);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.AirCmd);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new();
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
        TShock.Log.ConsoleInfo("[自动垃圾桶]重新加载配置完毕。");
    }
    #endregion

    #region 玩家更新配置方法（计入记录时间并创建配置结构）
    private void OnJoin(JoinEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        var list = Config.Items.FirstOrDefault(x => x.Name == plr.Name);

        if (!Config.Open) return;
        if (!Config.Items.Any(item => item.Name == plr.Name))
        {
            Config.Items.Add(new Configuration.ItemData()
            {
                Name = plr.Name,
                Enabled = false,
                TrashItem = false,
                LoginTime = DateTime.Now,
                Mess = true,
                ItemName = new List<string>()
            });
        }
        else
        {
            list!.LoginTime = DateTime.Now;
        }
        Config.Write();
    }
    #endregion

    #region 玩家离开服务器更新记录时间，横比所有玩家的记录时间，如超过清理周期，则自动删除玩家数据
    private void OnLeave(LeaveEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        var list = Config.Items.FirstOrDefault(x => x.Name == plr.Name);
        if (!Config.Open) return;
        if (Config.Items.Any(item => item.Name == plr.Name))
        {
            list!.LoginTime = DateTime.Now;
        }

        var Remove = Config.Items.Where(list =>
            list.LoginTime != default && (DateTime.Now - list.LoginTime).TotalHours >= Config.timer).ToList();

        foreach (var plr2 in Remove)
        {
            Config.Items.Remove(plr2);
        }

        Config.Write();
    }
    #endregion

    #region 触发自动垃圾桶
    public static long Timer = 0L;
    private void OnGameUpdate(EventArgs args)
    {
        Timer++;

        if (!Config.Open) return;

        foreach (var plr in TShock.Players.Where(plr => plr != null && plr.Active && plr.IsLoggedIn))
        {
            var list = Config.Items.FirstOrDefault(x => x.Name == plr.Name);
            if (list != null && list.Enabled && Timer % Config.UpdateRate == 0)
            {
                AutoAirItems(plr, list.ItemName, list.TrashItem,list.Mess);
            }
        }
    }
    #endregion

    #region 自动清理物品方法
    public static bool AutoAirItems(TSPlayer plr, List<string> List, bool trash,bool mess)
    {
        Player player = plr.TPlayer;

        for (int i = 0; i < player.inventory.Length; i++)
        {
            var item = player.inventory[i];
            var id = TShock.Utils.GetItemById(item.type).netID;
            var trashItem = player.trashItem;

            if (trash)
            {
                if (!trashItem.IsAir && !List.Contains(trashItem.Name))
                {
                    List.Add(trashItem.Name);
                    Config.Write();
                    plr.SendMessage($"已将 '[c/92C5EC:{trashItem.Name}]'添加到自动垃圾桶表", 255, 246, 158);
                }
            }

            if (item != null && List.Contains(item.Name) && item.Name != player.inventory[player.selectedItem].Name)
            {
                item.TurnToAir();
                plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, PlayerItemSlotID.Inventory0 + i);

                if (mess)
                {
                    var itemName = Lang.GetItemNameValue(id);
                    plr.SendMessage($"【自动垃圾桶】已将 '[c/92C5EC:{itemName}]'从您的背包中移除", 255, 246, 158);
                }
                return true;
            }
        }
        return false;
    }
    #endregion
}
