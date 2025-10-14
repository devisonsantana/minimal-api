using System.Text.Json;
using minimal_api.Domain.Enums;

namespace minimal_api.Domain.Exceptions
{
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
        {
            var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();

            try
            {
                await next();
            }
            catch (InvalidEnumValueException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails =
                    {
                        Title = "Invalid value for enum",
                        Detail = ex.Message,
                        Status = 400,
                        Extensions =
                        {
                            ["enumType"] = ex.EnumType,
                            ["providedValue"] = ex.ProvidedValue,
                            ["allowedValues"] = Enum.GetNames<Role>()
                        }
                    },
                    Exception = ex
                });
            }
            catch (InvalidUserValuesException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails =
                    {
                        Title = "User validation error",
                        Detail = "There are validation errors",
                        Status = 400,
                        Extensions =
                        {
                            ["errors"] = ex.Errors
                        }
                    },
                    Exception = ex
                });
            }
            catch (InvalidPageNumberException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails =
                    {
                        Title = "Invalid parameter",
                        Detail = ex.Message,
                        Status = 400,
                        Extensions =
                        {
                            ["providedValue"] = ex.ProvidedValue
                        }
                    },
                    Exception = ex
                });
            }
            catch (JsonException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails =
                    {
                        Title = "Error deserializing JSON",
                        Detail = ex.Message,
                        Status = 400
                    },
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails =
                    {
                        Title = "Internal server error",
                        Detail = ex.Message,
                        Status = 500
                    },
                    Exception = ex
                });
            }
        });

            return app;
        }
    }
}