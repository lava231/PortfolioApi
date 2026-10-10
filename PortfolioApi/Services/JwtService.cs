using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace PortfolioApi.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Guid adminId, string role, int expireMinutes = 60)
        {
            var key = _config["Jwt:Key"] ?? _config["JWT_SECRET"] ?? "CHANGE_THIS_SECRET";
            var issuer = _config["Jwt:Issuer"] ?? "PortfolioApi";
            var minutes = expireMinutes;

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, adminId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                issuer,
                claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
