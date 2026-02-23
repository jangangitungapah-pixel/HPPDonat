using HPPDonat.Models;

namespace HPPDonat.Data;

public sealed class ProduksiSettingRepository : IProduksiSettingRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ProduksiSettingRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ProduksiSettingModel> GetOrCreateAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var queryCommand = connection.CreateCommand();
        queryCommand.CommandText = """
            SELECT Id, JumlahDonatDihasilkan, BeratPerDonat, WastePersen, TargetProfitPersen, HariProduksiPerBulan
            FROM ProduksiSetting
            WHERE Id = 1;
            """;

        await using var reader = await queryCommand.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProduksiSettingModel
            {
                Id = reader.GetInt32(0),
                JumlahDonatDihasilkan = reader.GetDecimal(1),
                BeratPerDonat = reader.GetDecimal(2),
                WastePersen = reader.GetDecimal(3),
                TargetProfitPersen = reader.GetDecimal(4),
                HariProduksiPerBulan = reader.GetInt32(5)
            };
        }

        var defaultSetting = new ProduksiSettingModel();

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO ProduksiSetting (Id, JumlahDonatDihasilkan, BeratPerDonat, WastePersen, TargetProfitPersen, HariProduksiPerBulan)
            VALUES (1, $jumlahDonat, $beratPerDonat, $waste, $targetProfit, $hariProduksi);
            """;
        insertCommand.Parameters.AddWithValue("$jumlahDonat", defaultSetting.JumlahDonatDihasilkan);
        insertCommand.Parameters.AddWithValue("$beratPerDonat", defaultSetting.BeratPerDonat);
        insertCommand.Parameters.AddWithValue("$waste", defaultSetting.WastePersen);
        insertCommand.Parameters.AddWithValue("$targetProfit", defaultSetting.TargetProfitPersen);
        insertCommand.Parameters.AddWithValue("$hariProduksi", defaultSetting.HariProduksiPerBulan);
        await insertCommand.ExecuteNonQueryAsync();

        return defaultSetting;
    }

    public async Task UpdateAsync(ProduksiSettingModel setting)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE ProduksiSetting
            SET JumlahDonatDihasilkan = $jumlahDonat,
                BeratPerDonat = $beratPerDonat,
                WastePersen = $waste,
                TargetProfitPersen = $targetProfit,
                HariProduksiPerBulan = $hariProduksi
            WHERE Id = 1;
            """;
        command.Parameters.AddWithValue("$jumlahDonat", setting.JumlahDonatDihasilkan);
        command.Parameters.AddWithValue("$beratPerDonat", setting.BeratPerDonat);
        command.Parameters.AddWithValue("$waste", setting.WastePersen);
        command.Parameters.AddWithValue("$targetProfit", setting.TargetProfitPersen);
        command.Parameters.AddWithValue("$hariProduksi", setting.HariProduksiPerBulan);

        await command.ExecuteNonQueryAsync();
    }
}

