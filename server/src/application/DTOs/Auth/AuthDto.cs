using application.DTOs.Entities;

namespace application.DTOs.Auth;

public sealed record AuthDto
{
    public required UserDto UserDto { get; init; }
    public required string AccessToken { get; init; }
}