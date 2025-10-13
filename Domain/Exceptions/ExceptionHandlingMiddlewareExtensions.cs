using System.Text.Json;

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