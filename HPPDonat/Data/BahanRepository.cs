using HPPDonat.Models;

namespace HPPDonat.Data;

public sealed class BahanRepository : IBahanRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public BahanRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<BahanModel>> GetAllAsync()
    {
        var list = new List<BahanModel>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, NamaBahan, NettoPerPack, HargaPerPack FROM Bahan ORDER BY Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new BahanModel
            {
                Id = reader.GetInt32(0),
                NamaBahan = reader.GetString(1),
                NettoPerPack = reader.GetDecimal(2),
                HargaPerPack = reader.GetDecimal(3)
            });
        }

        return list;
    }

    public async Task<BahanModel> AddAsync(BahanModel bahan)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Bahan (NamaBahan, NettoPerPack, HargaPerPack)
            VALUES ($nama, $netto, $harga);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$nama", bahan.NamaBahan);
        command.Parameters.AddWithValue("$netto", bahan.NettoPerPack);
        command.Parameters.AddWithValue("$harga", bahan.HargaPerPack);

        var id = (long)(await command.ExecuteScalarAsync() ?? 0L);
        bahan.Id = (int)id;
        return bahan;
    }

    public async Task UpdateAsync(BahanModel bahan)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Bahan
            SET NamaBahan = $nama,
                NettoPerPack = $netto,
                HargaPerPack = $harga
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", bahan.Id);
        command.Parameters.AddWithValue("$nama", bahan.NamaBahan);
        command.Parameters.AddWithValue("$netto", bahan.NettoPerPack);
        command.Parameters.AddWithValue("$harga", bahan.HargaPerPack);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Bahan WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }
}

