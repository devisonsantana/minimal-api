using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Models;
using minimal_api.Domain.DTOs;
using minimal_api.Domain.Entities;
using minimal_api.Domain.Enums;
using minimal_api.Domain.Exceptions;
using minimal_api.Domain.Filters;
using minimal_api.Domain.Interfaces;
using minimal_api.Domain.ModelViews;
using minimal_api.Domain.Services;
using minimal_api.Infrastructure.Db;

namespace minimal_api;

public class Program
{
    public static void Main(string[] args)
    {
        #region Builder
        var builder = WebApplication.CreateBuilder(args);

        var key = builder.Configuration["Jwt"];
#if DEBUG
        if (string.IsNullOrEmpty(key)) key = "CHAVE-SECRETA-APENAS-PARA-DESENVOLVIMENTO";
#else
        if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("JWT key is not configured.");
#endif
        builder.Services.AddAuthentication(option =>
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = false,
                ValidateAudience = false,
                RoleClaimType = ClaimTypes.Role
            };
        });
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IVehicleService, VehicleService>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Vehicle Management API",
                Version = "v1",
                Description = "API for managing users and vehicles with JWT authentication",
                Contact = new OpenApiContact
                {
                    Name = "Devison Santana",
                    Email = "dev.devisonsan@hotmail.com"
                }
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insert a JWT token (ex: <your_jwt_token>)"
            });
            options.OperationFilter<AuthenticationFilter>();
            // options.UseAllOfToExtendReferenceSchemas();
            // options.SchemaGeneratorOptions.UseInlineDefinitionsForEnums = true;
        });

        builder.Services.AddDbContext<DatabaseContext>(options =>
        {
            options.UseMySql(
                builder.Configuration.GetConnectionString("mysql"),
                ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
            );
        });

        builder.Services.AddProblemDetails();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.Converters.Add(new StrictStringEnumConverter<Role>());
        });

        var app = builder.Build();

        app.UseGlobalExceptionHandler();

        #endregion

        #region Validator
        ErrorValidation ValidateVehicleDTO(VehicleDTO vehicleDTO)
        {
            var validations = new ErrorValidation();
            if (string.IsNullOrEmpty(vehicleDTO.Name))
                validations.Messages.Add("Vehicle name cannot be empty");
            if (string.IsNullOrEmpty(vehicleDTO.Brand))
                validations.Messages.Add("Vehicle brand cannot be empty");
            if (vehicleDTO.Year < 1769)
                validations.Messages.Add($"{vehicleDTO.Name}'s year cannot be too old, just above 1769");
            if (vehicleDTO.Year >= DateTime.Now.Year)
                validations.Messages.Add($"{vehicleDTO.Name}'s year cannot be in the future");
            return validations;
        }
        #endregion

        #region Token Generator
        string GenerateTokenJWT(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>()
            {
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
                // new Claim("email", user.Email),
                // new Claim("role", user.Role)
            };
            var token = new JwtSecurityToken(
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials,
                claims: claims
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion

        #region Home endpoint
        app.MapGet("/", () => Results.Json(new Home())).WithOpenApi(operation => new OpenApiOperation
        {
            Summary = "Home page",
            Description = "Returns a welcome message and a link to the API documentation. Useful as a health or info endpoint.",
            Tags = [ new OpenApiTag{
                Name = "Home"
            }],
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "Home page loaded successfully",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["message"] = new OpenApiString("Welcome to Vehicle Minimal API, feel free to test our endpoints, some requests require a token authentication"),
                                ["documentation"] = new OpenApiString("/swagger/index.html")
                            },
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["message"] = new OpenApiSchema { Type = "string" },
                                    ["documentation"] = new OpenApiSchema { Type = "string" }
                                }
                            }
                        }
                    }
                }
            }
        }).Produces<Home>(statusCode: StatusCodes.Status200OK);
        #endregion

        #region Sign-up and Sign-in endpoint
        app.MapPost("/signup", ([FromBody] UserDTO userDTO, IUserService service) =>
            {
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(userDTO.Email))
                    validationErrors.Add("The 'email' field is required");

                if (string.IsNullOrWhiteSpace(userDTO.Password))
                    validationErrors.Add("The 'password' field is required");

                if (validationErrors.Count > 0)
                {
                    throw new InvalidUserValuesException(validationErrors);
                }
                var user = new User
                {
                    Email = userDTO.Email,
                    Password = userDTO.Password,
                    Role = userDTO.Role.ToString()
                };
                var usrView = new UserModelView(service.Save(user));
                return Results.Created($"/user/{usrView.Id}", usrView);
            }).WithOpenApi(operation => new OpenApiOperation
            {
                Summary = "Register a new user",
                Description = "Creates a new user with the specified role (ADMIN or EDITOR)",
                Tags = [new OpenApiTag { Name = "Signup/Login" }],
                RequestBody = new OpenApiRequestBody
                {
                    Description = "User data to create new account",
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                                    ["password"] = new OpenApiSchema { Type = "string", Format = "password" },
                                    ["role"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Enum = new List<IOpenApiAny>
                                        {
                                            new OpenApiString("ADMIN"),
                                            new OpenApiString("EDITOR")
                                        }
                                    }
                                },
                                Required = new HashSet<string> { "email", "password", "role" }
                            },
                            Example = new OpenApiObject
                            {
                                ["email"] = new OpenApiString("johndoe@example.com"),
                                ["password"] = new OpenApiString("your-password"),
                                ["role"] = new OpenApiString("EDITOR")
                            }
                        }
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["201"] = new OpenApiResponse
                    {
                        Description = "User created successfully",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Example = new OpenApiObject
                                {
                                    ["id"] = new OpenApiInteger(1),
                                    ["email"] = new OpenApiString("johndoe@example.com"),
                                    ["role"] = new OpenApiString("EDITOR")
                                },
                                Schema = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        ["id"] = new OpenApiSchema { Type = "integer" },
                                        ["email"] = new OpenApiSchema { Type = "string" },
                                        ["role"] = new OpenApiSchema { Type = "string" }
                                    }
                                }
                            }
                        }
                    },
                    ["400"] = new OpenApiResponse
                    {
                        Description = "Invalid Request",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Examples = new Dictionary<string, OpenApiExample>
                                {
                                    ["InvalidEnumValue"] = new()
                                    {
                                        Summary = "Invalid enum",
                                        Description = "The value provided is not a valid Role enum name.",
                                        Value = new OpenApiString(
                                            JsonSerializer.Serialize(new
                                            {
                                                type = "about:blank",
                                                title = "Invalid value for enum",
                                                status = 400,
                                                detail = "Value 'guest' not valid for enum Role.",
                                                enumType = "Role",
                                                providedValue = "guest",
                                                allowedValues = new[]
                                                {
                                                    "ADMIN",
                                                    "EDITOR"
                                                }
                                            }
                                        ))
                                    },
                                    ["ValidationErrors"] = new()
                                    {
                                        Summary = "User validation error",
                                        Description = "Occurs when mandatory information is missing or invalid",
                                        Value = new OpenApiString(
                                            JsonSerializer.Serialize(new
                                            {
                                                type = "about:blank",
                                                title = "User validation error",
                                                status = 400,
                                                detail = "There are validation errors.",
                                                errors = new[]
                                                {
                                                    "The 'email' field is required",
                                                    "The 'password' field is required"
                                                }
                                            }
                                        ))
                                    }
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
                                        ["errors"] = new OpenApiSchema
                                        {
                                            Type = "array",
                                            Items = new OpenApiSchema { Type = "string" },
                                            Description = "List of validation messages (optional)"
                                        },
                                        ["enumType"] = new OpenApiSchema { Type = "string", Description = "Enum type name (if applicable)" },
                                        ["providedValue"] = new OpenApiSchema { Type = "string" },
                                        ["allowedValues"] = new OpenApiSchema
                                        {
                                            Type = "array",
                                            Items = new OpenApiSchema { Type = "string" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            })
            .AllowAnonymous();

        app.MapPost("/login", ([FromBody] LoginDTO loginDTO, IUserService service) =>
            {
                var usr = service.Login(loginDTO);
                if (usr is not null)
                {
                    var token = GenerateTokenJWT(usr);
                    return Results.Ok(new UserSignedModelView
                    {
                        Email = usr.Email,
                        Role = usr.Role,
                        Token = token
                    });
                }
                throw new LoginCredentialsException();
            }).WithOpenApi(operation => new OpenApiOperation
            {
                Summary = "Authenticate an existing user",
                Description = "Validates the provided credentials and returns a JWT token if successful",
                Tags = [new OpenApiTag { Name = "Signup/Login" }],
                RequestBody = new OpenApiRequestBody
                {
                    Description = "Login credentials (email and password)",
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                                    ["password"] = new OpenApiSchema { Type = "string", Format = "password" }
                                },
                                Required = new HashSet<string> { "email", "password" }
                            },
                            Example = new OpenApiObject
                            {
                                ["email"] = new OpenApiString("johndoe@example.com"),
                                ["password"] = new OpenApiString("your-password")
                            }
                        }
                    }
                },
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse
                    {
                        Description = "Login successfully, returns JWT token",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Example = new OpenApiObject
                                {
                                    ["email"] = new OpenApiString("johndoe@example.com"),
                                    ["role"] = new OpenApiString("EDITOR"),
                                    ["token"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")
                                }
                            }
                        }
                    },
                    ["401"] = new OpenApiResponse
                    {
                        Description = "Not recognized credentials",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Example = new OpenApiObject
                                {
                                    ["type"] = new OpenApiString("about:blank"),
                                    ["title"] = new OpenApiString("Credential error"),
                                    ["status"] = new OpenApiInteger(401),
                                    ["detail"] = new OpenApiString("Invalid email or password"),
                                },
                                Schema = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        ["type"] = new OpenApiSchema { Type = "string" },
                                        ["title"] = new OpenApiSchema { Type = "string" },
                                        ["status"] = new OpenApiSchema { Type = "integer" },
                                        ["detail"] = new OpenApiSchema { Type = "string" }
                                    }
                                }
                            }
                        }
                    }
                }
            }).Produces<UserSignedModelView>(StatusCodes.Status200OK)
            .AllowAnonymous();
        #endregion

        #region User endpoint
        app.MapGet("/user", ([FromQuery] int? page, IUserService service) =>
        {
            var users = new List<UserModelView>();
            service.FindAll(page ??= 1).ForEach(usr =>
            {
                users.Add(new UserModelView
                {
                    Id = usr.Id,
                    Email = usr.Email,
                    Role = usr.Role
                });
            });
            return Results.Ok(users);
        }).WithOpenApi(operation => new OpenApiOperation
        {
            Summary = "List all users created",
            Description = "Returns a paginated list of users. Access is restricted to users with the ADMIN role.",
            Tags = [new OpenApiTag { Name = "User" }],
            Parameters =
            [
                new()
                {
                    Name = "page",
                    In = ParameterLocation.Query,
                    Description= "Number of the page (opcional, default = 1)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(1) }
                }
            ],
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "List of users successfully retrieved",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiArray
                            {
                                new OpenApiObject
                                {
                                    ["id"] = new OpenApiInteger(1),
                                    ["email"] = new OpenApiString("johndoe@example.com"),
                                    ["role"] = new OpenApiString("EDITOR")
                                }
                            }
                        }
                    }
                },
                ["401"] = new OpenApiResponse
                { Description = "Unauthorized - Missing or invalid JWT token" },
                ["403"] = new OpenApiResponse
                { Description = "Forbidden - User does not have ADMIN role" }
            }
        }).Produces<UserModelView>(StatusCodes.Status200OK)
        .RequireAuthorization(new AuthorizeAttribute { Roles = nameof(Role.ADMIN) });

        app.MapGet("/user/{id}", ([FromRoute] int id, IUserService service) =>
        {
            var usr = service.FindById(id);
            if (usr == null) return Results.NotFound(new { message = $"Not Found - User with ID {id} not found." });
            return Results.Ok(new UserModelView { Id = usr.Id, Email = usr.Email, Role = usr.Role });
        }).WithOpenApi(operation => new OpenApiOperation
        {
            Summary = "Get user by ID",
            Description = "Retrieves a single user by their unique identifier. Access is restricted to users with the ADMIN role.",
            Tags = [new OpenApiTag { Name = "User" }],
            Parameters =
            [
                new()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Description= "Unique identifier of the user to retrieve",
                    Required = true,
                    Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(1) }
                }
            ],
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "User successfully retrieved",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["id"] = new OpenApiInteger(1),
                                ["email"] = new OpenApiString("johndoe@example.com"),
                                ["role"] = new OpenApiString("EDITOR")
                            }
                        }
                    }
                },
                ["404"] = new OpenApiResponse
                {
                    Description = "User not found",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["message"] = new OpenApiString("Not Found - User with ID 99 not found.")
                            }
                        }
                    }
                },
                ["401"] = new OpenApiResponse { Description = "Unauthorized - Missing or invalid JWT token" },
                ["403"] = new OpenApiResponse { Description = "Forbidden - User does not have ADMIN role" }
            }
        }).Produces<UserModelView>(StatusCodes.Status200OK)
        .RequireAuthorization(new AuthorizeAttribute { Roles = nameof(Role.ADMIN) });
        #endregion

        #region Vehicle endpoint
        app.MapPost("/vehicle", ([FromBody] VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
        {
            var validation = ValidateVehicleDTO(vehicleDTO);
            if (validation.Messages.Count > 0)
                return Results.BadRequest(validation);

            var vehicle = new Vehicle
            {
                Name = vehicleDTO.Name,
                Brand = vehicleDTO.Brand,
                Year = vehicleDTO.Year
            };

            vehicleService.Save(vehicle);

            return Results.Created($"/vehicle/{vehicle.Id}", new VehicleModelView(vehicle));
        }).WithOpenApi(operation => new OpenApiOperation
        {
            Summary = "Register a new vehicle",
            Description = "Creates a new vehicle entry. Only users with ADMIN or EDITOR roles can access this endpoint.",
            Tags = [new() { Name = "Vehicles" }],
            RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Description = "Vehicle data to be registered",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["name"] = new OpenApiSchema { Type = "string", Description = "Vehicle model name" },
                                ["brand"] = new OpenApiSchema { Type = "string", Description = "Manufacturer brand" },
                                ["year"] = new OpenApiSchema { Type = "integer", Description = "Year of manufacture" }
                            },
                            Required = new HashSet<string> { "name", "brand", "year" }
                        },
                        Example = new OpenApiObject
                        {
                            ["name"] = new OpenApiString("Civic"),
                            ["brand"] = new OpenApiString("Honda"),
                            ["year"] = new OpenApiInteger(2023)
                        }
                    }
                }
            },
            Responses = new OpenApiResponses
            {
                ["201"] = new OpenApiResponse
                {
                    Description = "Vehicle successfully created",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["id"] = new OpenApiInteger(1),
                                ["name"] = new OpenApiString("Civic"),
                                ["brand"] = new OpenApiString("Honda"),
                                ["year"] = new OpenApiInteger(2023)
                            },
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["id"] = new OpenApiSchema { Type = "integer" },
                                    ["name"] = new OpenApiSchema { Type = "string" },
                                    ["brand"] = new OpenApiSchema { Type = "string" },
                                    ["year"] = new OpenApiSchema { Type = "integer" }
                                }
                            }
                        }
                    }
                },
                ["400"] = new OpenApiResponse
                {
                    Description = "Vehicle error validation",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["messages"] = new OpenApiArray
                                {
                                    new OpenApiString("Vehicle name cannot be empty"),
                                    new OpenApiString("Vehicle brand cannot be empty"),
                                    new OpenApiString("Civic's year cannot be too old, just above 1769"),
                                    new OpenApiString("Civic's year cannot be in the future")
                                }
                            }
                        }
                    }
                },
                ["401"] = new OpenApiResponse { Description = "Unauthorized — missing or invalid token" },
                ["403"] = new OpenApiResponse { Description = "Forbidden — user does not have the required role (ADMIN or EDITOR)" }

            }
        }).Produces<VehicleModelView>(StatusCodes.Status201Created)
        .Produces<ErrorValidation>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{nameof(Role.ADMIN)},{nameof(Role.EDITOR)}" });

        app.MapPost("/vehicles", ([FromBody] List<VehicleDTO> vehicleDTOs, IVehicleService vehicleService) =>
        {
            var vehicles = new List<Vehicle>();
            foreach (var v in vehicleDTOs)
            {
                var validation = ValidateVehicleDTO(v);
                if (validation.Messages.Count > 0)
                {
                    return Results.BadRequest(validation);
                }
                vehicles.Add(new Vehicle
                {
                    Name = v.Name,
                    Brand = v.Brand,
                    Year = v.Year
                });
            }

            vehicleService.SaveAll(vehicles);
            var response = vehicles.Select(v => new VehicleModelView(v)).ToList();
            return Results.Created($"/vehicle", response);
        }).WithOpenApi(operation => new OpenApiOperation
        {
            Summary = "Register a list of vehicles",
            Description = "Creates a new vehicles entry, recommended for high amount of vehicle data. Only users with ADMIN or EDITOR roles can access this endpoint.",
            Tags = [new OpenApiTag { Name = "Vehicles" }],
            RequestBody = new OpenApiRequestBody
            {
                Description = "List of vehicle data to be registered",
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiArray
                        {
                            new OpenApiObject
                            {
                                ["name"] = new OpenApiString("Civic"),
                                ["brand"] = new OpenApiString("Honda"),
                                ["year"] = new OpenApiInteger(2023)
                            }
                        },
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["name"] = new OpenApiSchema { Type = "string", Description = "Vehicle model name" },
                                ["brand"] = new OpenApiSchema { Type = "string", Description = "Manufacturer brand" },
                                ["year"] = new OpenApiSchema { Type = "integer", Description = "Year of manufacture" }
                            },
                            Required = new HashSet<string> { "name", "brand", "year" }
                        }
                    }
                }
            },
            Responses = new OpenApiResponses
            {
                ["201"] = new OpenApiResponse
                {
                    Description = "Vehicles sucessfully created",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiArray
                            {
                                new OpenApiObject
                                {
                                    ["id"] = new OpenApiInteger(1),
                                    ["name"] = new OpenApiString("Civic"),
                                    ["brand"] = new OpenApiString("Honda"),
                                    ["year"] = new OpenApiInteger(2023)
                                }
                            }
                        }
                    }
                },
                ["400"] = new OpenApiResponse
                {
                    Description = "Vehicle error validation",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["messages"] = new OpenApiArray
                                {
                                    new OpenApiString("Vehicle name cannot be empty"),
                                    new OpenApiString("Vehicle brand cannot be empty"),
                                    new OpenApiString("Civic's year cannot be too old, just above 1769"),
                                    new OpenApiString("Civic's year cannot be in the future")
                                }
                            },
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["message"] = new OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new OpenApiSchema { Type = "string" }
                                    }
                                }
                            }
                        }
                    }
                },
                ["401"] = new OpenApiResponse { Description = "Unauthorized — missing or invalid token" },
                ["403"] = new OpenApiResponse { Description = "Forbidden — user does not have the required role (ADMIN or EDITOR)" }

            }
        }).Produces<List<VehicleModelView>>(StatusCodes.Status201Created)
        .Produces<ErrorValidation>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(new AuthorizeAttribute { Roles = nameof(Role.ADMIN) });

        app.MapGet("/vehicle", ([FromQuery] int? page, [FromQuery] string? name, [FromQuery] string? brand, IVehicleService service) =>
        {
            var vehicles = service.FindAll(page: page ??= 1, name: name, brand: brand);

            return Results.Ok<List<Vehicle>>(vehicles);
        }).WithOpenApi(operation => new OpenApiOperation
        {
            Summary = "List vehicles",
            Description = "Retrieves a paginated list of vehicles. You can filter by name and/or brand.",
            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Vehicles" } },
            Parameters = new List<OpenApiParameter>
            {
                new() {
                    Name = "page",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Page number for pagination (default: 1)",
                    Schema = new OpenApiSchema { Type = "integer", Default = new OpenApiInteger(1) }
                },
                new() {
                    Name = "name",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Filter by vehicle name (partial match)",
                    Schema = new OpenApiSchema { Type = "string" },
                    Example = new OpenApiString("Civic")
                },
                new() {
                    Name = "brand",
                    In = ParameterLocation.Query,
                    Required = false,
                    Description = "Filter by brand name (partial match)",
                    Schema = new OpenApiSchema { Type = "string" },
                    Example = new OpenApiString("Honda")
                }
            },
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "List of vehicles successfully retrieved",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        ["id"] = new OpenApiSchema { Type = "integer" },
                                        ["name"] = new OpenApiSchema { Type = "string" },
                                        ["brand"] = new OpenApiSchema { Type = "string" },
                                        ["year"] = new OpenApiSchema { Type = "integer" }
                                    }
                                }
                            },
                            Example = new OpenApiArray
                            {
                                new OpenApiObject
                                {
                                    ["id"] = new OpenApiInteger(1),
                                    ["name"] = new OpenApiString("Civic"),
                                    ["brand"] = new OpenApiString("Honda"),
                                    ["year"] = new OpenApiInteger(2023)
                                },
                                new OpenApiObject
                                {
                                    ["id"] = new OpenApiInteger(2),
                                    ["name"] = new OpenApiString("Corolla"),
                                    ["brand"] = new OpenApiString("Toyota"),
                                    ["year"] = new OpenApiInteger(2021)
                                }
                            }
                        }
                    }
                },
                ["400"] = new OpenApiResponse
                {
                    Description = "Invalid query parameters (e.g., page number must be positive)"
                }
            }
        }).AllowAnonymous();

        app.MapGet("/vehicle/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle != null) return Results.Ok(vehicle);

            return Results.NotFound(new { error = $"Vehicle with ID {id} not found" });
        }).WithOpenApi(operation => new OpenApiOperation
        {
            Summary = "Get vehicle by ID",
            Description = "Retrieves a specific vehicle by its unique identifier.",
            Tags = [
                new (){ Name = "Vehicles"}
            ],
            Parameters = [
                new ()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Required = true,
                    Description = "The unique identifier of the vehicle",
                    Schema = new OpenApiSchema { Type = "integer" },
                    Example = new OpenApiInteger(1)
                }
            ],
            Responses = new()
            {
                ["200"] = new OpenApiResponse
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
                                    ["name"] = new OpenApiSchema { Type = "string", Description = "Vehicle model name" },
                                    ["brand"] = new OpenApiSchema { Type = "string", Description = "Vehicle brand" },
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
                },
                ["404"] = new OpenApiResponse
                {
                    Description = "Vehicle not found",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["error"] = new OpenApiString("Vehicle with ID 99 not found")
                            }
                        }
                    }
                },
                ["400"] = new OpenApiResponse
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
                                ["detail"] = new OpenApiString("Invalid ID parameter — must be a positive integer or greater than zero"),
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
                }
            }
        }).AllowAnonymous();

        app.MapPut("/vehicle/{id}", ([FromRoute] int id, VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle == null) return Results.NotFound("Vehicle can't be updated because it doesn't exists on our database");

            var validation = ValidateVehicleDTO(vehicleDTO);
            if (validation.Messages.Count > 0)
                return Results.BadRequest(validation);

            vehicle.Name = vehicleDTO.Name;
            vehicle.Brand = vehicleDTO.Brand;
            vehicle.Year = vehicleDTO.Year;
            vehicleService.Update(vehicle);

            return Results.NoContent();
        }).WithTags("Vehicles")
        .RequireAuthorization(new AuthorizeAttribute { Roles = nameof(Role.ADMIN) });

        app.MapDelete("/vehicle/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle == null) return Results.NotFound("Vehicle can't be deleted because it doesn't exists on our database");
            vehicleService.Delete(vehicle);
            return Results.NoContent();
        }).WithTags("Vehicles")
        .RequireAuthorization(new AuthorizeAttribute { Roles = nameof(Role.ADMIN) });
        #endregion

        #region Using Swagger and SwaggerIU
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthentication();
        app.UseAuthorization();
        #endregion

        app.Run();
    }
}
