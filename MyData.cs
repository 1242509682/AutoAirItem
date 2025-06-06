﻿namespace AutoAirItem;

public class MyData
{
    //玩家数据表
    public List<PlayerData> Items { get; set; } = new List<PlayerData>();

    #region 玩家数据结构
    public class PlayerData
    {
        //玩家名字
        public string Name { get; set; }

        //玩家自己的自动垃圾桶开关
        public bool Enabled { get; set; } = false;

        //自动垃圾桶的回收提示
        public bool Mess { get; set; } = true;

        //物品的字典
        public Dictionary<int, int> TrashList { get; set; } = new Dictionary<int, int>();

        //排除物品的集合
        public HashSet<int> ExcluItem { get; set; } = new HashSet<int>();

        public PlayerData(string name = "", bool enabled = true, bool mess = true, Dictionary<int, int> trashList = null!, HashSet<int> excluItem = null!)
        {
            this.Name = name ?? "";
            this.Enabled = enabled;
            this.Mess = mess;
            this.TrashList = trashList;
            this.ExcluItem = excluItem ?? new HashSet<int>();
        }
    }
    #endregion
}