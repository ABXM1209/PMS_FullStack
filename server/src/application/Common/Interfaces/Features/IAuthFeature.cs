using application.Common.Results;
using application.DTOs.Entities;
using application.DTOs.Responses;
using application.Features.Auth.Login;
using application.Features.Auth.Register;

namespace application.Common.Interfaces.Features;

public interface IAuthFeature
{
    Task<Result<LoginResponseDto>> HandleLogin(LoginCommand command);
    Task<Result> HandleRegisterUser(RegisterUserCommand command);
    Task<Result<UserDto>> HandleMeRequest(Guid myId);
    Task<Result> RevokeRefreshToken(Guid userId);
}