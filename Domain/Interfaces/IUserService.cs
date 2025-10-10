using minimal_api.Domain.DTOs;
using minimal_api.Domain.Entities;

namespace minimal_api.Domain.Interfaces
{
    public interface IUserService
    {
        User? Login(LoginDTO loginDTO);
        User Save(User administrator);
        User? FindById(int id);
        List<User> FindAll(int? page = 1);
    }
}