namespace ReleasePilot.Infrastructure;

public static class DatabaseMigrator
{
    private const string createTableSql = @"
        CREATE TABLE IF NOT EXISTS schema_migrations (
            filename varchar(255) PRIMARY KEY,
            applied_at timestamp DEFAULT CURRENT_TIMESTAMP
        );";
    public static void EnsureDatabase(string connectionString)
    {
        using var conn = new Npgsql.NpgsqlConnection(connectionString);
        conn.Open();

        using (var cmd = new Npgsql.NpgsqlCommand(createTableSql, conn))
        {
            cmd.ExecuteNonQuery();
        }

        var asm = typeof(DatabaseMigrator).Assembly;
        var scripts = asm.GetManifestResourceNames()
            .Where(n => n.Contains(".Migrations.") && n.EndsWith(".sql"))
            .OrderBy(n => n)
            .ToList();

        foreach (var name in scripts)
        {
            using (var checkCmd = new Npgsql.NpgsqlCommand(
                        "SELECT EXISTS(SELECT 1 FROM schema_migrations WHERE filename = @name)", conn))
            {
                checkCmd.Parameters.AddWithValue("name", name);
                if ((bool)(checkCmd.ExecuteScalar() ?? false)) continue;
            }

            using var reader = new StreamReader(asm.GetManifestResourceStream(name)!);
            var sql = reader.ReadToEnd();

            using var transaction = conn.BeginTransaction();
            try
            {
                using var cmd = new Npgsql.NpgsqlCommand(sql, conn, transaction);
                cmd.ExecuteNonQuery();

                using var logCmd = new Npgsql.NpgsqlCommand(
                    "INSERT INTO schema_migrations (filename) VALUES (@name)", conn, transaction);
                logCmd.Parameters.AddWithValue("name", name);
                logCmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Migration failed: {name}. Transaction rolled back.", ex);
            }
        }
    }
}
