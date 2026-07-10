namespace Adeeb.Api.Configuration;

public class DatabaseInitializationOptions
{
    public bool AutoMigrate { get; set; } = false;
    public bool Seed { get; set; } = false;
}
