namespace ReleasePilot.Domain.ValueObjects;

public enum DeploymentEnvironment
{
    Dev = 1,
    Staging = 2,
    Production = 3
}

public static class EnvironmentExtensions
{
    public static DeploymentEnvironment? GetRequiredPrevious(this DeploymentEnvironment current) =>
        current switch
        {
            DeploymentEnvironment.Staging => DeploymentEnvironment.Dev,
            DeploymentEnvironment.Production => DeploymentEnvironment.Staging,
            _ => null
        };
}