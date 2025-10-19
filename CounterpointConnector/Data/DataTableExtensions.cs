using System.Data;
using CounterpointConnector.Models;

namespace CounterpointConnector.Data;

public static class DataTableExtensions
{
    public static DataTable ToTicketLineInputTvp(this IEnumerable<TicketLineDto> lines)
    {
        var table = new DataTable();
        table.Columns.Add("Sku", typeof(string));
        table.Columns.Add("Qty", typeof(int));
        table.Columns.Add("OverridePrice", typeof(decimal));
        foreach (var l in lines)
        {
            var row = table.NewRow();
            row["Sku"] = l.Sku;
            row["Qty"] = l.Qty;
            row["OverridePrice"] = (object?)l.OverridePrice ?? DBNull.Value;
            table.Rows.Add(row);
        }
        return table;
    }
}
