using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using minimal_api.Domain.Entities;
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

        public List<Vehicle> FindAll(int? page = 1, string? name = null, string? brand = null)
        {
            var query = _dbContext.Vehicles.AsQueryable();
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(v => EF.Functions.Like(v.Name.ToLower(), $"%{name}%"));
            }
            int itemsPerPage = 10;
            if (page != null)
            {
                return query.Skip(((int)page - 1) * itemsPerPage).Take(itemsPerPage).ToList();
            }
            else
            {
                return query.Skip((1 - 1) * itemsPerPage).Take(itemsPerPage).ToList();
            }
        }

        public Vehicle? FindById(int id)
        {
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