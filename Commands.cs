using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace AutoAirItem;

public class Commands
{
    public static void AirCmd(CommandArgs args)
    {
        var name = args.Player.Name;
        var config = AutoAirItem.Config.Items.FirstOrDefault(item => item.Name == name);

        if (!AutoAirItem.Config.Open) return;
        if (config == null)
        {
            args.Player.SendErrorMessage("请用角色进入服务器后输入：/air 指令查看菜单");
            return;
        }

        if (args.Parameters.Count == 0)
        {
            HelpCmd(args.Player);
            if (!config.Enabled)
            {
                args.Player.SendSuccessMessage($"请输入该指令开启→: [c/92C5EC:/air on] ");
            }
            else
            {
                args.Player.SendSuccessMessage($"您的垃圾桶监听状态为：[c/92C5EC:{config.TrashItem}]");
            }
            return;
        }

        if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "list")
        {
            args.Player.SendInfoMessage($"[{config.Name}的垃圾桶]\n" + string.Join(", ", config.ItemName.Select(x => "[c/92C5EC:{0}]".SFormat(x))));
            return;
        }

        if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "on")
        {
            bool isEnabled = config.Enabled;
            config.Enabled = !isEnabled;
            string Mess = isEnabled ? "禁用" : "启用";
            args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{Mess}]自动垃圾桶功能。");
            AutoAirItem.Config.Write();
            return;
        }

        if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "clear")
        {
            config.ItemName.Clear();
            args.Player.SendSuccessMessage($"已清理[c/92C5EC: {args.Player.Name} ]的自动垃圾桶表");
            AutoAirItem.Config.Write();
            return;
        }

        if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "yes")
        {
            config.ItemName.Add(args.TPlayer.inventory[args.TPlayer.selectedItem].Name);
            AutoAirItem.Config.Write();
            args.Player.SendSuccessMessage("手选物品 [c/92C5EC:{0}] 已加入自动垃圾桶中! 脱手即清!", args.TPlayer.inventory[args.TPlayer.selectedItem].Name);
            return;
        }

        if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "auto")
        {
            bool isEnabled = config.TrashItem;
            config.TrashItem = !isEnabled;
            string Mess = isEnabled ? "禁用" : "启用";
            args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 的垃圾桶位格监听功能已[c/92C5EC:{Mess}]");
            AutoAirItem.Config.Write();
            return;
        }

        if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "mess")
        {
            bool isEnabled = config.Mess;
            config.Mess = !isEnabled;
            string Mess = isEnabled ? "禁用" : "启用";
            args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 的自动清理消息已[c/92C5EC:{Mess}]");
            AutoAirItem.Config.Write();
            return;
        }

        if (args.Parameters.Count == 2)
        {
            Item item;
            List<Item> Items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
            if (Items.Count > 1)
            {
                args.Player.SendMultipleMatchError(Items.Select(i => i.Name));
                return;
            }

            if (Items.Count == 0)
            {
                args.Player.SendErrorMessage("不存在该物品，\"物品查询\": \"[c/92C5EC:https://terraria.wiki.gg/zh/wiki/Item_IDs]\"");
                return;
            }

            else
                item = Items[0];

            switch (args.Parameters[0].ToLower())
            {
                case "add":
                    {
                        if (config.ItemName.Contains(item.Name))
                        {
                            args.Player.SendErrorMessage("物品 [c/92C5EC:{0}] 已在垃圾桶中!", item.Name);
                            return;
                        }
                        config.ItemName.Add(item.Name);
                        AutoAirItem.Config.Write();
                        args.Player.SendSuccessMessage("已成功将物品添加到垃圾桶: [c/92C5EC:{0}]!", item.Name);
                        break;
                    }
                case "delete":
                case "del":
                case "remove":
                    {
                        if (!config.ItemName.Contains(item.Name))
                        {
                            args.Player.SendErrorMessage("物品 {0} 不在垃圾桶中!", item.Name);
                            return;
                        }
                        config.ItemName.Remove(item.Name);
                        AutoAirItem.Config.Write();
                        args.Player.SendSuccessMessage("已成功从垃圾桶删除物品: [c/92C5EC:{0}]!", item.Name);
                        break;
                    }

                default:
                    {
                        HelpCmd(args.Player);
                        break;
                    }
            }
        }
    }

    #region 菜单方法
    private static void HelpCmd(TSPlayer player)
    {
        if (player == null) return;
        else
        {
            player.SendMessage("【自动垃圾桶】指令菜单\n" +
             "/air —— 查看垃圾桶菜单\n" +
             "/air on —— 开启|关闭垃圾桶功能\n" +
             "/air list —— 列出自己的垃圾桶\n" +
             "/air clear —— 清理垃圾桶\n" +
             "/air yes —— 将手持物品加入垃圾桶\n" +
             "/air auto —— 监听垃圾桶位格开关\n" +
             "/air mess —— 开启|关闭清理消息\n" +
             "/air add 或 del 物品名字 —— 添加|删除《自动垃圾桶》的物品", Color.AntiqueWhite);
        }
    }
    #endregion
}
