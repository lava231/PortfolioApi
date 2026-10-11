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
            var key = _config["Jwt:Key"] ?? _config["JWT_SECRET"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Jwt:Key is not configured");
            if (string.IsNullOrWhiteSpace(issuer)) throw new InvalidOperationException("Jwt:Issuer is not configured");
            if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException("Jwt:Audience is not configured");

            var minutes = expireMinutes;

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < 32) throw new InvalidOperationException("Jwt:Key is too short. Use a key with at least 32 bytes.");

            var securityKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
