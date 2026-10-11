namespace CXView.Infrastructure;

public sealed class DatabaseOptions
{
    public const string Section = "Database";
    public string Provider { get; init; } = "SqlServer";
    public string ConnectionString { get; init; } = "";
    public string ProviderInvariantName => Provider.ToLowerInvariant() switch
    {
        "sqlserver" => "Microsoft.Data.SqlClient",
        "mysql" => "MySqlConnector",
        "inmemory" => "InMemory",
        _ => throw new InvalidOperationException($"Proveedor de base de datos no soportado: {Provider}")
    };
}
