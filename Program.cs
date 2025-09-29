using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using minimal_api.Domain.DTOs;
using minimal_api.Domain.Interfaces;
using minimal_api.Domain.ModelViews;
using minimal_api.Domain.Services;
using minimal_api.Infrastructure.Db;

namespace minimal_api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddScoped<IAdministratorService, AdministratorService>();

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

        app.MapGet("/", () => Results.Json(new Home()));

        app.MapPost("/login", ([FromBody] LoginDTO loginDTO, IAdministratorService administrator) =>
        {
            if (administrator.Login(loginDTO) != null)
            {
                return Results.Ok("Login Successfuly");
            }
            else
            {
                return Results.Unauthorized();
            }
        });

        app.UseSwagger();
        app.UseSwaggerUI();

        app.Run();
    }
}
