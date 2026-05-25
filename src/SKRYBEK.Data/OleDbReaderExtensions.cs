using System.Data.Common;

namespace SKRYBEK.Data;

internal static class OleDbReaderExtensions
{
    public static string GetStringSafe(this DbDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? string.Empty : r.GetString(ordinal);

    public static int? GetIntOrNull(this DbDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? null : Convert.ToInt32(r.GetValue(ordinal));

    public static int GetIntSafe(this DbDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? 0 : Convert.ToInt32(r.GetValue(ordinal));

    public static bool GetBoolSafe(this DbDataReader r, int ordinal)
        => !r.IsDBNull(ordinal) && Convert.ToBoolean(r.GetValue(ordinal));

    public static DateTime? GetDateOrNull(this DbDataReader r, int ordinal)
        => r.IsDBNull(ordinal) ? null : Convert.ToDateTime(r.GetValue(ordinal));
}
