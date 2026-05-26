using System.Data.Common;
using System.Text;

namespace SKRYBEK.Data;

internal static class OleDbReaderExtensions
{
    public static string GetStringSafe(this DbDataReader r, int ordinal)
    {
        if (r.IsDBNull(ordinal)) return string.Empty;
        var value = r.GetValue(ordinal);
        return value switch
        {
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => Convert.ToString(value) ?? string.Empty
        };
    }

    public static int? GetIntOrNull(this DbDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? null : Convert.ToInt32(r.GetValue(ordinal));

    public static int GetIntSafe(this DbDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? 0 : Convert.ToInt32(r.GetValue(ordinal));

    public static bool GetBoolSafe(this DbDataReader r, int ordinal)
        => !r.IsDBNull(ordinal) && Convert.ToBoolean(r.GetValue(ordinal));

    public static DateTime? GetDateOrNull(this DbDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? null : Convert.ToDateTime(r.GetValue(ordinal));
}
