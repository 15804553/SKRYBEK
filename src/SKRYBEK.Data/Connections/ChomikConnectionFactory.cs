using System.Data.OleDb;
using SKRYBEK.Core.Configuration;

namespace SKRYBEK.Data.Connections;

public sealed class ChomikConnectionFactory
{
    private readonly string _databasePath;
    private static readonly string[] DatabasePasswords = ["5359", "5393"];

    public ChomikConnectionFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public OleDbConnection Create()
    {
        if (string.IsNullOrWhiteSpace(_databasePath))
            throw new InvalidOperationException(
                $"Nie ustawiono ścieżki bazy CHOMIK w pliku {DatabasePatch.FileName}.");

        Exception? lastError = null;
        foreach (var pwd in DatabasePasswords)
        {
            try
            {
                var conn = new OleDbConnection(BuildConnectionString(pwd));
                conn.Open();
                conn.Close();
                return new OleDbConnection(BuildConnectionString(pwd));
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            $"Nie można otworzyć bazy CHOMIK:\n{_databasePath}",
            lastError);
    }

    private string BuildConnectionString(string databasePassword) =>
        $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_databasePath};Jet OLEDB:Database Password={databasePassword};";

    public string DatabasePath => _databasePath;

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conn = Create();
            await conn.OpenAsync();
            return conn.State == System.Data.ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }
}
