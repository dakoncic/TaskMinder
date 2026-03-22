using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyFeatures.Helpers
{
    public class CustomOperationIdFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            operation.OperationId = $"{actionName}";
        }
    }
}
