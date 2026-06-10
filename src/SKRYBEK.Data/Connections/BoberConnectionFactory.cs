using System.Data.OleDb;

namespace SKRYBEK.Data.Connections;

public sealed class BoberConnectionFactory
{
    private readonly string _databasePath;
    private const string Password = "5359";

    public BoberConnectionFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public OleDbConnection Create()
    {
        var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_databasePath};Jet OLEDB:Database Password={Password};";
        return new OleDbConnection(connectionString);
    }

    public string DatabasePath => _databasePath;

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conn = Create();
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
