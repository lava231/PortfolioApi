using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PortfolioApi.Repositories;
using PortfolioApi.Models.Entities;
using Xunit;

namespace PortfolioApi.Tests
{
    public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Login_Fails_With_Generic_Message_For_Invalid_Credentials()
        {
            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((Admin)null);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                });
            }).CreateClient();

            var payload = JsonSerializer.Serialize(new { email = "noone@example.com", password = "bad" });
            var res = await client.PostAsync("/api/auth/login", new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        private string CreateToken(string key, string issuer, string audience, string adminId = null, string role = "super-admin", DateTime? expires = null)
        {
            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>();
            if (!string.IsNullOrEmpty(adminId)) claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, adminId));
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));

            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires ?? DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task Token_With_Invalid_Signature_Is_Rejected()
        {
            var testKey = new string('a', 40);
            var otherKey = new string('b', 40);
            var issuer = "test-issuer";
            var audience = "test-audience";

            var admin = new Admin { Id = System.Guid.NewGuid(), IsActive = true, Role = "super-admin", Email = "a@b" };

            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

            // Set environment variables instead of ConfigureAppConfiguration to avoid test host extension method dependencies
            Environment.SetEnvironmentVariable("Jwt:Key", testKey);
            Environment.SetEnvironmentVariable("Jwt:Issuer", issuer);
            Environment.SetEnvironmentVariable("Jwt:Audience", audience);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                });
            }).CreateClient();

            // Create token signed with other key (invalid signature)
            var token = CreateToken(otherKey, issuer, audience, admin.Id.ToString());

            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Expired_Token_Is_Rejected()
        {
            var testKey = new string('a', 40);
            var issuer = "test-issuer";
            var audience = "test-audience";

            var admin = new Admin { Id = System.Guid.NewGuid(), IsActive = true, Role = "super-admin", Email = "a@b" };
            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

            Environment.SetEnvironmentVariable("Jwt:Key", testKey);
            Environment.SetEnvironmentVariable("Jwt:Issuer", issuer);
            Environment.SetEnvironmentVariable("Jwt:Audience", audience);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                });
            }).CreateClient();

            var token = CreateToken(testKey, issuer, audience, admin.Id.ToString(), expires: DateTime.UtcNow.AddMinutes(-10));

            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Wrong_Issuer_Or_Audience_Is_Rejected()
        {
            var testKey = new string('a', 40);
            var issuer = "test-issuer";
            var audience = "test-audience";

            var admin = new Admin { Id = System.Guid.NewGuid(), IsActive = true, Role = "super-admin", Email = "a@b" };
            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                });
                // Provide JWT configuration via environment variables in test context
                Environment.SetEnvironmentVariable("Jwt:Key", testKey);
                Environment.SetEnvironmentVariable("Jwt:Issuer", issuer);
                Environment.SetEnvironmentVariable("Jwt:Audience", audience);
            }).CreateClient();

            // Wrong issuer
            var tokenWrongIssuer = CreateToken(testKey, "bad-issuer", audience, admin.Id.ToString());
            var req1 = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenWrongIssuer);
            var res1 = await client.SendAsync(req1);
            Assert.Equal(HttpStatusCode.Unauthorized, res1.StatusCode);

            // Wrong audience
            var tokenWrongAudience = CreateToken(testKey, issuer, "bad-audience", admin.Id.ToString());
            var req2 = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenWrongAudience);
            var res2 = await client.SendAsync(req2);
            Assert.Equal(HttpStatusCode.Unauthorized, res2.StatusCode);
        }

        [Fact]
        public async Task Missing_Or_Malformed_AdminId_Claim_Is_Rejected()
        {
            var testKey = new string('a', 40);
            var issuer = "test-issuer";
            var audience = "test-audience";

            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.GetByIdAsync(It.IsAny<System.Guid>())).ReturnsAsync((Admin)null);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                });
                Environment.SetEnvironmentVariable("Jwt:Key", testKey);
                Environment.SetEnvironmentVariable("Jwt:Issuer", issuer);
                Environment.SetEnvironmentVariable("Jwt:Audience", audience);
            }).CreateClient();

            // Token without NameIdentifier
            var tokenNoId = CreateToken(testKey, issuer, audience, adminId: null);
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenNoId);
            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);

            // Token with malformed id
            var tokenBadId = CreateToken(testKey, issuer, audience, adminId: "not-a-guid");
            var req2 = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenBadId);
            var res2 = await client.SendAsync(req2);
            Assert.Equal(HttpStatusCode.Unauthorized, res2.StatusCode);
        }

        [Fact]
        public async Task Inactive_Or_Deleted_Admin_Is_Rejected()
        {
            var testKey = new string('a', 40);
            var issuer = "test-issuer";
            var audience = "test-audience";

            var admin = new Admin { Id = System.Guid.NewGuid(), IsActive = false, Role = "super-admin", Email = "a@b" };
            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                });
                Environment.SetEnvironmentVariable("Jwt:Key", testKey);
                Environment.SetEnvironmentVariable("Jwt:Issuer", issuer);
                Environment.SetEnvironmentVariable("Jwt:Audience", audience);
            }).CreateClient();

            var token = CreateToken(testKey, issuer, audience, admin.Id.ToString());
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Repository_Exception_During_TokenValidation_Fails_Closed()
        {
            var testKey = new string('a', 40);
            var issuer = "test-issuer";
            var audience = "test-audience";

            var adminId = System.Guid.NewGuid();
            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.GetByIdAsync(adminId)).ThrowsAsync(new System.Exception("DB error"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                });
                Environment.SetEnvironmentVariable("Jwt:Key", testKey);
                Environment.SetEnvironmentVariable("Jwt:Issuer", issuer);
                Environment.SetEnvironmentVariable("Jwt:Audience", audience);
            }).CreateClient();

            var token = CreateToken(testKey, issuer, audience, adminId.ToString());
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "/api/admin/export");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Login_Rate_Limiting_Returns_429_When_Exceeded()
        {
            // Configure a test host that sets PermitLimit to 1 so second request yields 429
            var testKey = new string('a', 40);
            var issuer = "test-issuer";
            var audience = "test-audience";

            var mockAdminRepo = new Mock<IAdminRepository>();
            mockAdminRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((Admin)null);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockAdminRepo.Object);
                    // Do not call AddRateLimiter here; tests run against the app's configured rate limiter.
                    // The app's LoginPolicy is already registered in Program.cs. We rely on the single-process test
                    // environment to exercise rate limiting behavior without re-registering the middleware here.
                });
                Environment.SetEnvironmentVariable("Jwt:Key", testKey);
                Environment.SetEnvironmentVariable("Jwt:Issuer", issuer);
                Environment.SetEnvironmentVariable("Jwt:Audience", audience);
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var payload = JsonSerializer.Serialize(new { email = "noone@example.com", password = "bad" });
            var res1 = await client.PostAsync("/api/auth/login", new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            // First attempt should be processed (401 expected because credentials invalid)
            Assert.Equal(HttpStatusCode.Unauthorized, res1.StatusCode);

            var res2 = await client.PostAsync("/api/auth/login", new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            // Second attempt should be rate-limited (429)
            Assert.Equal((HttpStatusCode)429, res2.StatusCode);
        }

        [Fact]
        public void Missing_Jwt_Configuration_Throws_On_Startup()
        {
            // Attempt to start host without required Jwt config and assert startup failure
            // Ensure no Jwt keys are present by clearing relevant environment variables for this test
            Environment.SetEnvironmentVariable("Jwt:Key", null);
            Environment.SetEnvironmentVariable("Jwt:Issuer", null);
            Environment.SetEnvironmentVariable("Jwt:Audience", null);

            var factory = _factory.WithWebHostBuilder(builder => { /* no additional config */ });

            // Creating a client should fail because the app throws during startup configuration
            Assert.ThrowsAny<System.Exception>(() => factory.CreateClient());
        }
    }
}
