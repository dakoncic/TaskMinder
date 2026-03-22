using Core.DomainModels;
using Infrastructure.DAL;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Entity = Infrastructure.Entities;

namespace Infrastructure.Helpers
{
    public static class StartupHelper
    {
        public static void ApplyMigrations(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<MyFeaturesDbContext>();

                    if (context.Database.IsRelational())
                    {
                        context.Database.Migrate();
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while applying the database migrations.");
                }
            }
        }

        public static void ConfigureMapster()
        {
            TypeAdapterConfig<Entity.TaskOccurrence, TaskOccurrence>.NewConfig()
                .PreserveReference(true);

            TypeAdapterConfig<TaskOccurrence, Entity.TaskOccurrence>.NewConfig()
                .PreserveReference(true);
        }
    }
}
