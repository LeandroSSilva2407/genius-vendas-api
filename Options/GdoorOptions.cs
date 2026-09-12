namespace GeniusVendas.Api.Options;

public sealed class GdoorOptions
{
    public string Mode { get; set; } = "Mock";
    public string ConnectionString { get; set; } = "";
    public GdoorSqlOptions Sql { get; set; } = new();
}

public sealed class GdoorSqlOptions
{
    public string Login { get; set; } = "";
    public string Products { get; set; } = "";
    public string Customers { get; set; } = "";
    public string ProductByCode { get; set; } = "";
}
