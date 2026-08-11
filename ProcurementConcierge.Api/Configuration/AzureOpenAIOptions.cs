namespace ProcurementConcierge.Api.Configuration;

/// <summary>
/// Azure OpenAI connection settings, bound from the "AzureOpenAI" section of appsettings.json.
/// </summary>
public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}
