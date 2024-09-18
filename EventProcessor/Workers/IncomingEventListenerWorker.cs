using System.Net;
using System.Text.Json;
using EventProcessor.Services;
using Shared.Requests;

namespace EventProcessor.Workers;

public class IncomingEventListenerWorker : BackgroundService
{
    private readonly IncidentsServiceSingleton _eventProcessor;
    private readonly HttpListener _httpListener = new();
    private readonly ILogger<IncomingEventListenerWorker> _logger;

    public IncomingEventListenerWorker(IncidentsServiceSingleton eventProcessor, ILogger<IncomingEventListenerWorker> logger)
    {
        _eventProcessor = eventProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _httpListener.Prefixes.Add("http://localhost:5888/events/");
        try
        {
            _httpListener.Start();
        }
        catch (HttpListenerException e)
        {
            _logger.LogError(e.Message);
            return;
        }
        _httpListener.Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            var httpContext = await _httpListener.GetContextAsync();
            var request = httpContext.Request;
            if (request.HttpMethod != HttpMethod.Post.Method) continue;
            
            string body;
            using (var reader = new StreamReader(httpContext.Request.InputStream, httpContext.Request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }
            
            var eventRequest = JsonSerializer.Deserialize<SendEventRequest>(body,
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            if (eventRequest == null) continue;
            
            await _eventProcessor.HandleEventRequest(eventRequest);

            var response = httpContext.Response;
            response.StatusCode = (int) HttpStatusCode.OK;
        }
    }
}