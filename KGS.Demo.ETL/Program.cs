using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KGS.Demo.ETL;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cfg = BuildConfig();
        var connStr = cfg.GetConnectionString("KgsDemo")
                      ?? "Server=(localdb)\\MSSQLLocalDB;Database=KGS_Demo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

        var csvPath = args.Length > 0
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "KGS.Demo.ETL", "payroll_eligibility.csv"));

        Console.WriteLine($"Loading CSV: {csvPath}");
        var (rows, rejects) = LoadCsv(csvPath);
        Console.WriteLine($"Valid rows: {rows.Count}, Rejected: {rejects.Count}");

        if (rejects.Count > 0)
        {
            var rejPath = Path.Combine(Path.GetDirectoryName(csvPath)!, "payroll_eligibility.rejects.txt");
            await File.WriteAllLinesAsync(rejPath, rejects);
            Console.WriteLine($"Wrote rejects: {rejPath}");
        }

        var affected = await Upsert_NoTVP(rows, connStr, CancellationToken.None);
        Console.WriteLine($"Upsert affected statements: {affected}");
        Console.WriteLine("Done.");
    }

    private record PayrollRow(string EmployeeId, string FirstName, string LastName, bool Eligible, decimal LimitPerPayPeriod);

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    private static (List<PayrollRow> rows, List<string> rejects) LoadCsv(string path)
    {
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        };

        var rows = new List<PayrollRow>();
        var rejects = new List<string>();

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, cfg);

        try
        {
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                try
                {
                    var empId = csv.GetField("EmployeeId")?.Trim();
                    var first = csv.GetField("FirstName")?.Trim();
                    var last = csv.GetField("LastName")?.Trim();
                    var eligS = csv.GetField("Eligible")?.Trim();
                    var limitS = csv.GetField("LimitPerPayPeriod")?.Trim();

                    if (string.IsNullOrWhiteSpace(empId) ||
                        string.IsNullOrWhiteSpace(first) ||
                        string.IsNullOrWhiteSpace(last))
                        throw new Exception("Missing required field(s).");

                    if (!bool.TryParse(eligS, out var eligible))
                        throw new Exception($"Invalid Eligible: '{eligS}'.");

                    if (!decimal.TryParse(limitS, NumberStyles.Number, CultureInfo.InvariantCulture, out var limit))
                        throw new Exception($"Invalid LimitPerPayPeriod: '{limitS}'.");

                    rows.Add(new PayrollRow(empId, first, last, eligible, limit));
                }
                catch (Exception exRow)
                {
                    rejects.Add($"Line {csv.Parser!.RawRow}: {exRow.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            rejects.Add($"File error: {ex.Message}");
        }

        return (rows, rejects);
    }

    private static async Task<int> Upsert_NoTVP(IEnumerable<PayrollRow> data, string connStr, CancellationToken ct)
    {
        const string mergeOne = @"
MERGE dbo.PayrollEligibility AS T
USING (SELECT @EmployeeId AS EmployeeId, @FirstName AS FirstName, @LastName AS LastName,
              @Eligible AS Eligible, @Limit AS LimitPerPayPeriod) AS S
ON T.EmployeeId = S.EmployeeId
WHEN MATCHED THEN UPDATE SET
    T.FirstName = S.FirstName,
    T.LastName = S.LastName,
    T.Eligible = S.Eligible,
    T.LimitPerPayPeriod = S.LimitPerPayPeriod,
    T.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (EmployeeId, FirstName, LastName, Eligible, LimitPerPayPeriod, UpdatedAtUtc)
    VALUES (S.EmployeeId, S.FirstName, S.LastName, S.Eligible, S.LimitPerPayPeriod, SYSUTCDATETIME());";

        await using var cn = new SqlConnection(connStr);
        await cn.OpenAsync(ct);

        var affected = 0;
        foreach (var r in data)
        {
            using var cmd = new SqlCommand(mergeOne, cn);
            cmd.Parameters.Add(new SqlParameter("@EmployeeId", SqlDbType.NVarChar, 20) { Value = r.EmployeeId });
            cmd.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = r.FirstName });
            cmd.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 100) { Value = r.LastName });
            cmd.Parameters.Add(new SqlParameter("@Eligible", SqlDbType.Bit) { Value = r.Eligible });
            cmd.Parameters.Add(new SqlParameter("@Limit", SqlDbType.Decimal) { Value = r.LimitPerPayPeriod, Precision = 18, Scale = 2 });

            affected += await cmd.ExecuteNonQueryAsync(ct);
        }

        return affected;
    }
}
