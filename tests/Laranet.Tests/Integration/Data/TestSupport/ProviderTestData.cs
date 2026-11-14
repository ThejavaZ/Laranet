namespace Laranet.Tests.Integration.Data.TestSupport;

/// <summary>
/// Selects the database provider used by the provider compatibility tests.
/// </summary>
public static class ProviderTestData
{
    public const string ProviderVariable = "LARANET_TEST_PROVIDER";
    public const string SqlServerConnectionVariable = "LARANET_SQLSERVER_CONNECTION";
    public const string PostgreSqlConnectionVariable = "LARANET_POSTGRES_CONNECTION";
    public const string MySqlConnectionVariable = "LARANET_MYSQL_CONNECTION";

    public static IEnumerable<object[]> Providers()
    {
        yield return [Resolve()];
    }

    public static DataTestProvider Resolve()
    {
        var value = Environment.GetEnvironmentVariable(ProviderVariable);

        return value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "sqlite" => DataTestProvider.Sqlite,
            "sqlserver" => DataTestProvider.SqlServer,
            "postgresql" or "postgres" => DataTestProvider.PostgreSql,
            "mysql" or "mariadb" => DataTestProvider.MySql,
            _ => throw new InvalidOperationException(
                $"Unsupported {ProviderVariable} value '{value}'. " +
                "Use 'sqlite', 'sqlserver', 'postgresql', or 'mysql'."),
        };
    }
}
