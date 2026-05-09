using MassageSaas.Domain.Entities;

namespace MassageSaas.Infrastructure.Auth;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) CreateAccessToken(User user);
    string CreateRefreshToken();
}
