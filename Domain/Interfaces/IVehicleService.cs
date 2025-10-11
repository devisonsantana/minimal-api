using minimal_api.Domain.Entities;

namespace minimal_api.Domain.Interfaces
{
    public interface IVehicleService
    {
        List<Vehicle> FindAll(int page, string? name = null, string? brand = null);
        Vehicle? FindById(int id);
        void Delete(Vehicle vehicle);
        void Save(Vehicle vehicle);
        void SaveAll(List<Vehicle> vehicles);
        void Update(Vehicle vehicle);
    }
}