using System.Data.OleDb;

namespace SKRYBEK.Data;

/// <summary>
/// Jawne typy parametrów dla Microsoft ACE OLE DB (unika błędu 3464).
/// </summary>
internal static class OleDbParameterExtensions
{
    public static void AddInteger(this OleDbParameterCollection parameters, object value)
    {
        parameters.Add(new OleDbParameter { OleDbType = OleDbType.Integer, Value = value });
    }

    public static void AddSmallInt(this OleDbParameterCollection parameters, object value)
    {
        parameters.Add(new OleDbParameter { OleDbType = OleDbType.SmallInt, Value = value });
    }

    public static void AddDate(this OleDbParameterCollection parameters, DateTime value)
    {
        parameters.Add(new OleDbParameter { OleDbType = OleDbType.Date, Value = value });
    }

    public static void AddMemo(this OleDbParameterCollection parameters, string? value)
    {
        parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.LongVarWChar,
            Value = string.IsNullOrEmpty(value) ? DBNull.Value : value
        });
    }

    public static void AddText(this OleDbParameterCollection parameters, string? value)
    {
        parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.VarWChar,
            Value = string.IsNullOrEmpty(value) ? DBNull.Value : value
        });
    }

    public static void AddNullableInteger(this OleDbParameterCollection parameters, int? value)
    {
        parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.Integer,
            Value = value.HasValue ? value.Value : DBNull.Value
        });
    }
}
