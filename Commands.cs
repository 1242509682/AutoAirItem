using Terraria;
using TShockAPI;
using static AutoAirItem.AutoAirItem;

namespace AutoAirItem;

public class Commands
{
    #region 主体指令
    public static void AirCmd(CommandArgs args)
    {
        var plr = args.Player;
        var data = Data.Items.FirstOrDefault(x => x.Name == plr.Name);

        if (!AutoAirItem.Config.Enabled)
        {
            return;
        }

        if (args.Parameters.Count == 0)
        {
            HelpCmd(args.Player, data);
            return;
        }

        if (args.Parameters.Count == 1)
        {
            if (data != null)
            {
                switch (args.Parameters[0].ToLower())
                {
                    case "h":
                    case "help":
                    case "menu":
                        {
                            HelpCmd(plr, data);
                            break;
                        }

                    case "on":
                    case "off":
                        {
                            if (data.Enabled)
                            {
                                plr.SendSuccessMessage($"玩家 [{plr.Name}] 已[c/92C5EC:关闭]自动垃圾桶功能。");
                                data.Enabled = false;
                            }
                            else
                            {
                                plr.SendSuccessMessage($"玩家 [{plr.Name}] 已[c/92C5EC:启用]自动垃圾桶功能。");
                                data.Enabled = true;
                            }

                            DB.UpdateData(data); // 更新数据库
                            break;
                        }

                    case "+":
                    case "add":
                        {
                            var sel = plr.SelectedItem;
                            if (plr.SelectedItem == null || plr.SelectedItem.type <= 0 || plr.SelectedItem.IsAir)
                            {
                                plr.SendErrorMessage("你手上没有物品！");
                                return;
                            }

                            if (data.ExcluItem.Contains(sel.type))
                            {
                                // 如果已在排除列表中，则移除
                                data.ExcluItem.Remove(sel.type);
                                plr.SendSuccessMessage($"已将物品 [i/s1:{sel.type}] 从排除列表中移除。");
                            }
                            else
                            {
                                // 如果不在排除列表中，则添加
                                data.ExcluItem.Add(sel.type);
                                plr.SendSuccessMessage($"已将物品 [i/s1:{sel.type}] 添加到排除列表中。");
                            }

                            DB.UpdateData(data);
                            break;
                        }

                    case "l":
                    case "list":
                        {
                            // 初始化一个空物品，防止 maxStack == 0
                            var item = new Item();
                            item.SetDefaults(0);
                            var trash = data.TrashList;
                            if (trash.Count == 0)
                            {
                                plr.SendErrorMessage($"[{data.Name}的垃圾桶] 中没有任何物品。");
                                return;
                            }

                            var icons = new List<string>();
                            foreach (var t in trash.OrderBy(k => k.Key))
                            {
                                // 获取该物品的最大堆叠数量
                                Item item2 = new Item();
                                item2.SetDefaults(t.Key);
                                int myStack = item2.maxStack > 0 ? item2.maxStack : item.maxStack;

                                for (int i = 0; i < t.Value / myStack; i++)
                                {
                                    icons.Add($"[i/s{myStack}:{t.Key}]");
                                }

                                if (t.Value % myStack > 0)
                                {
                                    icons.Add($"[i/s{t.Value % myStack}:{t.Key}]");
                                }
                            }

                            // 自动垃圾桶每行显示最多 7 个图标
                            var chunks = icons
                                .Select((icon, index) => new { icon, index })
                                .GroupBy(x => x.index / Config.ListLine)
                                .Select(g => string.Join("  ", g.Select(x => x.icon)));

                            var text = string.Join("\n", chunks);

                            plr.SendInfoMessage($"[{data.Name}的垃圾桶]\n" + text);

                            if (data.ExcluItem != null && data.ExcluItem.Any())
                            {
                                // 排除表每行显示最多 7 个图标
                                var chunks2 = data.ExcluItem
                                    .OrderBy(id => id) // 可选：按ID排序
                                    .Select((id, index) => new { id, index })
                                    .GroupBy(x => x.index / Config.ListLine)
                                    .Select(g => g.Select(x => $"[i/s1:{x.id}]").ToList())
                                    .ToList();

                                var lines = chunks2.Select(group => string.Join(" ", group));
                                string message = $"[{data.Name}的排除表]\n" + string.Join("\n", lines);
                                plr.SendInfoMessage(message);
                            }
                            break;
                        }

                    case "c":
                    case "clear":
                        {
                            data.TrashList.Clear();
                            data.ExcluItem.Clear();
                            plr.SendSuccessMessage($"已清理[c/92C5EC: {plr.Name} ]的自动垃圾桶表");
                            DB.UpdateData(data); // 更新数据库
                            break;
                        }

                    case "m":
                    case "mess":
                        {
                            if (data.Mess)
                            {
                                plr.SendSuccessMessage($"玩家 [{plr.Name}] 已[c/92C5EC:关闭]垃圾桶提示功能。");
                                data.Mess = false;
                            }
                            else
                            {
                                plr.SendSuccessMessage($"玩家 [{plr.Name}] 已[c/92C5EC:开启]垃圾桶提示功能。");
                                data.Mess = true;
                            }
                            DB.UpdateData(data); // 更新数据库
                            break;
                        }

                    default:
                        {
                            HelpCmd(plr, data);
                            break;
                        }
                }
            }

            if (args.Parameters[0].ToLower() == "rs" || args.Parameters[0].ToLower() == "reset")
            {
                Reset(args);
                return;
            }
        }

        if (args.Parameters.Count >= 2)
        {
            switch (args.Parameters[0].ToLower())
            {
                case "-":
                case "d":
                case "del":
                case "delete":
                case "remove":
                    {
                        HandleTrashRemoveCommand(plr, args, data);
                        break;
                    }

                case "ck":
                case "check":
                    {
                        if (int.TryParse(args.Parameters[1], out var num))
                        {
                            if (plr != TSServerPlayer.Server)
                            {
                                CheckCmd(args, num);
                            }
                            else
                            {
                                CheckCmd2(args, num);
                            }
                        }
                        else
                        {
                            plr.SendInfoMessage("正确格式为:/air ck 你要筛选的物品数量");
                        }
                        break;
                    }

                default:
                    {
                        HelpCmd(args.Player, data);
                        break;
                    }
            }
        }
    }
    #endregion

