using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(User user, string roleName);
}
