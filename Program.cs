using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using minimal_api.Domain.DTOs;
using minimal_api.Domain.Entities;
using minimal_api.Domain.Enums;
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };
        });
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<IAdministratorService, AdministratorService>();
        builder.Services.AddScoped<IVehicleService, VehicleService>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<DatabaseContext>(options =>
        {
            options.UseMySql(
                builder.Configuration.GetConnectionString("mysql"),
                ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
            );
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
        ErrorValidation ValidateAdministratorDTO(AdministratorDTO administratorDTO)
        {
            var validations = new ErrorValidation();
            if (string.IsNullOrEmpty(administratorDTO.Email))
                validations.Messages.Add("Email field cannot be empty");
            if (string.IsNullOrEmpty(administratorDTO.Password))
                validations.Messages.Add("Password field must be filled");
            if (administratorDTO.Role == null)
                validations.Messages.Add("Role field cannot be empty");

            return validations;
        }
        #endregion

        #region Home endpoint
        app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
        #endregion

        #region Login endpoint
        string GenerateTokenJWT(Administrator administrator)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>()
            {
                // new Claim(ClaimTypes.Email, administrator.Email),
                // new Claim(ClaimTypes.Role, administrator.Role)
                new Claim("email", administrator.Email),
                new Claim("role", administrator.Role)
            };
            var token = new JwtSecurityToken(
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials,
                claims: claims
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        app.MapPost("/login", ([FromBody] LoginDTO loginDTO, IAdministratorService administrator) =>
            {
                var adm = administrator.Login(loginDTO);
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
            }).WithTags("Login");
        #endregion

        #region Administrator endpoint
        app.MapPost("/administrator", ([FromBody] AdministratorDTO administratorDTO, IAdministratorService administratorService) =>
        {
            var validation = ValidateAdministratorDTO(administratorDTO);
            if (validation.Messages.Count > 0)
                return Results.BadRequest(validation);
            var administrator = new Administrator
            {
                Email = administratorDTO.Email,
                Password = administratorDTO.Password,
                Role = administratorDTO.Role.ToString() ?? Role.EDITOR.ToString()
            };
            administratorService.Save(administrator);
            return Results.Created($"/adm/{administrator.Id}", administrator);
        }).WithTags("Administrator");

        app.MapGet("/administrator", ([FromQuery] int? page, IAdministratorService administratorService) =>
        {
            var adms = new List<AdministratorModelView>();
            administratorService.FindAll(page).ForEach(adm =>
            {
                adms.Add(new AdministratorModelView
                {
                    Id = adm.Id,
                    Email = adm.Email,
                    Role = adm.Role
                });
            });
            return Results.Ok(adms);
        }).WithTags("Administrator").RequireAuthorization();

        app.MapGet("/administrator/{id}", ([FromRoute] int id, IAdministratorService administratorService) =>
        {
            var administrator = administratorService.FindById(id);
            if (administrator == null) return Results.NotFound();
            return Results.Ok(new AdministratorModelView { Id = administrator.Id, Email = administrator.Email, Role = administrator.Role });
        }).WithTags("Administrator").RequireAuthorization();
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
        }).WithTags("Vehicles").RequireAuthorization();

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
        }).WithTags("Vehicles").RequireAuthorization();

        app.MapGet("/vehicle", ([FromQuery] int? page, IVehicleService vehicleService) =>
        {
            var vehicles = vehicleService.FindAll(page);

            return Results.Ok<List<Vehicle>>(vehicles);
        }).WithTags("Vehicles").RequireAuthorization();

        app.MapGet("/vehicle/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle != null) return Results.Ok(vehicle);

            return Results.NotFound();
        }).WithTags("Vehicles").RequireAuthorization();

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
        }).WithTags("Vehicles").RequireAuthorization();

        app.MapDelete("/vehicle/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle == null) return Results.NotFound("Vehicle can't be deleted because it doesn't exists on our database");
            vehicleService.Delete(vehicle);
            return Results.NoContent();
        }).WithTags("Vehicles").RequireAuthorization();
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
