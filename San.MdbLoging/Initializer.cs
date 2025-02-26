using Common.DataAccess.Repository;
using Common.DataAccess.Repository.Base;
using Common.DataAccess.Repository.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using Quartz.Logging;
using San.MdbLogging.LogFile;
using San.MdbLogging.BgTasks;
using San.MdbLogging.Models;
using San.SqlLogging;

namespace San.MdbLogging;

public static class Initializer
{
    private static bool _isHostedServicesAdded;

    private static bool _isHttpAccessorAdded;

    /// <summary>
    /// Add Service MongoDb Logger generic with lifetime
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="lifetime"></param>
    public static void AddSanLogger<T, TKey>(this IServiceCollection services, IConfiguration configuration, ServiceLifetime lifetime) where T : BaseMongoModel
    {
        // Configure database settings  
        services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));

        // Register logging services with the specified lifetime  
        services.Add(new ServiceDescriptor(typeof(LogService<T>), typeof(LogService<T>), lifetime));

        services.Add(new ServiceDescriptor(typeof(QueueManager<T>), serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LogDatabaseSettings>>();
            var logger = serviceProvider.GetRequiredService<LogService<T>>();
            var batchSize = options.Value.BatchSize; // Assuming BatchSize is a property of LogDatabaseSettings  
            return new QueueManager<T>(options, serviceProvider, logger, batchSize);
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(LogManager<T>), serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LogDatabaseSettings>>();
            var queueManager = serviceProvider.GetRequiredService<QueueManager<T>>();
            return new LogManager<T>(serviceProvider, options, queueManager);
        }, lifetime));

        // Register hosted services if not already added  
        if (!_isHostedServicesAdded)
        {
            services.AddHttpContextAccessor();
            services.Add(new ServiceDescriptor(typeof(IMdbLogger<>), typeof(LogManagerStandard<>), lifetime));
            services.AddHostedService<QueuedHostedService<T>>();
            services.AddSingleton<IBackgroundTaskQueue<T>, BackgroundTaskQueue<T>>();
            _isHostedServicesAdded = true;
        }

        // Register a factory for LogManager<T>  
        services.Add(new ServiceDescriptor(typeof(Func<string, LogManager<T>>), serviceProvider =>
        {
            return (string colName) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<LogDatabaseSettings>>();
                var queueManager = serviceProvider.GetRequiredService<QueueManager<T>>();
                return new LogManager<T>(serviceProvider, options, queueManager, colName);
            };
        }, lifetime));
    }

    /// <summary>
    /// Add Service MongoDb Logger with singleton lifetime
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddSanLogger<T, TKey>(this IServiceCollection services, IConfiguration configuration) where T : BaseMongoModel
    {
        services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));
        services.AddSingleton(typeof(LogService<T>));
        services.AddSingleton(typeof(QueueManager<T>));
        services.AddSingleton(typeof(LogManager<T>));
        if (!_isHostedServicesAdded)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton(typeof(IMdbLogger<>), typeof(LogManagerStandard<>));
            services.AddHostedService<QueuedHostedService<T>>();
            services.AddSingleton<IBackgroundTaskQueue<T>, BackgroundTaskQueue<T>>();
            _isHostedServicesAdded = true;
        }

        services.AddSingleton((Func<IServiceProvider, Func<string, LogManager<T>>>)delegate (IServiceProvider sp)
        {
            IServiceProvider sp2 = sp;
            return (string colName) => new LogManager<T>(sp2, sp2.GetRequiredService<IOptions<LogDatabaseSettings>>(), sp2.GetRequiredService<QueueManager<T>>(), colName);
        });
    }

    /// <summary>  
    /// Add Service Sql Logger with lifetime  
    /// </summary>  
    /// <typeparam name="T"></typeparam>  
    /// <param name="services"></param>  
    /// <param name="configuration"></param>  
    /// <param name="lifetime"></param>  
    public static void AddSanLoggerSql<T>(this IServiceCollection services, IConfiguration configuration, ServiceLifetime lifetime) where T : BaseSqlModel
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));
                services.AddDbContextFactory<LogDbContext<T>>(options =>
                {
                    var config = configuration.GetSection("LogDatabaseSettings").Get<LogDatabaseSettings>();
                    options.UseSqlServer(config.SqlConnectionString);
                });

                //services.RegisterRepositoryBase<LogDbContext<T>>();
                services.AddSingleton(typeof(IRepositoryBase<,>), typeof(RepositoryBase<,>));
                services.AddSingleton(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

                services.AddSingleton(typeof(ILogServiceSql<T, LogDbContext<T>>), typeof(LogServiceSql<T, LogDbContext<T>>));
                services.AddSingleton(typeof(LogManagerSql<T>));
                services.AddSingleton(typeof(QueueManagerSql<T>));

                if (!_isHostedServicesAdded)
                {
                    services.AddHttpContextAccessor();
                    services.AddSingleton(typeof(ISQLLogger<,>), typeof(LogManagerStandardSql<,>));
                    services.AddHostedService<QueuedHostedService<T>>();
                    services.AddSingleton<IBackgroundTaskQueue<T>, BackgroundTaskQueue<T>>();
                    _isHostedServicesAdded = true;
                }
                break;

            case ServiceLifetime.Scoped:
                services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));
                services.AddDbContextFactory<LogDbContext<T>>(options =>
                {
                    var config = configuration.GetSection("LogDatabaseSettings").Get<LogDatabaseSettings>();
                    options.UseSqlServer(config.SqlConnectionString);
                }, lifetime);

                //services.RegisterRepositoryBase<LogDbContext<T>>();
                services.AddScoped(typeof(IRepositoryBase<,>), typeof(RepositoryBase<,>));
                services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

                services.AddScoped(typeof(ILogServiceSql<T, LogDbContext<T>>), typeof(LogServiceSql<T, LogDbContext<T>>));
                services.AddScoped(typeof(LogManagerSql<T>));
                services.AddScoped(typeof(QueueManagerSql<T>));

                if (!_isHostedServicesAdded)
                {
                    services.AddHttpContextAccessor();
                    services.AddScoped(typeof(ISQLLogger<,>), typeof(LogManagerStandardSql<,>));
                    services.AddHostedService<QueuedHostedService<T>>();
                    services.AddScoped<IBackgroundTaskQueue<T>, BackgroundTaskQueue<T>>();
                    _isHostedServicesAdded = true;
                }
                break;

            case ServiceLifetime.Transient:
                services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));
                services.AddDbContextFactory<LogDbContext<T>>(options =>
                {
                    var config = configuration.GetSection("LogDatabaseSettings").Get<LogDatabaseSettings>();
                    options.UseSqlServer(config.SqlConnectionString);
                }, lifetime);

                //services.RegisterRepositoryBase<LogDbContext<T>>();
                services.AddTransient(typeof(IRepositoryBase<,>), typeof(RepositoryBase<,>));
                services.AddTransient(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
                services.AddTransient(typeof(ILogServiceSql<T, LogDbContext<T>>), typeof(LogServiceSql<T, LogDbContext<T>>));
                services.AddTransient(typeof(LogManagerSql<T>));
                services.AddTransient(typeof(QueueManagerSql<T>));

                if (!_isHostedServicesAdded)
                {
                    services.AddHttpContextAccessor();
                    services.AddTransient(typeof(ISQLLogger<,>), typeof(LogManagerStandardSql<,>));
                    services.AddHostedService<QueuedHostedService<T>>();
                    services.AddTransient<IBackgroundTaskQueue<T>, BackgroundTaskQueue<T>>();
                    _isHostedServicesAdded = true;
                }
                break;

            default:
                break;
        }
    }

    /// <summary>  
    /// Perform database migration after service registration  
    /// </summary>  
    /// <typeparam name="T"></typeparam>  
    /// <param name="host"></param>  
    public static void MigrateLogDatabase<T>(this IHost host) where T : BaseSqlModel
    {
        using (var scope = host.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var context = scopedServices.GetRequiredService<LogDbContext<T>>();
            var config = scopedServices.GetRequiredService<IOptions<LogDatabaseSettings>>().Value;

            try
            {
                context.Database.ExecuteSqlRaw(config.SqlScriptString);
            }
            catch (Exception ex)
            {
                // Log the exception here  
                throw new InvalidOperationException("An error occurred during database migration.", ex);
            }
        }
    }

    public static void AddSanLoggerSql<T>(this IServiceCollection services, IConfiguration configuration) where T : BaseSqlModel
    {
        var _config = configuration.GetSection("LogDatabaseSettings");
        var _configLog = configuration.GetSection("Logging");
        services.Configure<LogDatabaseSettings>(_config);

        var config = _config.Get<LogDatabaseSettings>();
        services.AddDbContextFactory<LogDbContext<T>>(options =>
        {
            options.UseSqlServer(config.SqlConnectionString);
        }, ServiceLifetime.Scoped);

        services.RegisterRepositoryBase<LogDbContext<T>>();

        using (var serviceProvider = services.BuildServiceProvider())
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                try
                {
                    var context = scopedServices.GetRequiredService<LogDbContext<T>>();
                    context.Database.ExecuteSqlRaw(config.SqlScriptString);
                }
                catch (Exception ex)
                {
                    var logger = scopedServices.GetRequiredService<ILogger>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }
        }
        services.AddScoped(typeof(ILogServiceSql<T, LogDbContext<T>>), typeof(LogServiceSql<T, LogDbContext<T>>));
        services.AddScoped(typeof(QueueManagerSql<T>));
        services.AddScoped(typeof(LogManagerSql<T>));

        if (!_isHostedServicesAdded)
        {
            services.AddHttpContextAccessor();
            services.AddScoped(typeof(ISQLLogger<,>), typeof(LogManagerStandardSql<,>));
            services.AddHostedService<QueuedHostedService<T>>();
            services.AddSingleton<IBackgroundTaskQueue<T>, BackgroundTaskQueue<T>>();
            _isHostedServicesAdded = true;
        }
    }

    public static void AddSanLogger(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));
        services.AddSingleton(typeof(LogService<LogModel>));
        services.AddSingleton(typeof(QueueManager<LogModel>));
        services.AddSingleton(typeof(LogManager<LogModel>));
        if (!_isHostedServicesAdded)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton(typeof(IMdbLogger<>), typeof(LogManagerStandard<>));
            _isHostedServicesAdded = true;
        }

        services.AddHostedService<QueuedHostedService<LogModel>>();
        services.AddSingleton<IBackgroundTaskQueue<LogModel>, BackgroundTaskQueue<LogModel>>();
    }
    public static void AddSanLogger(this IServiceCollection services, IConfiguration configuration, ServiceLifetime lifetime)
    {
        // Configure database settings  
        services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));

        // Register log services with the specified lifetime  
        services.Add(new ServiceDescriptor(typeof(LogService<LogModel>), typeof(LogService<LogModel>), lifetime));

        services.Add(new ServiceDescriptor(typeof(LogManager<LogModel>), typeof(LogManager<LogModel>), lifetime));
        services.Add(new ServiceDescriptor(typeof(QueueManager<LogModel>), typeof(QueueManager<LogModel>), lifetime));

        // Register hosted services, ensuring they won't be added multiple times  
        if (!_isHostedServicesAdded)
        {
            services.AddHttpContextAccessor();
            services.Add(new ServiceDescriptor(typeof(IMdbLogger<>), typeof(LogManagerStandard<>), lifetime));
            services.AddHostedService<QueuedHostedService<LogModel>>();
            services.AddSingleton<IBackgroundTaskQueue<LogModel>, BackgroundTaskQueue<LogModel>>();
            _isHostedServicesAdded = true;
        }
    }

    public static void UseSanLogger(this IApplicationBuilder app)
    {
        BsonSerializer.RegisterSerializer(typeof(DateTime), new MyMongoDBDateTimeSerializer());
        LoggingAspect.SetHttpAccessor(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
        LoggingAspect.SetServiceProvider(app.ApplicationServices);
        if (!_isHttpAccessorAdded)
        {
            _isHttpAccessorAdded = true;
        }
    }

    public static void UseSanLogger<T>(this IApplicationBuilder app) where T : BaseMongoModel
    {
        LogManager<T>.SetHttpAccessor(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
        if (!_isHttpAccessorAdded)
        {
            _isHttpAccessorAdded = true;
        }
    }

    public static void UseSanLoggerSql<T>(this IApplicationBuilder app) where T : BaseSqlModel
    {
        LogManagerSql<T>.SetHttpAccessor(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
        if (!_isHttpAccessorAdded)
        {
            _isHttpAccessorAdded = true;
        }
    }

    public static void UseSanLoggerStandard(this IServiceProvider sp)
    {
        BsonSerializer.RegisterSerializer(typeof(DateTime), new MyMongoDBDateTimeSerializer());
        LoggingAspect.SetServiceProvider(sp);
    }
}