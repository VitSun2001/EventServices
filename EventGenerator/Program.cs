using EventGenerator.Configurations;
using EventGenerator.Services;
using EventGenerator.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EventGeneratorOptions>(builder.Configuration.GetSection(nameof(EventGeneratorOptions)));
builder.Services.Configure<EventServiceOptions>(builder.Configuration.GetSection(nameof(EventServiceOptions)));
builder.Services.AddHttpClient<IEventService>();
builder.Services.AddTransient<IEventService, EventService>();
builder.Services.AddHostedService<EventGeneratorWorker>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();