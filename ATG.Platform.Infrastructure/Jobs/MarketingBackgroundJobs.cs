using ATG.Platform.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ATG.Platform.Infrastructure.Jobs;

public class MarketingBackgroundJobs(IMarketingService marketing, ILogger<MarketingBackgroundJobs> logger)
{
    public async Task CheckPortalApprovalDelaysAsync()
    {
        logger.LogInformation("Running portal approval delay check");
        await marketing.ProcessPortalApprovalRemindersAsync();
    }

    public async Task CheckMarketingDeadlinesAsync()
    {
        logger.LogInformation("Running marketing deadline check");
        await marketing.ProcessDeadlineWarningsAsync();
    }
}
