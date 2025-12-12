using System.Linq;
using FacturaApp.Core.Models;
using FacturaApp.Services;

public class AuthService
{
    private readonly IRepository<Usuario> _repo;

    public AuthService(IRepository<Usuario> repo)
    {
        _repo = repo;
    }

    public async Task<Usuario?> LoginAsync(string username, string password)
    {
        var users = await _repo.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return null;

        return PasswordHelper.VerifyPassword(password, user.Password) ? user : null;
    }
}