using EventGenerator.Configurations;
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EventGeneratorOptions>(builder.Configuration.GetSection(nameof(EventGeneratorOptions)));
builder.Services.Configure<EventServiceOptions>(builder.Configuration.GetSection(nameof(EventServiceOptions)));
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();