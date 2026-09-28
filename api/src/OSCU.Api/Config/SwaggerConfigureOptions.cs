using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
// OpenAPI.NET v2 (which Swashbuckle 10 uses) moved OpenApiInfo out of the
// Microsoft.OpenApi.Models namespace and up into Microsoft.OpenApi. If this
// import fails to resolve after restore, check the Swashbuckle major version.
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OSCU.Api.Config;

/// <summary>
/// Generates one Swagger document per discovered API version.
/// </summary>
public class SwaggerConfigureOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Referral and Case Management API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "This API version has been deprecated."
                    : "Referral and case management for the Home Office."
            });
        }
    }
}
