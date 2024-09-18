using EventProcessor.Configurations;
using EventProcessor.Data;
using EventProcessor.Extensions;
using EventProcessor.Services;
using EventProcessor.Workers;
using Microsoft.EntityFrameworkCore;
using Shared.Queries;
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.SetupDatabase();

app.UseSwagger();
app.UseSwaggerUI(configuration =>
{
    configuration.RoutePrefix = "";
    configuration.SwaggerEndpoint("/swagger/v1/swagger.json", "EventProcessor");
});

app.MapGet("/incidents", async Task<IResult> (IIncidentsService incidentsService,
    string? sortColumn, string? orderBy, int? page, int? pageSize) =>
{
    page ??= 1;
    pageSize ??= 100;
    
    var response = await incidentsService.GetIncidents(new GetIncidentsQuery(sortColumn, orderBy, page.Value, pageSize.Value));
    return Results.Ok(response);
});

app.MapPost("/events", async Task<IResult> (SendEventRequest request, IIncidentsService incidentsService) =>
{
    await incidentsService.HandleEventRequest(request);
    return Results.Ok();
});

app.Run();