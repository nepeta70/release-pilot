namespace ReleasePilot.Infrastructure;

public static class DatabaseMigrator
{
    public static void EnsureDatabase(string connectionString)
    {
        using var conn = new Npgsql.NpgsqlConnection(connectionString);
        conn.Open();

        var asm = typeof(DatabaseMigrator).Assembly;
        var scripts = asm.GetManifestResourceNames()
            .Where(n => n.Contains(".Migrations.", StringComparison.OrdinalIgnoreCase) && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var name in scripts)
        {
            using var stream = asm.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Embedded migration not found: {name}");
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(sql)) continue;

            try
            {
                using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Migration failed while executing {name}", ex);
            }
        }
    }
}
