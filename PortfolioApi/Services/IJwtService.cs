using System;

namespace PortfolioApi.Services
{
    public interface IJwtService
    {
        string GenerateToken(Guid adminId, string role, int expireMinutes = 60);
    }
}
