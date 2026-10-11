using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using PortfolioApi.Models.Entities;
using PortfolioApi.Repositories;
using Xunit;

namespace PortfolioApi.Tests
{
    // Minimal authentication handler for tests
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, Microsoft.AspNetCore.Authentication.ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, System.Guid.NewGuid().ToString()), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "super-admin") };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class ContactTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ContactTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UpdateStatus_Returns_BadRequest_For_Missing_Status()
        {
            var mockRepo = new Mock<IContactRepository>();

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IContactRepository>(mockRepo.Object);
                    services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", o => { });
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; });
                });
            }).CreateClient();

            var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            var res = await client.PutAsync($"/api/contact/{System.Guid.NewGuid()}", content);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_Returns_BadRequest_For_Unsupported_Status()
        {
            var mockRepo = new Mock<IContactRepository>();
            var contact = new Contact { Id = System.Guid.NewGuid(), Status = ContactStatus.Unread };
            mockRepo.Setup(r => r.GetByIdAsync(contact.Id)).ReturnsAsync(contact);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IContactRepository>(mockRepo.Object);
                    services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", o => { });
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; });
                });
            }).CreateClient();

            var payload = JsonSerializer.Serialize(new { status = "invalid" });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var res = await client.PutAsync($"/api/contact/{contact.Id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_Returns_NotFound_When_Missing()
        {
            var mockRepo = new Mock<IContactRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<System.Guid>())).ReturnsAsync((Contact)null);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IContactRepository>(mockRepo.Object);
                    services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", o => { });
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; });
                });
            }).CreateClient();

            var payload = JsonSerializer.Serialize(new { status = ContactStatus.Read });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var res = await client.PutAsync($"/api/contact/{System.Guid.NewGuid()}", content);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_Succeeds_With_Valid_Status()
        {
            var mockRepo = new Mock<IContactRepository>();
            var contact = new Contact { Id = System.Guid.NewGuid(), Status = ContactStatus.Unread };
            mockRepo.Setup(r => r.GetByIdAsync(contact.Id)).ReturnsAsync(contact);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Contact>())).ReturnsAsync((Contact c) => { c.UpdatedAt = System.DateTime.UtcNow; return c; });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IContactRepository>(mockRepo.Object);
                    services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", o => { });
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; });
                });
            }).CreateClient();

            var payload = JsonSerializer.Serialize(new { status = ContactStatus.Read });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var res = await client.PutAsync($"/api/contact/{contact.Id}", content);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadAsStringAsync();
            Assert.Contains("status", body);
        }

        [Fact]
        public async Task Pagination_Rejects_Invalid_Params()
        {
            var mockRepo = new Mock<IContactRepository>();
            mockRepo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(((System.Collections.Generic.IEnumerable<Contact>)System.Array.Empty<Contact>(), 0));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IContactRepository>(mockRepo.Object);
                    services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", o => { });
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; });
                });
            }).CreateClient();

            var res1 = await client.GetAsync("/api/contact?page=0");
            Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);
            var res2 = await client.GetAsync("/api/contact?limit=0");
            Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);
        }

        [Fact]
        public async Task Contact_Create_Is_Rate_Limited()
        {
            var mockRepo = new Mock<IContactRepository>();
            mockRepo.Setup(r => r.AddAsync(It.IsAny<Contact>())).ReturnsAsync((Contact c) => { c.Id = System.Guid.NewGuid(); c.CreatedAt = System.DateTime.UtcNow; return c; });

            // Provide rate limit configuration via environment to avoid test host extension mismatch
            Environment.SetEnvironmentVariable("Test_RateLimit_ContactPolicy_PermitLimit", "1");

            Environment.SetEnvironmentVariable("Jwt:Key", new string('a', 40));
            Environment.SetEnvironmentVariable("Jwt:Issuer", "test");
            Environment.SetEnvironmentVariable("Jwt:Audience", "test");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IContactRepository>(mockRepo.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var payload = JsonSerializer.Serialize(new { name = "A", email = "a@b.com", message = "0123456789" });
            var res1 = await client.PostAsync("/api/contact", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            Assert.True(res1.StatusCode == HttpStatusCode.Created || res1.StatusCode == HttpStatusCode.Unauthorized || res1.StatusCode == HttpStatusCode.BadRequest);

            var res2 = await client.PostAsync("/api/contact", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal((HttpStatusCode)429, res2.StatusCode);
        }

        [Fact]
        public async Task Import_Rejects_Invalid_Status_And_Does_Not_Call_Repo()
        {
            var mockRepo = new Mock<IContactRepository>();
            Environment.SetEnvironmentVariable("Jwt:Key", new string('a', 40));
            Environment.SetEnvironmentVariable("Jwt:Issuer", "test");
            Environment.SetEnvironmentVariable("Jwt:Audience", "test");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IContactRepository>(mockRepo.Object);
                    services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", o => { });
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; });
                });
            }).CreateClient();

            var importPayload = JsonSerializer.Serialize(new { contacts = new[] { new { name = "A", email = "a@b.com", message = "0123456789", status = "bad" } } });
            var res = await client.PostAsync("/api/admin/import", new StringContent(importPayload, System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            mockRepo.Verify(r => r.ReplaceContactsAsync(It.IsAny<System.Collections.Generic.IEnumerable<Contact>>()), Times.Never);
        }
    }
}
