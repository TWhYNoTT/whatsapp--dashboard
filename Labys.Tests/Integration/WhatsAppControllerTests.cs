using Labys.Domain.DTOs;
using Labys.Domain.Entities;
using Labys.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Labys.Tests.Integration
{
    public class WhatsAppControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private string _authToken;

        public WhatsAppControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task AuthenticateAsync()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var loginData = new LoginDTO
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            var content = new StringContent(JsonConvert.SerializeObject(loginData), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/Auth/login", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<dynamic>(responseString);
            _authToken = responseData.Token;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        }

        [Fact]
        public async Task GetActiveConversations_ReturnsSuccessAndCorrectContentType()
        {
            // Arrange
            await AuthenticateAsync();

            // Act
            var response = await _client.GetAsync("/api/WhatsApp/conversations");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task SendMessage_WithValidData_ReturnsSuccess()
        {
            // Arrange
            await AuthenticateAsync();
            var message = new WhatsAppMessageDTO
            {
                ToNumber = "+1234567890",
                Body = "Test message from integration test"
            };

            var content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/WhatsApp/send", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<dynamic>(responseString);
            Assert.True((bool)responseData.Success);
        }

        [Fact]
        public async Task WebhookTest_ReturnsCorrectTwiMLResponse()
        {
            // Act
            var response = await _client.PostAsync("/api/WhatsApp/test-webhook", null);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/xml", response.Content.Headers.ContentType.ToString());
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("<Response><Message>", responseString);
        }
    }

    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Create a new service provider
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                // Add a database context using an in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                    options.UseInternalServiceProvider(serviceProvider);
                });

                // Build the service provider
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();

                    // Ensure the database is created
                    db.Database.EnsureCreated();

                    // Seed test data
                    SeedTestData(db, userManager, roleManager).Wait();
                }
            });
        }

        private async Task SeedTestData(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Add roles
            if (!await roleManager.RoleExistsAsync("SuperAdmin"))
            {
                await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            }

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Add test user
            if (await userManager.FindByEmailAsync("test@example.com") == null)
            {
                var user = new ApplicationUser
                {
                    UserName = "test@example.com",
                    Email = "test@example.com",
                    Branch = 1
                };

                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "SuperAdmin");
                }
            }

            // Add test contact
            if (!await context.Contacts.AnyAsync())
            {
                context.Contacts.Add(new Contact
                {
                    Name = "Test Contact",
                    PhoneNumber = "+1234567890",
                    HasOptedIn = true,
                    OptInDate = DateTime.UtcNow,
                    LastContactDate = DateTime.UtcNow
                });
            }

            // Add test template
            if (!await context.WhatsAppTemplates.AnyAsync())
            {
                context.WhatsAppTemplates.Add(new WhatsAppTemplate
                {
                    Name = "Test Template",
                    Description = "Test template for integration tests",
                    ContentSid = "HX00000000000000000000000000000000",
                    IsApproved = true,
                    Language = "en",
                    Type = "utility",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add test chatbot rule
            if (!await context.ChatbotRules.AnyAsync())
            {
                context.ChatbotRules.Add(new ChatbotRule
                {
                    Name = "Test Rule",
                    Keywords = "test,integration",
                    Response = "This is a test response",
                    Priority = 100,
                    IsActive = true
                });
            }

            await context.SaveChangesAsync();
        }
    }
}