using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using MySql.Data.MySqlClient;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.DatabaseInitializer;

public sealed class Function
{
    public async Task<InitializationResult> FunctionHandler(InitializationRequest? request, ILambdaContext context)
    {
        if (request?.Action is not ("initialize" or "migrate-banner-slots" or "seed-dev-samples"))
            throw new ArgumentException("action must be exactly 'initialize', 'migrate-banner-slots' or 'seed-dev-samples'.", nameof(request));

        if (request.Action == "seed-dev-samples" && Environment.GetEnvironmentVariable("STAGE") != "dev")
            throw new InvalidOperationException("Sample data can only be seeded in the explicit development stage (STAGE=dev).");

        var host = RequiredEnvironmentVariable("DB_HOST");
        var database = RequiredEnvironmentVariable("DB_NAME");
        var secretArn = RequiredEnvironmentVariable("DB_SECRET_ARN");
        var portText = RequiredEnvironmentVariable("DB_PORT");
        if (!uint.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is 0 or > 65535)
            throw new InvalidOperationException("DB_PORT must be an integer between 1 and 65535.");

        var timeout = context.RemainingTime - TimeSpan.FromSeconds(5);
        if (timeout <= TimeSpan.Zero)
            throw new TimeoutException("Insufficient time remaining to initialize the database.");
        using var cancellation = new CancellationTokenSource(timeout);
        var cancellationToken = cancellation.Token;

        try
        {
            using var secrets = new AmazonSecretsManagerClient();
            var secret = await secrets.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretArn }, cancellationToken);
            DbCredentials? credentials;
            try
            {
                credentials = JsonSerializer.Deserialize<DbCredentials>(secret.SecretString ?? "{}");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("The database secret must be a JSON object with username and password strings.");
            }

            if (string.IsNullOrWhiteSpace(credentials?.Username) || string.IsNullOrEmpty(credentials.Password))
                throw new InvalidOperationException("The database secret must contain username and password.");

            var connectionString = new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = port,
                Database = database,
                UserID = credentials.Username,
                Password = credentials.Password,
                CharacterSet = "utf8mb4",
                SslMode = MySqlSslMode.Required,
                ConnectionTimeout = 30,
                Pooling = false
            };

            await using var connection = new MySqlConnection(connectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // A session lock prevents two manual invocations passing the empty check together.
            // Pooling is disabled, so closing this connection also releases its lock.
            await using var acquireLock = new MySqlCommand("SELECT GET_LOCK('galashow_schema_v001_initialize', 0)", connection);
            if (Convert.ToInt32(await acquireLock.ExecuteScalarAsync(cancellationToken)) != 1)
                throw new InvalidOperationException("Another schema initialization is in progress.");

            var existingTableCount = await CountTablesAsync(connection, cancellationToken);
            if (request.Action == "initialize")
            {
                if (existingTableCount != 0)
                    throw new InvalidOperationException("Initialization requires an empty database. Existing tables require an explicit migration review.");

                // Only packaged SQL is executed. MySQL DDL can leave a partial schema on failure.
                var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"), cancellationToken);
                await using var command = new MySqlCommand(schema, connection) { CommandTimeout = 120 };
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var tableCount = await CountTablesAsync(connection, cancellationToken);
            if (tableCount != 11)
                throw new InvalidOperationException("The database must contain the expected 11 tables before applying v002. Review the database before retrying.");

            var bannerSlotCount = await EnsureBannerSlotsAsync(connection, cancellationToken);
            if (request.Action == "seed-dev-samples")
            {
                var counts = await SampleSeeder.SeedAsync(connection, cancellationToken);
                context.Logger.LogLine("Development sample data v1 seeded; existing nonempty content preserved.");
                return new InitializationResult("v002", "samples-seeded", tableCount, bannerSlotCount, counts);
            }

            var result = request.Action == "initialize" ? "initialized" : "migrated";
            context.Logger.LogLine($"Database v002 {result} with 11 tables and {bannerSlotCount} banner slots.");
            return new InitializationResult("v002", result, tableCount, bannerSlotCount);
        }
        catch (MySqlException exception)
        {
            // Driver messages may contain user/host details. Do not return them or an inner exception.
            throw new InvalidOperationException($"Database initialization failed (MySQL error {exception.Number}). Review the database before retrying.");
        }
        catch (AmazonSecretsManagerException)
        {
            throw new InvalidOperationException("Unable to read the configured database secret. Check the Lambda role, secret, and VPC network path.");
        }
    }

    private static async Task<int> EnsureBannerSlotsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "v002_banner_slots.sql"), cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection, transaction) { CommandTimeout = 30 };
        await command.ExecuteNonQueryAsync(cancellationToken);

        await using var count = new MySqlCommand("SELECT COUNT(*) FROM banners WHERE id BETWEEN 1 AND 10", connection, transaction);
        var slotCount = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));
        if (slotCount != 10)
            throw new InvalidOperationException("Banner migration did not produce all 10 required slots.");

        await transaction.CommitAsync(cancellationToken);
        return slotCount;
    }

    private static async Task<int> CountTablesAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static string RequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{name} is required.")
            : value;
    }

    private sealed record DbCredentials
    {
        [JsonPropertyName("username")]
        public string? Username { get; init; }

        [JsonPropertyName("password")]
        public string? Password { get; init; }
    }
}

public sealed record InitializationRequest
{
    [JsonPropertyName("action")]
    public string? Action { get; init; }
}

public sealed record InitializationResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("result")] string Result,
    [property: JsonPropertyName("tableCount")] int TableCount,
    [property: JsonPropertyName("bannerSlotCount")] int BannerSlotCount,
    [property: JsonPropertyName("tableRowCounts"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    Dictionary<string, int>? TableRowCounts = null);
