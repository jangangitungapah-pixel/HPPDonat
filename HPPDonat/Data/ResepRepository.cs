using HPPDonat.Models;

namespace HPPDonat.Data;

public sealed class ResepRepository : IResepRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ResepRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ResepModel>> GetAllAsync()
    {
        var list = new List<ResepModel>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, BahanId, JumlahDipakai FROM Resep ORDER BY Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ResepModel
            {
                Id = reader.GetInt32(0),
                BahanId = reader.GetInt32(1),
                JumlahDipakai = reader.GetDecimal(2)
            });
        }

        return list;
    }

    public async Task<ResepModel> AddAsync(ResepModel resep)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Resep (BahanId, JumlahDipakai)
            VALUES ($bahanId, $jumlahDipakai);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$bahanId", resep.BahanId);
        command.Parameters.AddWithValue("$jumlahDipakai", resep.JumlahDipakai);

        var id = (long)(await command.ExecuteScalarAsync() ?? 0L);
        resep.Id = (int)id;
        return resep;
    }

    public async Task UpdateJumlahDipakaiAsync(int id, decimal jumlahDipakai)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Resep SET JumlahDipakai = $jumlahDipakai WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$jumlahDipakai", jumlahDipakai);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteByBahanIdAsync(int bahanId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Resep WHERE BahanId = $bahanId";
        command.Parameters.AddWithValue("$bahanId", bahanId);
        await command.ExecuteNonQueryAsync();
    }
}