    #region 菜单方法
    private static void HelpCmd(TSPlayer plr, MyData.PlayerData? data)
    {
        if (plr == null)
        {
            return;
        }

        if (plr.HasPermission("AutoAir.admin"))
        {
            if (plr != TSPlayer.Server)
            {
                if (data != null)
                {
                    plr.SendInfoMessage("【自动垃圾桶】指令菜单 [i:3456][C/F2F2C7:插件开发] [C/BFDFEA:by] [c/00FFFF:羽学][i:3459]\n" +
                    "/air on —— 开启|关闭[c/89DF85:垃圾桶]功能\n" +
                    "/air l —— [c/F19092:列出]自己的[c/F2F191:垃圾桶]\n" +
                    "/air c —— [c/85CEDF:清理]垃圾桶\n" +
                    "/air m —— 开启|关闭[c/F2F292:清理消息]\n" +
                    "/air ck 数量—— 筛选出物品超过此数量的玩家\n" +
                    "/air a —— 将手上物品[c/F19092:排除]或[c/F2F292:还原]到检测\n" +
                    "/air d 名字 —— 从自动垃圾桶[c/F19092:全部取出]\n" +
                    "/air d 名字 数量 —— 从垃圾桶[c/F19092:取出指定数量]\n" +
                    "/air rs —— 清空[c/85CFDE:所有玩家]数据", 193, 223, 186);
                    if (!data.Enabled)
                    {
                        plr.SendSuccessMessage($"请输入该指令开启→: [c/92C5EC:/air on] ");
                    }
                }
            }
            else
            {
                plr.SendInfoMessage("【自动垃圾桶】指令菜单\n" +
                    "/air ck 数量—— 筛选出物品超过此数量的玩家\n" +
                    "/air rs —— 清空[c/85CFDE:所有玩家]数据", 193, 223, 186);
                plr.SendSuccessMessage($"其余指令需要您进入游戏内才会显示");
            }
        }
        else
        {
            if (data != null)
            {
                plr.SendInfoMessage("【自动垃圾桶】指令菜单 [i:3456][C/F2F2C7:插件开发] [C/BFDFEA:by] [c/00FFFF:羽学][i:3459]\n" +
                    "/air on —— 开启|关闭[c/89DF85:垃圾桶]功能\n" +
                    "/air l —— [c/F19092:列出]自己的[c/F2F191:垃圾桶]\n" +
                    "/air a —— 将手上物品[c/F19092:排除]或[c/F2F292:还原]到检测\n" +
                    "/air d 名字 —— 从自动垃圾桶[c/F19092:全部取出]\n" +
                    "/air d 名字 数量 —— 从垃圾桶[c/F19092:取出指定数量]\n" +
                    "/air c —— [c/85CEDF:清理]垃圾桶\n" +
                    "/air ck 数量 —— 筛选出物品超过此数量的玩家\n" +
                    "/air m —— 开启|关闭[c/F2F292:清理消息]", 193, 223, 186);

                if (!data.Enabled)
                {
                    plr.SendSuccessMessage($"请输入该指令开启→: [c/92C5EC:/air on] ");
                }
            }
        }
    }
    #endregion

