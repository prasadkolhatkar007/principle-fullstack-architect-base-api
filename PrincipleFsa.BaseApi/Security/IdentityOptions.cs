namespace PrincipleFsa.BaseApi.Security;

public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    public string Authority { get; init; } = "";

    public string Audience { get; init; } = "";

    public bool RequireHttpsMetadata { get; init; } = true;
}

