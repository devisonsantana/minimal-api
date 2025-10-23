using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace minimal_api.Domain.Filters
{
    public class AuthenticationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            var allowAnonymous = endpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous) return;

            var hasAuthorize = endpointMetadata.OfType<AuthorizeAttribute>().Any();

            if (hasAuthorize)
            {
                operation.Security =
                [
                    new() {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            }, new string[] { }
                        }
                    }
                ];
            }
        }
    }
    public class VehicleResponseFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.RelativePath?.StartsWith("vehicle/{id}") == true &&
                context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true)
            {
                operation.Summary = "Get vehicle by ID";
                operation.Description = "Retrieves a specific vehicle by its unique identifier.";
                operation.Tags = new List<OpenApiTag>
                {
                    new(){Name ="Vehicles"}
                };
                operation.Parameters = [
                    new ()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Required = true,
                    Description = "The unique identifier of the vehicle",
                    Schema = new OpenApiSchema { Type = "integer" },
                    Example = new OpenApiInteger(1)
                }
                ];
                operation.Responses["200"] = new OpenApiResponse
                {
                    Description = "Vehicle found successfully",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["id"] = new OpenApiSchema { Type = "integer", Description = "Vehicle ID" },
                                    ["name"] = new OpenApiSchema { Type = "string", Description = "Vehicle name" },
                                    ["brand"] = new OpenApiSchema { Type = "string", Description = "Brand" },
                                    ["year"] = new OpenApiSchema { Type = "integer", Description = "Year of manufacture" }
                                }
                            },
                            Example = new OpenApiObject
                            {
                                ["id"] = new OpenApiInteger(1),
                                ["name"] = new OpenApiString("Civic"),
                                ["brand"] = new OpenApiString("Honda"),
                                ["year"] = new OpenApiInteger(2023)
                            }
                        }
                    }
                };
                operation.Responses["404"] = new OpenApiResponse
                {
                    Description = "Vehicle not found",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["message"] = new OpenApiString("Vehicle with ID 99 not found")
                            },
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["message"] = new OpenApiSchema { Type = "string" }
                                }
                            }
                        }
                    }
                };
                operation.Responses["400"] = new OpenApiResponse
                {
                    Description = "Invalid ID supplied",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["type"] = new OpenApiString("about:blank"),
                                ["title"] = new OpenApiString("Invalid parameter"),
                                ["status"] = new OpenApiInteger(400),
                                ["detail"] = new OpenApiString("Invalid ID parameter — must be greater than zero integer"),
                                ["providedValue"] = new OpenApiInteger(0)
                            },
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["type"] = new OpenApiSchema { Type = "string" },
                                    ["title"] = new OpenApiSchema { Type = "string" },
                                    ["status"] = new OpenApiSchema { Type = "integer" },
                                    ["detail"] = new OpenApiSchema { Type = "string" },
                                    ["providedValue"] = new OpenApiSchema { Type = "integer" }
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}