    #region 重置数据方法
    public static void Reset(CommandArgs args)
    {
        if (!args.Player.HasPermission("AutoAir.admin"))
        {
            args.Player.SendErrorMessage("你没有权限使用此指令！");
            return;
        }
        else
        {
            Data.Items.Clear(); // 清空内存数据
            DB.ClearData();
            args.Player.SendSuccessMessage($"已[c/92C5EC:清空]所有玩家《自动垃圾桶》数据！");
        }
    }
    #endregion

    #region 检查物品数量方法（玩家版）
    public static void CheckCmd(CommandArgs args, int num)
    {
        var plr = args.Player;
        var index = 1;

        // 初始化空物品获取最大堆叠值
        var item2 = new Item();
        item2.SetDefaults(0);

        plr.SendInfoMessage($"以下垃圾物品数量超过【[c/92C5EC:{num}]】的玩家");
        var ItemList = new List<(int Index, string Name, List<string> Icons)>(); // 把 TrashList 改为 Icons 字符串列表

        var db = AutoAirItem.DB.GetAll(); // 调用数据库查询方法
        foreach (var data in db)
        {
            var findStart = data.TrashList.Where(pair => pair.Value >= num).ToList();

            if (!findStart.Any()) continue;

            // 拆分为多个图标
            var icons = new List<string>();
            foreach (var t in findStart)
            {
                // 获取该物品的最大堆叠（兜底使用全局默认）
                Item item = new Item();
                item.SetDefaults(t.Key);
                int myStack = item.maxStack > 0 ? item.maxStack : item2.maxStack;

                for (int i = 0; i < t.Value / myStack; i++)
                    icons.Add($"[i/s{myStack}:{t.Key}]");

                if (t.Value % myStack > 0)
                    icons.Add($"[i/s{t.Value % myStack}:{t.Key}]");
            }

            if (icons.Count > 0)
            {
                ItemList.Add((index++, data.Name, icons));
            }
        }

        if (ItemList.Count > 0)
        {
            foreach (var p in ItemList)
            {
                // 每7个图标换行
                var chunks = p.Icons
                    .Select((icon, idx) => new { icon, idx })
                    .GroupBy(x => x.idx / Config.ListLine)
                    .Select(g => string.Join("  ", g.Select(x => x.icon)));

                var mess = $"\n[c/32CEB7:{p.Index}.][c/F3E83B:{p.Name}:]\n" + string.Join("\n", chunks);
                plr.SendMessage(mess, 193, 223, 186);
            }
        }
        else
        {
            plr.SendMessage($"没有找到垃圾数量超过[c/92C5EC:{num}]的玩家", 193, 223, 186);
        }
    }


    #endregion

