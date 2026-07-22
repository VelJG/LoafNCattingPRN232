using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;

namespace LoafNCatting.Services.Mappers;

public static class AuthDtoMapper
{
    public static UserDto ToUserDto(User user, string roleName) => new(
        user.UserId,
        user.Name,
        user.Email,
        user.PhoneNumber,
        user.Address,
        roleName,
        user.IsActive,
        user.IsEmailVerified);
}
