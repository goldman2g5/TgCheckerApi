using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TgCheckerApi.MiddleWare
{
    public class ApiKeyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-API-KEY",
                In = ParameterLocation.Header,
                Description = "API Key needed to access endpoints",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "String",
                    Default = new OpenApiString("7bdf1ca44d84484c9864c06c0aedc1beb740909b02e4404ebafd381db897e1a5387567f8b42f47c7b5192eac60547460e0003c11fd804d1a966a30eacd939a3acaa9a352797f436aad6cd14f27517554")
                }
            });
        }
    }
}
