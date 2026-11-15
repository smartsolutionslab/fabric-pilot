namespace FabricPilot.BuildingBlocks.Modules;

public static class PermissionMappings
{
    /// <summary>The permission a module's policy name stands for.</summary>
    public static PermissionDto ToDto(this string policyName)
    {
        Ensure.That(policyName).IsNotNull();
        return new PermissionDto(policyName);
    }
}
