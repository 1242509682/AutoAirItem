using System.Text.Json;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace AutoAirItem;

public class Database
{
    #region ����Ͱ���ݱ�ṹ
    public Database()
    {
        // Tshock 6 �����ݣ�
        // var sql = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlQueryBuilder());

        // Tshock 5 �����ݣ�
        var sql = new SqlTableCreator(TShock.DB, new SqliteQueryCreator());

        // ���岢ȷ�� AutoTrash ��Ľṹ
        sql.EnsureTableStructure(new SqlTable("AutoTrash", //����
            new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, AutoIncrement = true }, // ������
            new SqlColumn("Name", MySqlDbType.TinyText) { NotNull = true }, // �ǿ��ַ�����
            new SqlColumn("Enabled", MySqlDbType.Int32) { DefaultValue = "0" }, // boolֵ��
            new SqlColumn("Mess", MySqlDbType.Int32) { DefaultValue = "1" }, // boolֵ��
            new SqlColumn("TrashList", MySqlDbType.Text) // �ı��У����ڴ洢���л����Ƴ���Ʒ�ֵ�
        ));
    }
    #endregion

    #region �������ݷ���
    public bool AddData(MyData.PlayerData data)
    {
        var trashList = JsonSerializer.Serialize(data.TrashList);

        return TShock.DB.Query("INSERT INTO AutoTrash (Name, Enabled, Mess, TrashList) VALUES (@0, @1, @2, @3)",
            data.Name, data.Enabled ? 1 : 0, data.Mess ? 1 : 0, trashList) != 0;
    }
    #endregion

    #region ��������
    public bool UpdateData(MyData.PlayerData data)
    {
        var trashList = JsonSerializer.Serialize(data.TrashList);

        // �������м�¼
        if (TShock.DB.Query("UPDATE AutoTrash SET Enabled = @0, Mess = @1, TrashList = @2 WHERE Name = @3",
            data.Enabled ? 1 : 0, data.Mess ? 1 : 0, trashList, data.Name) != 0)
        {
            return true;
        }

        // ���û�и��µ��κμ�¼��������¼�¼
        return TShock.DB.Query("INSERT INTO AutoTrash (Name, Enabled, Mess, TrashList) VALUES (@0, @1, @2, @3)",
            data.Name, data.Enabled ? 1 : 0, data.Mess ? 1 : 0, trashList) != 0;
    }
    #endregion

    #region ��ȡ�������
    public MyData.PlayerData? GetData(string name)
    {
        using var reader = TShock.DB.QueryReader("SELECT * FROM AutoTrash WHERE Name = @0", name);

        if (reader.Read())
        {
            return new MyData.PlayerData
            (
                name: reader.Get<string>("Name"),
                enabled: reader.Get<int>("Enabled") == 1,
                mess: reader.Get<int>("Mess") == 1,
                trashList: JsonSerializer.Deserialize<Dictionary<int, int>>(reader.Get<string>("TrashList"))! // �����л����ֵ�
            );
        }

        return null;
    }
    #endregion

    #region �����������ݣ�ÿ������������ʱ���ڶ�ȡ֮ǰ���µ����ݣ���Ҫ���ڽ�����ڴ����ʱ���ݶ�ʧ�ķ���
    public List<MyData.PlayerData> GetAllData()
    {
        var data = new List<MyData.PlayerData>();

        using var reader = TShock.DB.QueryReader("SELECT * FROM AutoTrash");

        while (reader.Read())
        {
            data.Add(new MyData.PlayerData(
                name: reader.Get<string>("Name"),
                enabled: reader.Get<int>("Enabled") == 1,
                mess: reader.Get<int>("Mess") == 1,
                trashList: JsonSerializer.Deserialize<Dictionary<int, int>>(reader.Get<string>("TrashList"))!
            ));
        }

        return data;
    }
    #endregion

    #region �����������ݷ���
    public bool ClearData()
    {
        return TShock.DB.Query("DELETE FROM AutoTrash") != 0;
    }
    #endregion
}