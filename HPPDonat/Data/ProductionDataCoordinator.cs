using HPPDonat.Models;
using Microsoft.Data.Sqlite;

namespace HPPDonat.Data;

public sealed class ProductionDataCoordinator : IProductionDataCoordinator
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ProductionDataCoordinator(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(BahanModel bahan, IReadOnlyList<ResepModel> resepItems)> AddBahanDanResepAsync(BahanModel bahan, IReadOnlyList<int> varianIds, decimal jumlahDipakai)
    {
        var resepItems = new List<ResepModel>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var foreignKeyCommand = connection.CreateCommand();
        foreignKeyCommand.CommandText = "PRAGMA foreign_keys = ON;";
        await foreignKeyCommand.ExecuteNonQueryAsync();

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        await using var insertBahan = connection.CreateCommand();
        insertBahan.Transaction = transaction;
        insertBahan.CommandText = """
            INSERT INTO Bahan (NamaBahan, Satuan, NettoPerPack, HargaPerPack)
            VALUES ($nama, $satuan, $netto, $harga);
            SELECT last_insert_rowid();
            """;
        insertBahan.Parameters.AddWithValue("$nama", bahan.NamaBahan);
        insertBahan.Parameters.AddWithValue("$satuan", bahan.Satuan);
        insertBahan.Parameters.AddWithValue("$netto", bahan.NettoPerPack);
        insertBahan.Parameters.AddWithValue("$harga", bahan.HargaPerPack);

        var bahanId = (int)(long)(await insertBahan.ExecuteScalarAsync() ?? 0L);
        bahan.Id = bahanId;

        foreach (var varianId in varianIds.Distinct())
        {
            await using var insertResep = connection.CreateCommand();
            insertResep.Transaction = transaction;
            insertResep.CommandText = """
                INSERT INTO Resep (BahanId, VarianId, JumlahDipakai)
                VALUES ($bahanId, $varianId, $jumlah);
                SELECT last_insert_rowid();
                """;
            insertResep.Parameters.AddWithValue("$bahanId", bahanId);
            insertResep.Parameters.AddWithValue("$varianId", varianId);
            insertResep.Parameters.AddWithValue("$jumlah", jumlahDipakai);

            var resepId = (int)(long)(await insertResep.ExecuteScalarAsync() ?? 0L);
            resepItems.Add(new ResepModel
            {
                Id = resepId,
                BahanId = bahanId,
                VarianId = varianId,
                JumlahDipakai = jumlahDipakai
            });
        }

        await transaction.CommitAsync();
        return (bahan, resepItems);
    }

    public async Task DeleteBahanDanResepAsync(int bahanId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var foreignKeyCommand = connection.CreateCommand();
        foreignKeyCommand.CommandText = "PRAGMA foreign_keys = ON;";
        await foreignKeyCommand.ExecuteNonQueryAsync();

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        await using var deleteBahan = connection.CreateCommand();
        deleteBahan.Transaction = transaction;
        deleteBahan.CommandText = "DELETE FROM Bahan WHERE Id = $id";
        deleteBahan.Parameters.AddWithValue("$id", bahanId);
        await deleteBahan.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }
}
