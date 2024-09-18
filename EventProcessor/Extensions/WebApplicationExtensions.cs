using EventProcessor.Data;
using Microsoft.EntityFrameworkCore;

namespace EventProcessor.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication SetupDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<EventProcessorDbContext>();

        try
        {
            dbContext.Database.Migrate();
        }
        catch (Exception e)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogError(e, "There was an error while creating or migrating database");

            Console.WriteLine(e);
            throw;
        }

        return app;
    }
}