    #region 检查物品数量方法 (控制台优化版)
    public static void CheckCmd2(CommandArgs args, int num)
    {
        var plr = args.Player;
        var index = 1;

        plr.SendInfoMessage($"以下垃圾物品数量超过【{num}】的玩家");
        var ItemList = new List<(int Index, string Name, List<string> TextEntries)>(); // 改用文本条目

        var db = AutoAirItem.DB.GetAll();
        foreach (var data in db)
        {
            var findStart = data.TrashList.Where(pair => pair.Value >= num).ToList();
            if (!findStart.Any()) continue;

            //物品名称列表
            var nameList = new List<string>();
            foreach (var t in findStart)
            {
                // 获取物品名称
                var name = Lang.GetItemNameValue(t.Key);
                nameList.Add($"{name}({t.Value})");
            }

            if (nameList.Count > 0)
            {
                ItemList.Add((index++, data.Name, nameList));
            }
        }

        if (ItemList.Count > 0)
        {
            foreach (var p in ItemList)
            {
                // 每7个物品一行
                var chunks = p.TextEntries
                    .Select((item, idx) => new { item, idx })
                    .GroupBy(x => x.idx / Config.ListLine)
                    .Select(g => string.Join("  ", g.Select(x => x.item)));

                var mess = $"\n[c/32CEB7:{p.Index}.][c/F3E83B:{p.Name}:]\n" + string.Join("\n", chunks);
                plr.SendMessage(mess, 250, 240, 150);
            }
        }
        else
        {
            plr.SendInfoMessage($"没有找到垃圾数量超过{num}的玩家");
        }
    }
    #endregion

    #region 返还物品时显示图标，并将该物品自动加入排除表逻辑
    private static void HandleTrashRemoveCommand(TSPlayer plr, CommandArgs args, MyData.PlayerData? data)
    {
        if (data == null) return;

        // 参数解析
        string itemName = args.Parameters[1];
        int howMuch = -1; // -1 表示全部取出

        if (args.Parameters.Count == 3 && int.TryParse(args.Parameters[2], out int amount))
        {
            howMuch = amount;
        }

        // 获取物品
        var items = TShock.Utils.GetItemByIdOrName(itemName);
        if (items.Count == 0)
        {
            plr.SendErrorMessage("找不到该物品，请输入正确的物品名或ID。");
            return;
        }

        var item = items[0];
        int itemType = item.type;
        int maxStack = item.maxStack > 0 ? item.maxStack : 1;

        // 检查垃圾桶是否有这个物品
        if (!data.TrashList.TryGetValue(itemType, out int total) || total <= 0)
        {
            plr.SendErrorMessage($"物品 [i/s1:{itemType}] 不在垃圾桶中！");
            return;
        }

        // 确定实际取出数量
        int toTake = howMuch == -1 ? total : Math.Min(howMuch, total);

        // 初始化 ExcluItem 如果为空
        data.ExcluItem ??= new HashSet<int>();

        // 取出物品后处理
        if (toTake == total)
        {
            data.TrashList.Remove(itemType);
            plr.SendSuccessMessage($"已从垃圾桶中取出全部:[i/s1:{itemType}]");
           
            if (data.ExcluItem.Contains(itemType)) 
            { 
                data.ExcluItem.Remove(itemType);
                plr.SendInfoMessage($"并已将 [i/s1:{itemType}] 移出排除列表");
            }
        }
        else
        {
            data.TrashList[itemType] = total - toTake;
            if (!data.ExcluItem.Contains(itemType))
            {
                data.ExcluItem.Add(itemType);
                plr.SendInfoMessage($"注意: 物品 [i/s1:{itemType}] 已被添加到排除列表。");
                plr.SendInfoMessage($"可手持该物品,使用 [c/92C5EC:/air a] 来移出排除表。");
            }
        }

        // 更新数据库
        DB.UpdateData(data);

        // 发放物品
        GiveItems(plr, itemType, toTake, maxStack);

        // 显示图标
        List<string> icons = new();
        int fullStacks = toTake / maxStack;
        int remainder = toTake % maxStack;

        for (int i = 0; i < fullStacks; i++)
            icons.Add($"[i/s{maxStack}:{itemType}]");

        if (remainder > 0)
            icons.Add($"[i/s{remainder}:{itemType}]");

        string icon = string.Join(" + ", icons);

        // 剩余数量
        int left = data.TrashList.TryGetValue(itemType, out int remaining) ? remaining : 0;
        plr.SendInfoMessage($"已取出: {icon} \n剩余: {left}个");
    } 
    #endregion

    #region 使用/air del 给物品方法
    private static void GiveItems(TSPlayer plr, int itemType, int count, int maxStack)
    {
        if (count <= 0) return;

        for (int i = 0; i < count / maxStack; i++)
            plr.GiveItem(itemType, maxStack);

        if (count % maxStack > 0)
            plr.GiveItem(itemType, count % maxStack);
    } 
    #endregion
}
