using EventProcessor.Configurations;
using EventProcessor.Data;
using EventProcessor.Extensions;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EventProcessorOptions>(builder.Configuration.GetSection(nameof(EventProcessorOptions)));
builder.Services.AddDbContext<EventProcessorDbContext>(options =>
    {
        switch (builder.Environment.EnvironmentName)
        {
            case "Postgres":
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
                break;
            default:
                options.UseSqlite("Data Source=incidents.db");
                break;
        }
    }
);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.SetupDatabase();

app.Run();