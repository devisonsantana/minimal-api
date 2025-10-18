using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Domain.Entities;

namespace minimal_api.Domain.ModelViews
{
    public record VehicleModelView
    {
        public int Id { get; }
        public string Name { get; }
        public string Brand { get; }
        public int Year { get; }
        public VehicleModelView(Vehicle vehicle)
        {
            Id = vehicle.Id;
            Name = vehicle.Name;
            Brand = vehicle.Brand;
            Year = vehicle.Year;
        }
    }
}