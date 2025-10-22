using minimal_api.Domain.DTOs;
using minimal_api.Domain.Entities;
using minimal_api.Domain.Exceptions;
using minimal_api.Domain.Interfaces;
using minimal_api.Infrastructure.Db;

namespace minimal_api.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly DatabaseContext _dbContext;
        public UserService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<User> FindAll(int page)
        {
            if (page <= 0) throw new InvalidParameterException(page, $"The value for 'page' must be greater than zero");
            var query = _dbContext.Users.AsQueryable();
            int itemsPerPage = 10;
            return [.. query.Skip(((int)page - 1) * itemsPerPage).Take(itemsPerPage)];
        }

        public User? FindById(int id)
        {
            if (id <= 0) throw new InvalidParameterException(id, "Invalid ID parameter — must be greater than zero integer");
            return _dbContext.Users.Where(u => u.Id == id).FirstOrDefault();
        }

        public User? Login(LoginDTO loginDTO)
        {
            return _dbContext.Users
                .Where(usr => usr.Email == loginDTO.Email && usr.Password == loginDTO.Password)
                .FirstOrDefault();
        }

        public User Save(User user)
        {
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
            return user;
        }
    }
}