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
        public Administrator? Login(LoginDTO loginDTO)
        {
            return _dbContext.Administrators
                .Where(adm => adm.Email == loginDTO.Email && adm.Password == loginDTO.Password)
                .FirstOrDefault();
        }
    }
}