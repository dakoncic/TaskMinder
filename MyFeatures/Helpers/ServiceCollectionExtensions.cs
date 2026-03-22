using Core.Interfaces;
using Core.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.DAL;
using Infrastructure.Helpers;
using Infrastructure.Interfaces.IRepository;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace MyFeatures.Helpers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMyFeaturesDataAccess(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MyFeaturesDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure();
                });
            });

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        public static IServiceCollection AddMyFeaturesApplication(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ITaskTemplateService, TaskTemplateService>();
            services.AddScoped<INotepadService, NotepadService>();

            StartupHelper.ConfigureMapster();

            return services;
        }

        public static IServiceCollection AddMyFeaturesApi(this IServiceCollection services)
        {
            services.AddProblemDetails();

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "One or more validation errors occurred.",
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                        Instance = context.HttpContext.Request.Path
                    };

                    problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    return new BadRequestObjectResult(problemDetails);
                };
            });

            return services;
        }

        public static IServiceCollection AddMyFeaturesOpenApi(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.OperationFilter<CustomOperationIdFilter>();
            });

            return services;
        }

        public static IServiceCollection AddMyFeaturesCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("CorsOrigins:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", corsBuilder =>
                {
                    corsBuilder.WithOrigins(allowedOrigins)
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}