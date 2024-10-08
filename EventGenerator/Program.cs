using EventGenerator.Configurations;
using EventGenerator.Services;
using EventGenerator.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EventGeneratorOptions>(builder.Configuration.GetSection(nameof(EventGeneratorOptions)));
builder.Services.Configure<EventServiceOptions>(builder.Configuration.GetSection(nameof(EventServiceOptions)));
builder.Services.AddHttpClient<IEventService>();
builder.Services.AddTransient<IEventService, EventService>();
builder.Services.AddHostedService<EventGeneratorWorker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(configuration =>
{
    configuration.RoutePrefix = "";
    configuration.SwaggerEndpoint("/swagger/v1/swagger.json", "MY API");
});


app.MapPost("/events", async Task<IResult> (IEventService eventSenderService) =>
{
    var generateEvent = eventSenderService.GenerateEvent();
    var responseMessage = await eventSenderService.SendEvent(generateEvent);
    return responseMessage.IsSuccessStatusCode ? Results.Ok() : Results.BadRequest();
});

app.Run();