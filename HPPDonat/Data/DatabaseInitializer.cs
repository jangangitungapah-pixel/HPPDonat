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

        await ExecuteNonQueryAsync(connection, "PRAGMA foreign_keys = OFF;");

        await EnsureBahanTableAsync(connection);
        await EnsureResepVarianTableAsync(connection);
        await EnsureResepTableAsync(connection);
        await EnsureToppingTableAsync(connection);
        await EnsureProduksiSettingTableAsync(connection);

        await NormalizeDataAsync(connection);
        await EnsureTriggersAsync(connection);

        await ExecuteNonQueryAsync(connection, "PRAGMA foreign_keys = ON;");
    }

    private static async Task EnsureBahanTableAsync(SqliteConnection connection)
    {
        var bahanExists = await TableExistsAsync(connection, "Bahan");
        if (!bahanExists)
        {
            await ExecuteNonQueryAsync(connection, """
                CREATE TABLE Bahan (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NamaBahan TEXT NOT NULL,
                    Satuan TEXT NOT NULL DEFAULT 'gram',
                    NettoPerPack REAL NOT NULL CHECK (NettoPerPack > 0),
                    HargaPerPack REAL NOT NULL CHECK (HargaPerPack >= 0)
                );
                """);
            return;
        }

        if (!await ColumnExistsAsync(connection, "Bahan", "Satuan"))
        {
            await ExecuteNonQueryAsync(connection, "ALTER TABLE Bahan ADD COLUMN Satuan TEXT NOT NULL DEFAULT 'gram';");
        }
    }

    private static async Task EnsureResepVarianTableAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS ResepVarian (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NamaVarian TEXT NOT NULL UNIQUE,
                IsActive INTEGER NOT NULL CHECK (IsActive IN (0, 1))
            );
            """);

        await ExecuteNonQueryAsync(connection, """
            INSERT INTO ResepVarian (Id, NamaVarian, IsActive)
            SELECT 1, 'Default', 1
            WHERE NOT EXISTS (SELECT 1 FROM ResepVarian);
            """);
    }

    private static async Task EnsureResepTableAsync(SqliteConnection connection)
    {
        var resepExists = await TableExistsAsync(connection, "Resep");
        if (!resepExists)
        {
            await CreateResepTableAsync(connection);
            return;
        }

        var hasVarianId = await ColumnExistsAsync(connection, "Resep", "VarianId");
        if (!hasVarianId)
        {
            await ExecuteNonQueryAsync(connection, "ALTER TABLE Resep RENAME TO Resep_Legacy;");
            await CreateResepTableAsync(connection);

            await ExecuteNonQueryAsync(connection, """
                INSERT INTO Resep (BahanId, VarianId, JumlahDipakai)
                SELECT BahanId, 1, CASE WHEN JumlahDipakai < 0 THEN 0 ELSE JumlahDipakai END
                FROM Resep_Legacy;
                """);

            await ExecuteNonQueryAsync(connection, "DROP TABLE Resep_Legacy;");
        }

        await ExecuteNonQueryAsync(connection, """
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Resep_VarianId_BahanId
            ON Resep (VarianId, BahanId);
            """);
    }

    private static async Task CreateResepTableAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE Resep (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BahanId INTEGER NOT NULL,
                VarianId INTEGER NOT NULL,
                JumlahDipakai REAL NOT NULL CHECK (JumlahDipakai >= 0),
                FOREIGN KEY (BahanId) REFERENCES Bahan(Id) ON DELETE CASCADE,
                FOREIGN KEY (VarianId) REFERENCES ResepVarian(Id) ON DELETE CASCADE
            );
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Resep_VarianId_BahanId
            ON Resep (VarianId, BahanId);
            """);
    }

    private static async Task EnsureToppingTableAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Topping (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NamaTopping TEXT NOT NULL,
                BiayaPerDonat REAL NOT NULL CHECK (BiayaPerDonat >= 0),
                IsActive INTEGER NOT NULL CHECK (IsActive IN (0, 1))
            );
            """);
    }

    private static async Task EnsureProduksiSettingTableAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS ProduksiSetting (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                JumlahDonatDihasilkan REAL NOT NULL CHECK (JumlahDonatDihasilkan > 0),
                BeratPerDonat REAL NOT NULL CHECK (BeratPerDonat > 0),
                WastePersen REAL NOT NULL CHECK (WastePersen >= 0 AND WastePersen <= 99),
                TargetProfitPersen REAL NOT NULL CHECK (TargetProfitPersen >= 1 AND TargetProfitPersen <= 95),
                HariProduksiPerBulan INTEGER NOT NULL CHECK (HariProduksiPerBulan >= 1 AND HariProduksiPerBulan <= 31)
            );
            """);

        if (!await ColumnExistsAsync(connection, "ProduksiSetting", "BeratPerDonat"))
        {
            await ExecuteNonQueryAsync(connection, "ALTER TABLE ProduksiSetting ADD COLUMN BeratPerDonat REAL NOT NULL DEFAULT 50;");
        }
    }

    private static async Task NormalizeDataAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, """
            UPDATE Bahan
            SET NamaBahan = 'Bahan'
            WHERE TRIM(COALESCE(NamaBahan, '')) = '';

            UPDATE Bahan
            SET Satuan = LOWER(TRIM(COALESCE(Satuan, 'gram')));

            UPDATE Bahan
            SET Satuan = 'gram'
            WHERE Satuan NOT IN ('gram', 'kg', 'ml', 'liter', 'butir', 'pcs', 'sendok');

            UPDATE Bahan SET NettoPerPack = 1 WHERE NettoPerPack <= 0;
            UPDATE Bahan SET HargaPerPack = 0 WHERE HargaPerPack < 0;

            UPDATE Resep SET VarianId = 1 WHERE VarianId <= 0;
            UPDATE Resep SET JumlahDipakai = 0 WHERE JumlahDipakai < 0;

            UPDATE Topping SET NamaTopping = 'Topping' WHERE TRIM(COALESCE(NamaTopping, '')) = '';
            UPDATE Topping SET BiayaPerDonat = 0 WHERE BiayaPerDonat < 0;
            UPDATE Topping SET IsActive = 1 WHERE IsActive NOT IN (0, 1);

            UPDATE ProduksiSetting
            SET JumlahDonatDihasilkan = CASE WHEN JumlahDonatDihasilkan <= 0 THEN 1 ELSE JumlahDonatDihasilkan END,
                BeratPerDonat = CASE WHEN BeratPerDonat <= 0 THEN 50 ELSE BeratPerDonat END,
                WastePersen = CASE WHEN WastePersen < 0 THEN 0 WHEN WastePersen > 99 THEN 99 ELSE WastePersen END,
                TargetProfitPersen = CASE WHEN TargetProfitPersen < 1 THEN 1 WHEN TargetProfitPersen > 95 THEN 95 ELSE TargetProfitPersen END,
                HariProduksiPerBulan = CASE WHEN HariProduksiPerBulan < 1 THEN 1 WHEN HariProduksiPerBulan > 31 THEN 31 ELSE HariProduksiPerBulan END
            WHERE Id = 1;

            UPDATE ResepVarian SET NamaVarian = 'Varian' WHERE TRIM(COALESCE(NamaVarian, '')) = '';
            UPDATE ResepVarian SET IsActive = 0 WHERE IsActive NOT IN (0, 1);
            """);

        await ExecuteNonQueryAsync(connection, """
            UPDATE ResepVarian
            SET IsActive = CASE
                WHEN Id = (SELECT Id FROM ResepVarian ORDER BY IsActive DESC, Id LIMIT 1) THEN 1
                ELSE 0
            END;
            """);
    }

    private static async Task EnsureTriggersAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, """
            DROP TRIGGER IF EXISTS trg_bahan_validate_insert;
            DROP TRIGGER IF EXISTS trg_bahan_validate_update;
            DROP TRIGGER IF EXISTS trg_resep_validate_insert;
            DROP TRIGGER IF EXISTS trg_resep_validate_update;
            DROP TRIGGER IF EXISTS trg_topping_validate_insert;
            DROP TRIGGER IF EXISTS trg_topping_validate_update;
            DROP TRIGGER IF EXISTS trg_produksi_validate_insert;
            DROP TRIGGER IF EXISTS trg_produksi_validate_update;
            DROP TRIGGER IF EXISTS trg_varian_validate_insert;
            DROP TRIGGER IF EXISTS trg_varian_validate_update;

            CREATE TRIGGER trg_bahan_validate_insert
            BEFORE INSERT ON Bahan
            WHEN TRIM(COALESCE(NEW.NamaBahan, '')) = '' OR NEW.NettoPerPack <= 0 OR NEW.HargaPerPack < 0
                OR TRIM(COALESCE(NEW.Satuan, '')) = ''
            BEGIN
                SELECT RAISE(ABORT, 'Data bahan tidak valid');
            END;

            CREATE TRIGGER trg_bahan_validate_update
            BEFORE UPDATE ON Bahan
            WHEN TRIM(COALESCE(NEW.NamaBahan, '')) = '' OR NEW.NettoPerPack <= 0 OR NEW.HargaPerPack < 0
                OR TRIM(COALESCE(NEW.Satuan, '')) = ''
            BEGIN
                SELECT RAISE(ABORT, 'Data bahan tidak valid');
            END;

            CREATE TRIGGER trg_resep_validate_insert
            BEFORE INSERT ON Resep
            WHEN NEW.JumlahDipakai < 0 OR NEW.VarianId <= 0
            BEGIN
                SELECT RAISE(ABORT, 'Data resep tidak valid');
            END;

            CREATE TRIGGER trg_resep_validate_update
            BEFORE UPDATE ON Resep
            WHEN NEW.JumlahDipakai < 0 OR NEW.VarianId <= 0
            BEGIN
                SELECT RAISE(ABORT, 'Data resep tidak valid');
            END;

            CREATE TRIGGER trg_topping_validate_insert
            BEFORE INSERT ON Topping
            WHEN TRIM(COALESCE(NEW.NamaTopping, '')) = '' OR NEW.BiayaPerDonat < 0 OR NEW.IsActive NOT IN (0, 1)
            BEGIN
                SELECT RAISE(ABORT, 'Data topping tidak valid');
            END;

            CREATE TRIGGER trg_topping_validate_update
            BEFORE UPDATE ON Topping
            WHEN TRIM(COALESCE(NEW.NamaTopping, '')) = '' OR NEW.BiayaPerDonat < 0 OR NEW.IsActive NOT IN (0, 1)
            BEGIN
                SELECT RAISE(ABORT, 'Data topping tidak valid');
            END;

            CREATE TRIGGER trg_produksi_validate_insert
            BEFORE INSERT ON ProduksiSetting
            WHEN NEW.JumlahDonatDihasilkan <= 0 OR NEW.BeratPerDonat <= 0 OR NEW.WastePersen < 0 OR NEW.WastePersen > 99 OR NEW.TargetProfitPersen < 1 OR NEW.TargetProfitPersen > 95 OR NEW.HariProduksiPerBulan < 1 OR NEW.HariProduksiPerBulan > 31
            BEGIN
                SELECT RAISE(ABORT, 'Pengaturan produksi tidak valid');
            END;

            CREATE TRIGGER trg_produksi_validate_update
            BEFORE UPDATE ON ProduksiSetting
            WHEN NEW.JumlahDonatDihasilkan <= 0 OR NEW.BeratPerDonat <= 0 OR NEW.WastePersen < 0 OR NEW.WastePersen > 99 OR NEW.TargetProfitPersen < 1 OR NEW.TargetProfitPersen > 95 OR NEW.HariProduksiPerBulan < 1 OR NEW.HariProduksiPerBulan > 31
            BEGIN
                SELECT RAISE(ABORT, 'Pengaturan produksi tidak valid');
            END;

            CREATE TRIGGER trg_varian_validate_insert
            BEFORE INSERT ON ResepVarian
            WHEN TRIM(COALESCE(NEW.NamaVarian, '')) = '' OR NEW.IsActive NOT IN (0, 1)
            BEGIN
                SELECT RAISE(ABORT, 'Data varian tidak valid');
            END;

            CREATE TRIGGER trg_varian_validate_update
            BEFORE UPDATE ON ResepVarian
            WHEN TRIM(COALESCE(NEW.NamaVarian, '')) = '' OR NEW.IsActive NOT IN (0, 1)
            BEGIN
                SELECT RAISE(ABORT, 'Data varian tidak valid');
            END;
            """);
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return count > 0;
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var existingColumn = reader.GetString(1);
            if (string.Equals(existingColumn, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
