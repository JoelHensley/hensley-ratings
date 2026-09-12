using DatabaseLayer;
using HensleyRatings.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration["DbPath"]
    ?? Environment.GetEnvironmentVariable("DB_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "console", "BuildFiles", "collegefootball.db");

builder.Services.AddDbContext<CollegeFootballEntities>(options =>
    options
        .UseLazyLoadingProxies()
        .UseSqlite($"Data Source={Path.GetFullPath(dbPath)}"));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(
        "http://localhost:5173",
        "http://localhost:4173"
    ).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapRatingsEndpoints();
app.MapScheduleEndpoints();
app.MapTeamsEndpoints();
app.MapMetaEndpoints();

app.Run();

public partial class Program { }
