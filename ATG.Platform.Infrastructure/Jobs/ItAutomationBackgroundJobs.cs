using ATG.Platform.Application.Interfaces;

namespace ATG.Platform.Infrastructure.Jobs;

public class ItAutomationBackgroundJobs(IItAutomationService service)
{
    public Task CheckExpiryWarningsAsync() => service.ProcessExpiryWarningsAsync();
}
