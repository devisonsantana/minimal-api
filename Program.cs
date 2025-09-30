using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using minimal_api.Domain.DTOs;
using minimal_api.Domain.Entities;
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

        #region Home endpoint
        app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
        #endregion

        #region Administrator endpoint
        app.MapPost("/administrator/login", ([FromBody] LoginDTO loginDTO, IAdministratorService administrator) =>
        {
            if (administrator.Login(loginDTO) != null)
            {
                return Results.Ok("Login Successfuly");
            }
            else
            {
                return Results.Unauthorized();
            }
        }).WithTags("Administrators");
        #endregion

        #region Vehicle endpoint
        app.MapPost("/vehicle", ([FromBody] VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
        {
            var vehicle = new Vehicle
            {
                Name = vehicleDTO.Name,
                Brand = vehicleDTO.Brand,
                Year = vehicleDTO.Year
            };
            vehicleService.Save(vehicle);
            return Results.Created($"/vehicle/{vehicle.Id}", vehicle);
        }).WithTags("Vehicles");

        app.MapGet("/vehicle", ([FromQuery] int? page, IVehicleService vehicleService) =>
        {
            var vehicles = vehicleService.FindAll(page);
            return Results.Ok<List<Vehicle>>(vehicles);
        }).WithTags("Vehicles");

        app.MapGet("/vehicle/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle != null) return Results.Ok(vehicle);
            return Results.NotFound();
        }).WithTags("Vehicles");

        app.MapPut("/vehicle/{id}", ([FromRoute] int id, VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
        {
            var vehicle = vehicleService.FindById(id);
            if (vehicle == null) return Results.NotFound("Vehicle can't be updated because it doesn't exists on our database");
            vehicle.Name = vehicleDTO.Name;
            vehicle.Brand = vehicleDTO.Brand;
            vehicle.Year = vehicleDTO.Year;
            vehicleService.Update(vehicle);
            return Results.NoContent();
        }).WithTags("Vehicles");
        #endregion

        #region Using Swagger and SwaggerIU
        app.UseSwagger();
        app.UseSwaggerUI();
        #endregion

        app.Run();
    }
}
