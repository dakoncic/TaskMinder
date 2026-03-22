using MyFeatures.Helpers;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, _, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services
    .AddMyFeaturesDataAccess(builder.Configuration)
    .AddMyFeaturesApplication()
    .AddMyFeaturesApi()
    .AddMyFeaturesOpenApi()
    .AddMyFeaturesCors(builder.Configuration);

var app = builder.Build();

app.UseMyFeaturesPipeline();
app.ApplyMyFeaturesMigrations();

await app.RunAsync();
