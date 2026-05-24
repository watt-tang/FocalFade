using Microsoft.Extensions.Logging;

namespace FocalFade.Core;

public sealed class AppLifecycleService
{
    private readonly ILogger<AppLifecycleService> _logger;

    public AppLifecycleService(ILogger<AppLifecycleService> logger)
    {
        _logger = logger;
    }

    public void OnSessionEnding()
    {
        _logger.LogInformation("Windows session ending");
    }
}
