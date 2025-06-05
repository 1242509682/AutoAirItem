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
        MyData.PlayerData? data = Data.Items.FirstOrDefault(x => x.Name == plr.Name);

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

                                //当从排除列表中移除时，尝试将物品添加到自动垃圾桶
                                if (data.TrashList.ContainsKey(sel.type))
                                {
                                    var other = new HashSet<int>();

                                    for (var i = 0; i < plr.TPlayer.inventory.Length; i++)
                                    {
                                        var inv = plr.TPlayer.inventory[i];
                                        if (data.TrashList.ContainsKey(inv.type) &&
                                            !data.ExcluItem.Contains(inv.type))
                                        {
                                            other.Add(inv.type);
                                            data.TrashList[inv.type] += inv.stack;
                                            inv.TurnToAir();
                                            plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, i);
                                        }
                                    }

                                    // 将物品ID转换为图标字符串
                                    var icons = other
                                        .OrderBy(id => id)
                                        .Select(id => $"[i/s1:{id}]")
                                        .ToList();

                                    // 按每行数量分组显示（使用 Config.ListLine）
                                    var chunks = icons
                                        .Select((icon, index) => new { icon, index })
                                        .GroupBy(x => x.index / Config.ListLine)
                                        .Select(g => string.Join(" ", g.Select(x => x.icon)));

                                    var text = string.Join("\n", chunks);

                                    if (!string.IsNullOrEmpty(text))
                                    {
                                        plr.SendMessage($"同时回收物品:\n{text}", 240, 250, 150);
                                    }
                                }
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
                            var trash = data.TrashList;
                            if (trash.Count == 0)
                            {
                                plr.SendErrorMessage($"\n[{data.Name}的垃圾桶] 中没有任何物品。");
                                return;
                            }

                            var item = new Item();
                            item.SetDefaults(0);
                            int MaxStack = item.maxStack;

                            // 生成垃圾桶图标（现在加上索引）
                            var icons = trash
                                .OrderBy(k => k.Key)
                                .Select((pair, index) => new { Icon = GetItemIcons(pair.Key, pair.Value, MaxStack), Index = index + 1 })
                                .SelectMany(x => x.Icon.Select(icon => $"[c/A0CAEB:{x.Index}.]{icon}")) // 每个图标前加索引
                                .ToList();

                            SendFormattedList(plr, icons, $"{data.Name}的垃圾桶", Config.ListLine);

                            // 如果存在排除列表，则发送
                            if (data.ExcluItem != null && data.ExcluItem.Any())
                            {
                                var excluIcons = data.ExcluItem
                                    .OrderBy(id => id)
                                    .Select(id => $"[i/s1:{id}]")
                                    .ToList();

                                SendFormattedList(plr, excluIcons, $"{data.Name}的排除表", Config.ListLine);
                            }


                            plr.SendMessage($"\n指定[c/FF8292:序号]取出全部:/air d [c/C497F7:序号] [c/C497F7:-i]", 240, 250, 150);
                            plr.SendMessage($"指定[c/FF8292:序号]取出数量:/air d [c/C497F7:序号] 数量 [c/C497F7:-i]", 240, 250, 150);
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
                    plr.SendMessage("《自动垃圾桶》 [i:3456][C/F2F2C7:插件开发] [C/BFDFEA:by] [c/00FFFF:羽学][i:3459]\n" +
                    "/air on —— 开启|关闭[c/EA9944:垃圾桶]功能\n" +
                    "/air l —— [c/AEEBE9:列出]自己的[c/F2F191:垃圾桶]\n" +
                    "/air m —— 开启|关闭[c/76A4CF:清理消息]\n" +
                    "/air a —— 将手上物品[c/DB4057:排除]或[c/77D1B2:还原]到检测\n" +
                    "/air d 名字 —— 从自动垃圾桶[c/8AD278:全部取出]\n" +
                    "/air d 名字 数量 —— 从垃圾桶[c/F2F292:取出指定数量]\n" +
                    "/air c —— [c/85CEDF:清空]垃圾桶与回收[c/DB4057:排除表]\n" +
                    "/air ck 数量 —— 筛选物品超过[c/D278BD:此数量]的玩家\n" +
                    "/air rs —— 清空[c/85CFDE:所有玩家]数据", 220, 180, 186);
                    if (!data.Enabled)
                    {
                        plr.SendSuccessMessage($"请输入该指令开启→: [c/92C5EC:/air on] ");
                    }

                    if (!TShock.ServerSideCharacterConfig.Settings.Enabled)
                    {
                        plr.SendErrorMessage($"本插件需要开启SSC强制开荒才会回收物品");
                    }
                }
            }
            else
            {
                plr.SendMessage("《自动垃圾桶》控制台可用指令\n" +
                    "/air ck 数量—— 筛选物品超过[c/D278BD:此数量]的玩家\n" +
                    "/air rs —— 清空[c/85CFDE:所有玩家]数据", 193, 223, 186);
                plr.SendSuccessMessage($"其余指令需要您进入游戏内才会显示");
            }
        }
        else
        {
            if (data != null)
            {
                plr.SendMessage("《自动垃圾桶》 [i:3456][C/F2F2C7:插件开发] [C/BFDFEA:by] [c/00FFFF:羽学][i:3459]\n" +
                    "/air on —— 开启|关闭[c/EA9944:垃圾桶]功能\n" +
                    "/air l —— [c/AEEBE9:列出]自己的[c/F2F191:垃圾桶]\n" +
                    "/air m —— 开启|关闭[c/76A4CF:清理消息]\n" +
                    "/air a —— 将手上物品[c/DB4057:排除]或[c/77D1B2:还原]到检测\n" +
                    "/air d 名字 —— 从自动垃圾桶[c/8AD278:全部取出]\n" +
                    "/air d 名字 数量 —— 从垃圾桶[[c/F2F292:取出指定数量]\n" +
                    "/air c —— [c/85CEDF:清空]垃圾桶与回收[c/DB4057:排除表]\n" +
                    "/air ck 数量 —— 筛选物品超过[c/D278BD:此数量]的玩家\n" +
                    "/air m —— 开启|关闭[c/F2F292:清理消息]", 220, 180, 186);

                if (!data.Enabled)
                {
                    plr.SendSuccessMessage($"请输入该指令开启→: [c/92C5EC:/air on] ");
                }
            }
        }
    }
    #endregion

    #region 检查物品数量方法（玩家版）
    public static void CheckCmd(CommandArgs args, int num)
    {
        var plr = args.Player;

        // 获取默认最大堆叠数
        var defaultItem = new Item();
        defaultItem.SetDefaults(0);
        int defaultMaxStack = defaultItem.maxStack;

        // 构建图标条目
        var ItemList = BuildItemList(data =>
        {
            var findStart = data.TrashList.Where(pair => pair.Value >= num).ToList();
            if (!findStart.Any()) return new List<string>();

            var icons = new List<string>();
            foreach (var t in findStart)
            {
                Item item = new Item();
                item.SetDefaults(t.Key);
                int myStack = item.maxStack > 0 ? item.maxStack : defaultMaxStack;

                int fullStacks = t.Value / myStack;
                int remainder = t.Value % myStack;

                for (int i = 0; i < fullStacks; i++)
                    icons.Add($"[i/s{myStack}:{t.Key}]");

                if (remainder > 0)
                    icons.Add($"[i/s{remainder}:{t.Key}]");
            }

            return icons;
        });

        SendFormattedMessage(plr, ItemList,
            Config.ListLine, (icon) => icon, $"以下垃圾物品数量超过【[c/76A4CF:{num}]】的玩家",
            (index, name) => $"\n([c/A0EBD3:{index}]).[c/F5677A:{name}:]",
            (r, g, b) => (r: 193, g: 223, b: 186));
    }
    #endregion

    #region 检查物品数量方法 (控制台优化版)
    public static void CheckCmd2(CommandArgs args, int num)
    {
        var plr = args.Player;

        // 构建文本条目
        var ItemList = BuildItemList(data =>
        {
            var findStart = data.TrashList.Where(pair => pair.Value >= num).ToList();
            if (!findStart.Any()) return new List<string>();

            return findStart
                .Select(t => $"{Lang.GetItemNameValue(t.Key)}({t.Value})")
                .ToList();
        });

        SendFormattedMessage(plr, ItemList,
            Config.ListLine, (entry) => entry, $"以下垃圾物品数量超过【[c/76A4CF:{num}]】的玩家\n",
            (index, name) => $"\n[c/A0EBD3:{index}.][c/F3E83B:{name}:]",
            (r, g, b) => (r: 250, g: 240, b: 150));
    }
    #endregion

    #region 返还物品时显示图标，并将该物品自动加入排除表逻辑
    private static void HandleTrashRemoveCommand(TSPlayer plr, CommandArgs args, MyData.PlayerData? data)
    {
        if (data == null) return;

        // 参数解析
        if (args.Parameters.Count < 2)
        {
            plr.SendErrorMessage("指令格式错误。用法: /air d <物品名|ID [数量]> 或 /air d <索引> [-i] [数量]");
            return;
        }

        bool useIndex = args.Parameters.Contains("-i");
        int indexOrItemId;

        if (!int.TryParse(args.Parameters[1], out indexOrItemId))
        {
            plr.SendErrorMessage("请输入有效的数字作为索引或物品ID。");
            return;
        }

        int howMuch = -1; // 默认 -1 表示全部取出
        int amountIndex = useIndex ? 2 : 2;

        // 如果参数足够且第3个是数字，则读取为取出数量
        if (args.Parameters.Count > amountIndex && int.TryParse(args.Parameters[amountIndex], out int parsedAmount))
        {
            howMuch = parsedAmount;
        }

        int itemType = -1;

        if (useIndex)
        {
            // 索引模式
            int index = indexOrItemId;

            if (index < 1)
            {
                plr.SendErrorMessage("索引必须大于等于1。");
                return;
            }

            var sortedTrash = data.TrashList.OrderBy(k => k.Key).ToList();

            if (index > sortedTrash.Count)
            {
                plr.SendErrorMessage($"索引超出范围，当前只有 {sortedTrash.Count} 项。");
                return;
            }

            itemType = sortedTrash[index - 1].Key;
        }
        else
        {
            // 物品名或ID模式
            string itemNameOrId = args.Parameters[1];

            if (!int.TryParse(itemNameOrId, out itemType))
            {
                var items = TShock.Utils.GetItemByIdOrName(itemNameOrId);
                if (items.Count == 0)
                {
                    plr.SendErrorMessage("找不到该物品，请输入正确的物品名或ID。");
                    return;
                }

                itemType = items[0].type;
            }

            if (!data.TrashList.TryGetValue(itemType, out _) || itemType <= 0)
            {
                plr.SendErrorMessage($"物品 [i/s1:{itemType}] 不在垃圾桶中！");
                return;
            }
        }

        // 获取物品 maxStack
        var item = new Item();
        item.SetDefaults(itemType);
        int maxStack = item.maxStack > 0 ? item.maxStack : 1;

        // 获取该物品在垃圾桶中的总数
        if (!data.TrashList.TryGetValue(itemType, out int total) || total <= 0)
        {
            plr.SendErrorMessage($"物品 [i/s1:{itemType}] 不在垃圾桶中！");
            return;
        }

        // 确定实际取出数量
        int toTake = howMuch == -1 ? total : Math.Min(howMuch, total);

        // 初始化 ExcluItem（如果为空）
        data.ExcluItem ??= new HashSet<int>();

        // 处理取出逻辑
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

    #region 根据物品 ID 和数量生成多个图标（按最大堆叠分割）
    private static List<string> GetItemIcons(int itemId, int total, int defaultMaxStack)
    {
        var item = new Item();
        item.SetDefaults(itemId);
        int maxStack = item.maxStack > 0 ? item.maxStack : defaultMaxStack;

        var icons = new List<string>();

        int fullStacks = total / maxStack;
        int remainder = total % maxStack;

        for (int i = 0; i < fullStacks; i++)
            icons.Add($"[i/s{maxStack}:{itemId}]");

        if (remainder > 0)
            icons.Add($"[i/s{remainder}:{itemId}]");

        return icons;
    }
    #endregion

    #region 将图标列表分组显示并发送给玩家
    // 将图标列表分组显示并发送给玩家
    private static void SendFormattedList(TSPlayer plr, List<string> icons, string title, int lineSize = 7)
    {
        var chunks = icons
            .Select((icon, index) => new { icon, index })
            .GroupBy(x => x.index / lineSize)
            .Select(g => string.Join("  ", g.Select(x => x.icon)));

        var text = string.Join("\n", chunks);
        plr.SendInfoMessage($"\n[c/F2FF9C:{title}]\n{text}");
    }
    #endregion

    #region 构建物品列表提供给CK指令用
    private static List<(int Index, string Name, List<string> Entries)> BuildItemList(Func<MyData.PlayerData, List<string>> itemSelector)
    {
        var result = new List<(int Index, string Name, List<string> Entries)>();
        int index = 1;

        foreach (var data in AutoAirItem.DB.GetAll())
        {
            var entries = itemSelector(data);
            if (entries.Count > 0)
            {
                result.Add((index++, data.Name, entries));
            }
        }

        return result;
    }
    #endregion

    #region ck指令格式化信息方法
    private static void SendFormattedMessage<T>(TSPlayer plr, List<(int Index, string Name, List<T> Entries)> playerList, int lineSize, Func<T, string> toString, string header, Func<int, string, string> formatTitle, Func<byte, byte, byte, (byte r, byte g, byte b)> getColor)
    {
        plr.SendInfoMessage(header);

        if (playerList.Count == 0)
        {
            plr.SendInfoMessage($"没有找到符合条件的玩家");
            return;
        }

        foreach (var p in playerList)
        {
            var chunks = p.Entries
                .Select((item, idx) => new { item, idx })
                .GroupBy(x => x.idx / lineSize)
                .Select(g => string.Join("  ", g.Select(x => toString(x.item))));

            var mess = formatTitle(p.Index, p.Name) + "\n" + string.Join("\n", chunks);
            var color = getColor(0, 0, 0);
            plr.SendMessage(mess, color.r, color.g, color.b);
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
}
