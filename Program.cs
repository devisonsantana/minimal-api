using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        var app = builder.Build();
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

        ErrorValidation ValidateUserDTO(UserDTO userDTO)
        {
            var validations = new ErrorValidation();
            if (string.IsNullOrEmpty(userDTO.Email))
                validations.Messages.Add("Email field cannot be empty");
            if (string.IsNullOrEmpty(userDTO.Password))
                validations.Messages.Add("Password field must be filled");
            if (userDTO.Role == null)
                validations.Messages.Add("Role field cannot be empty");

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
        app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
        #endregion

        #region Sign-up and Sign-in endpoint
        app.MapPost("/signup", ([FromBody] UserDTO userDTO, IUserService service) =>
            {
                var validation = ValidateUserDTO(userDTO);
                if (validation.Messages.Count > 0)
                    return Results.BadRequest(validation);
                var user = new User
                {
                    Email = userDTO.Email,
                    Password = userDTO.Password,
                    Role = userDTO.Role.ToString() ?? Role.EDITOR.ToString()
                };
                service.Save(user);
                return Results.Created($"/user/{user.Id}", user);
            }).WithOpenApi(operation => new OpenApiOperation
            {
                Summary = "Register a new user",
                Description = "Creates a new user with the specified role (ADMIN or EDITOR)",
                Tags = [new OpenApiTag { Name = "Signup/Login" }],
                RequestBody = new OpenApiRequestBody
                {
                    Description = "User data to create new account",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new OpenApiObject
                            {
                                ["email"] = new OpenApiString("jonhdoe@example.com"),
                                ["password"] = new OpenApiString("your-password"),
                                ["role"] = new OpenApiString("EDITOR")
                            }
                        }
                    }
                }
            })
            .AllowAnonymous();

        app.MapPost("/login", ([FromBody] LoginDTO loginDTO, IUserService service) =>
            {
                var adm = service.Login(loginDTO);
                if (adm != null)
                {
                    var token = GenerateTokenJWT(adm);
                    return Results.Ok(new UserSignedModelView
                    {
                        Email = adm.Email,
                        Role = adm.Role,
                        Token = token
                    });
                }
                return Results.Unauthorized();
            }).WithTags("Signup/Login").AllowAnonymous();
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
            Description = "Returns a paginated list of users. Access restricted to administrators only",
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
                                    ["email"] = new OpenApiString("admin@example.com"),
                                }
                            }
                        }
                    }
                },
                ["401"] = new OpenApiResponse
                { Description = "Unauthorized - JWT token missing or invalid" },
                ["403"] = new OpenApiResponse
                { Description = "Forbidden - User does not have ADMIN role" }
            }
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = nameof(Role.ADMIN) });

        app.MapGet("/user/{id}", ([FromRoute] int id, IUserService service) =>
        {
            var usr = service.FindById(id);
            if (usr == null) return Results.NotFound();
            return Results.Ok(new UserModelView { Id = usr.Id, Email = usr.Email, Role = usr.Role });
        }).WithTags("User")
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

            return Results.Created($"/vehicle/{vehicle.Id}", vehicle);
        }).WithTags("Vehicles")
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
            return Results.Created($"/vehicle", vehicles);
        }).WithTags("Vehicles")
        .RequireAuthorization(new AuthorizeAttribute { Roles = nameof(Role.ADMIN) });

        app.MapGet("/vehicle", ([FromQuery] int? page, [FromQuery] string? name, [FromQuery] string? brand, IVehicleService service) =>
        {
            var vehicles = service.FindAll(page: page ??= 1, name: name, brand: brand);

            return Results.Ok<List<Vehicle>>(vehicles);
        }).WithTags("Vehicles")
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{nameof(Role.ADMIN)},{nameof(Role.EDITOR)}" });

        app.MapGet("/vehicle/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle != null) return Results.Ok(vehicle);

            return Results.NotFound();
        }).WithTags("Vehicles")
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{nameof(Role.ADMIN)},{nameof(Role.EDITOR)}" });

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
