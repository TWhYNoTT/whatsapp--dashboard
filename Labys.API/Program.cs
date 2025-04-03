using Labys.API.Middleware;
using Labys.Application.Services.Implementations;
using Labys.Application.Services.Interfaces;
using Labys.Domain.Entities;
using Labys.Infrastructure.Data;
using Labys.Infrastructure.Repositories.Implementations;
using Labys.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Text;

namespace Labys.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigureMiddleware(app, app.Environment);

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("ConnectionString"));
            });

            // Add Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings for development
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure JWT Authentication
            //services.AddAuthentication(options =>
            //{
            //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            //}).AddJwtBearer(options =>
            //{
            //    options.SaveToken = true;
            //    options.RequireHttpsMetadata = false;
            //    options.TokenValidationParameters = new TokenValidationParameters()
            //    {
            //        ValidateIssuer = true,
            //        ValidIssuer = configuration["JwtSetting:issuer"],
            //        ValidateAudience = true,
            //        ValidAudience = configuration["JwtSetting:audience"],
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new SymmetricSecurityKey(
            //            Encoding.UTF8.GetBytes(configuration["JwtSetting:SecritKey"])),
            //        ValidateLifetime = true,
            //        ClockSkew = TimeSpan.Zero
            //    };
            //});

            // Add Controllers
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

            // Register Swagger
            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "WhatsApp Business Dashboard API",
                    Description = "API for WhatsApp business messaging with Twilio integration"
                });

                swagger.CustomSchemaIds(type => type.FullName);

                // Enable JWT Authorization in Swagger
                swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token in the text input below.",
                });

                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

                // Try to include XML comments if they exist
                try
                {
                    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                    {
                        swagger.IncludeXmlComments(xmlPath);
                    }
                }
                catch
                {
                    // Ignore XML comment loading errors
                }
            });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("ApiPolicy", policy => {
                    policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
            });

            // Register HTTP context accessor
            services.AddHttpContextAccessor();

            // Register Repositories
            RegisterRepositories(services);

            // Register Services
            RegisterServices(services);
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            // Register Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IWhatsAppMessageRepository, WhatsAppMessageRepository>();
            services.AddScoped<IContactRepository, ContactRepository>();
            services.AddScoped<ITemplateRepository, TemplateRepository>();
            services.AddScoped<ICampaignRepository, CampaignRepository>();
            services.AddScoped<IChatbotRuleRepository, ChatbotRuleRepository>();
            services.AddScoped<IConversationAssignmentRepository, ConversationAssignmentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            // Register Services
            services.AddScoped<IWhatsAppService, WhatsAppService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<ICampaignService, CampaignService>();
            services.AddScoped<IChatbotService, ChatbotService>();
            services.AddScoped<IConversationAssignmentService, ConversationAssignmentService>();
            // Add this line - register the background service
            services.AddHostedService<CampaignBackgroundService>();
        }

        private static void ConfigureMiddleware(WebApplication app, IHostEnvironment env)
        {
            // Development specific middleware
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            // Global exception handling middleware
            app.UseExceptionHandlingMiddleware();

            // WhatsApp webhook middleware for Content-Type handling
            app.UseWhatsAppWebhookMiddleware();

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "WhatsApp API V1");
                c.RoutePrefix = "swagger";
            });

            // Configure static files with default wwwroot folder
            app.UseStaticFiles();

            // Add explicit configuration for media directory
            // This allows accessing files at /media/filename.ext
            var mediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
            if (!Directory.Exists(mediaPath))
            {
                Directory.CreateDirectory(mediaPath);
            }

            // If you need additional directory outside wwwroot:
            var externalMediaPath = Path.Combine(Directory.GetCurrentDirectory(), "media");
            if (!Directory.Exists(externalMediaPath))
            {
                Directory.CreateDirectory(externalMediaPath);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(externalMediaPath),
                RequestPath = "/media"
            });

            // Other middleware
            app.UseCors("ApiPolicy");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
        }
    }
}