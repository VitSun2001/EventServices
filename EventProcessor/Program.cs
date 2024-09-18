using EventProcessor.Configurations;
using EventProcessor.Data;
using EventProcessor.Extensions;
using EventProcessor.Services;
using EventProcessor.Workers;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
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
builder.Services.AddScoped<IIncidentsService, IncidentsService>();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.SetupDatabase();
app.MapPost("/events", async Task<IResult> (SendEventRequest request, IIncidentsService incidentsService) =>
{
    await incidentsService.HandleEventRequest(request);
    return Results.Ok();
});

app.Run();