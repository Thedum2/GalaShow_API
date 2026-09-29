using MySql.Data.MySqlClient;

namespace GalaShow.DatabaseInitializer;

internal static class SampleSeeder
{
    public static async Task<Dictionary<string, int>> SeedAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "dev-samples-v1.sql"), cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection, transaction) { CommandTimeout = 60 };
        command.Parameters.AddWithValue("@assetBaseUrl", "https://dev.galashow.cloud/sample-data/v1/");
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Fixed table names only; request data never supplies SQL or asset URLs.
        var counts = new Dictionary<string, int>();
        foreach (var table in new[] { "banners", "background", "policies", "sns_links", "minigames",
                     "minigame_tags", "minigame_tutorials", "minigame_controls", "minigame_survival_stats", "viewer_avatars" })
        {
            await using var count = new MySqlCommand($"SELECT COUNT(*) FROM `{table}`", connection, transaction);
            counts[table] = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        return counts;
    }
}
