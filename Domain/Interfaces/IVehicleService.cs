using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Domain.Entities;

namespace minimal_api.Domain.Interfaces
{
    public interface IVehicleService
    {
        List<Vehicle> FindAll(int? page = 1, string? name = null, string? brand = null);
        Vehicle? FindById(int id);
        void Delete(Vehicle vehicle);
        void Save(Vehicle vehicle);
        void Update(Vehicle vehicle);
    }
}