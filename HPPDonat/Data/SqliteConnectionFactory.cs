using System.IO;
using Microsoft.Data.Sqlite;

namespace HPPDonat.Data;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory()
    {
        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HPPDonatCalculator");

        Directory.CreateDirectory(appFolder);
        var dbPath = Path.Combine(appFolder, "hpp_donat_calculator.db");
        _connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared";
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}

