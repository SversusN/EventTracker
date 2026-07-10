using EventTracker.UsersService.Domain.Models;

namespace EventTracker.UsersService.Application.Ports;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByLoginAsync(string login);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
