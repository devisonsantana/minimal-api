using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Domain.DTOs;
using minimal_api.Domain.Entities;
using minimal_api.Domain.Interfaces;
using minimal_api.Infrastructure.Db;

namespace minimal_api.Domain.Services
{
    public class AdministratorService : IAdministratorService
    {
        private readonly DatabaseContext _dbContext;
        public AdministratorService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<Administrator> FindAll(int? page = 1)
        {
            var query = _dbContext.Administrators.AsQueryable();
            int itemsPerPage = 10;
            if (page != null)
            {
                return query.Skip(((int)page - 1) * itemsPerPage).Take(itemsPerPage).ToList();
            }
            else
            {
                page = 1;
                return query.Skip(((int) page - 1) * itemsPerPage).Take(itemsPerPage).ToList();
            }
        }

        public Administrator? FindById(int id)
        {
            return _dbContext.Administrators.Where(a => a.Id == id).FirstOrDefault();
        }

        public Administrator? Login(LoginDTO loginDTO)
        {
            return _dbContext.Administrators
                .Where(adm => adm.Email == loginDTO.Email && adm.Password == loginDTO.Password)
                .FirstOrDefault();
        }

        public Administrator Save(Administrator administrator)
        {
            _dbContext.Administrators.Add(administrator);
            _dbContext.SaveChanges();
            return administrator;
        }
    }
}