using ATG.Platform.Infrastructure;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), ".."))
    .AddJsonFile("ATG.Platform.API/appsettings.json")
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());
services.AddInfrastructure(config);
var sp = services.BuildServiceProvider();
await HoDataSeeder.SeedAsync(sp);
Console.WriteLine("HO seed complete");
