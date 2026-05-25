using System.Data.OleDb;

namespace SKRYBEK.Data.Connections;

public sealed class SkrybekConnectionFactory
{
    private readonly string _databasePath;
    private const string Password = "5359";

    public SkrybekConnectionFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public OleDbConnection Create()
    {
        var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_databasePath};Jet OLEDB:Database Password={Password};";
        return new OleDbConnection(connectionString);
    }

    public string DatabasePath => _databasePath;
}
