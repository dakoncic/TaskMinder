using Infrastructure.Helpers;
using MyFeatures.Middlewares;

namespace MyFeatures.Helpers
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseMyFeaturesPipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            return app;
        }

        public static WebApplication ApplyMyFeaturesMigrations(this WebApplication app)
        {
            StartupHelper.ApplyMigrations(app);
            return app;
        }
    }
}