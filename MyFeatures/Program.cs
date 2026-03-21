using Core.Interfaces;
using Core.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.DAL;
using Infrastructure.Helpers;
using Infrastructure.Interfaces.IRepository;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using MyFeatures.Helpers;
using MyFeatures.Middlewares;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, _, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.

builder.Services.AddDbContext<MyFeaturesDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure();
    });
});

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<ITaskTemplateService, TaskTemplateService>();
builder.Services.AddScoped<INotepadService, NotepadService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    //ovo je dodatak konfiguracije za cirkularnu referencu
    //kada return u akciji već uspješno prođe zbog Mapstera
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    //potrebno za slanje enuma između frontenda i backenda
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

//omogućuje automatsku validaciju
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

//mapster registracija nakon servisa
StartupHelper.ConfigureMapster();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//ovo će generirat ime akcije controllera za frontend isto kao što je i za backend
//npr. this.userService.GetAll()
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<CustomOperationIdFilter>();
});

var allowedOrigins = builder.Configuration.GetSection("CorsOrigins:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", corsBuilder =>
    {
        corsBuilder.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();

//dodano zbog UI 
app.UseStaticFiles();

app.UseRouting();

app.UseCors("CorsPolicy");

app.MapControllers();
//dodano zbog UI
app.MapFallbackToFile("index.html");

StartupHelper.ApplyMigrations(app);

await app.RunAsync();
