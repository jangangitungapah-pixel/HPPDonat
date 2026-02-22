using Microsoft.Data.Sqlite;

namespace HPPDonat.Data;

public sealed class DatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Bahan (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NamaBahan TEXT NOT NULL,
                NettoPerPack REAL NOT NULL,
                HargaPerPack REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Resep (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BahanId INTEGER NOT NULL UNIQUE,
                JumlahDipakai REAL NOT NULL,
                FOREIGN KEY (BahanId) REFERENCES Bahan(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Topping (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NamaTopping TEXT NOT NULL,
                BiayaPerDonat REAL NOT NULL,
                IsActive INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProduksiSetting (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                JumlahDonatDihasilkan REAL NOT NULL,
                WastePersen REAL NOT NULL,
                TargetProfitPersen REAL NOT NULL,
                HariProduksiPerBulan INTEGER NOT NULL
            );
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}

