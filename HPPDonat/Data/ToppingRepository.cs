using HPPDonat.Models;

namespace HPPDonat.Data;

public sealed class ToppingRepository : IToppingRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ToppingRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ToppingModel>> GetAllAsync()
    {
        var list = new List<ToppingModel>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, NamaTopping, BiayaPerDonat, IsActive FROM Topping ORDER BY Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ToppingModel
            {
                Id = reader.GetInt32(0),
                NamaTopping = reader.GetString(1),
                BiayaPerDonat = reader.GetDecimal(2),
                IsActive = reader.GetInt32(3) == 1
            });
        }

        return list;
    }

    public async Task<ToppingModel> AddAsync(ToppingModel topping)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Topping (NamaTopping, BiayaPerDonat, IsActive)
            VALUES ($nama, $biaya, $isActive);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$nama", topping.NamaTopping);
        command.Parameters.AddWithValue("$biaya", topping.BiayaPerDonat);
        command.Parameters.AddWithValue("$isActive", topping.IsActive ? 1 : 0);

        var id = (long)(await command.ExecuteScalarAsync() ?? 0L);
        topping.Id = (int)id;
        return topping;
    }

    public async Task UpdateAsync(ToppingModel topping)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Topping
            SET NamaTopping = $nama,
                BiayaPerDonat = $biaya,
                IsActive = $isActive
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", topping.Id);
        command.Parameters.AddWithValue("$nama", topping.NamaTopping);
        command.Parameters.AddWithValue("$biaya", topping.BiayaPerDonat);
        command.Parameters.AddWithValue("$isActive", topping.IsActive ? 1 : 0);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Topping WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }
}

