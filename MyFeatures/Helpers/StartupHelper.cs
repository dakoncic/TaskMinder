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
                    context.Database.Migrate(); // This applies any pending migrations
                }
                catch (Exception ex)
                {
                    //should replace with serilog
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while applying the database migrations.");
                }
            }
        }

        public static void ConfigureMapster()
        {
            //mapiranja konfigurirati smjer svaki za sebe
            //.TwoWays() ne radi sa PreserveReference()

            TypeAdapterConfig<Entity.TaskOccurrence, TaskOccurrence>.NewConfig()
                .PreserveReference(true);

            TypeAdapterConfig<TaskOccurrence, Entity.TaskOccurrence>.NewConfig()
                .PreserveReference(true);
        }
    }
}
