using HPPDonat.Models;

namespace HPPDonat.Data;

public sealed class ResepVarianRepository : IResepVarianRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ResepVarianRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ResepVarianModel>> GetAllAsync()
    {
        var list = new List<ResepVarianModel>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, NamaVarian, IsActive FROM ResepVarian ORDER BY Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ResepVarianModel
            {
                Id = reader.GetInt32(0),
                NamaVarian = reader.GetString(1),
                IsActive = reader.GetInt32(2) == 1
            });
        }

        return list;
    }

    public async Task<ResepVarianModel> AddAsync(string namaVarian, bool isActive = false)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ResepVarian (NamaVarian, IsActive)
            VALUES ($nama, $aktif);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$nama", namaVarian);
        command.Parameters.AddWithValue("$aktif", isActive ? 1 : 0);

        var id = (int)(long)(await command.ExecuteScalarAsync() ?? 0L);

        if (isActive)
        {
            await SetActiveAsync(id);
        }

        return new ResepVarianModel
        {
            Id = id,
            NamaVarian = namaVarian,
            IsActive = isActive
        };
    }

    public async Task SetActiveAsync(int id)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE ResepVarian
            SET IsActive = CASE WHEN Id = $id THEN 1 ELSE 0 END;
            """;
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ResepVarian WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }
}
