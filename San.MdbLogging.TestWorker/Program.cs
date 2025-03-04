using Common.Activation;
using MongoLogger;
using San.MdbLogging.TestWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSanLoggerSql<LogUpdatePrice>(builder.Configuration, lifetime: ServiceLifetime.Singleton);
builder.Services.AddHostedService<Worker>();
var serviceProvider = builder.Services.BuildServiceProvider();
ServiceActivator.Configure(serviceProvider);
var host = builder.Build();
host.MigrateLogDatabase<LogUpdatePrice>();
host.Run();
