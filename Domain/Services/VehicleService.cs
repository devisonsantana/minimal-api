using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using minimal_api.Domain.Entities;
using minimal_api.Domain.Exceptions;
using minimal_api.Domain.Interfaces;
using minimal_api.Infrastructure.Db;

namespace minimal_api.Domain.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly DatabaseContext _dbContext;

        public VehicleService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Delete(Vehicle vehicle)
        {
            _dbContext.Vehicles.Remove(vehicle);
            _dbContext.SaveChanges();
        }

        public List<Vehicle> FindAll(int page, string? name = null, string? brand = null)
        {
            if (page <= 0) throw new InvalidParameterException(page, "The value for 'page' must be positive.");
            var query = _dbContext.Vehicles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(v => EF.Functions.Like(v.Name.ToLower(), $"%{name.ToLower()}%"));

            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(v => EF.Functions.Like(v.Brand.ToLower(), $"%{brand.ToLower()}%"));

            int itemsPerPage = 10;

            return [.. query.Skip(((int)page - 1) * itemsPerPage).Take(itemsPerPage)];
        }

        public Vehicle? FindById(int id)
        {
            if (id <= 0) throw new InvalidParameterException(id, "Invalid ID parameter — must be a positive integer or greater than zero");
            // return _dbContext.Vehicles.Find(id);
            return _dbContext.Vehicles.Where(v => v.Id == id).FirstOrDefault();
        }

        public void Save(Vehicle vehicle)
        {
            _dbContext.Vehicles.Add(vehicle);
            _dbContext.SaveChanges();
        }

        public void SaveAll(List<Vehicle> vehicles)
        {
            _dbContext.Vehicles.AddRange(vehicles);
            _dbContext.SaveChanges();
        }

        public void Update(Vehicle vehicle)
        {
            _dbContext.Vehicles.Update(vehicle);
            _dbContext.SaveChanges();
        }
    }
}