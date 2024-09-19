using System.Net;
using System.Text.Json;
using EventProcessor.Configurations;
using EventProcessor.Services;
using Microsoft.Extensions.Options;
using Shared.Requests;

namespace EventProcessor.Workers;

public class IncomingEventListenerWorker : BackgroundService
{
    private readonly IIncidentsService _incidentsService;
    private readonly HttpListener _httpListener = new();
    private readonly ILogger<IncomingEventListenerWorker> _logger;
    private readonly EventProcessorOptions _options;

    public IncomingEventListenerWorker(
        IIncidentsService incidentsService,
        ILogger<IncomingEventListenerWorker> logger,
        IOptions<EventProcessorOptions> options
    )
    {
        _incidentsService = incidentsService;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _httpListener.Prefixes.Add(_options.AlternativeIncidentHttpListenerUri);
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

            await _incidentsService.HandleEventRequest(eventRequest);

            var response = httpContext.Response;
            response.StatusCode = (int) HttpStatusCode.OK;
            response.ContentType = "text/plain";
            await response.OutputStream.WriteAsync(Array.Empty<byte>().AsMemory(0, 0), stoppingToken);
            response.OutputStream.Close();
        }
    }
}