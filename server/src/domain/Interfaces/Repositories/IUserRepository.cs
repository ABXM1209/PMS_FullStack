using domain.Entities;
using domain.Interfaces.Repositories;

namespace domain.Interfaces.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User> GetByEmailAsync(string email);
    Task<bool> IsUserExistByEmailAsync(string email);
}