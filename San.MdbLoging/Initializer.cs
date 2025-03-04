using Common.DataAccess.Repository;
using Common.DataAccess.Repository.Base;
using Common.DataAccess.Repository.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoLogger.Attributes;
using MongoLogger.BgTasks;
using MongoLogger.Middleware;
using MongoLogger.Models;
using San.MdbLogging;
using San.SqlLogging;
using System;

namespace MongoLogger
{
    public static class Initializer
    {
        private static bool _isHostedServicesAdded = false;
        private static bool _isHttpAccessorAdded = false;

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
            services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));

            services.Add(new ServiceDescriptor(typeof(LogService<LogModel>), typeof(LogService<LogModel>), lifetime));

            services.Add(new ServiceDescriptor(typeof(LogManager<LogModel>), typeof(LogManager<LogModel>), lifetime));
            services.Add(new ServiceDescriptor(typeof(QueueManager<LogModel>), typeof(QueueManager<LogModel>), lifetime));

            if (!_isHostedServicesAdded)
            {
                services.AddHttpContextAccessor();
                services.Add(new ServiceDescriptor(typeof(IMdbLogger<>), typeof(LogManagerStandard<>), lifetime));
                services.AddHostedService<QueuedHostedService<LogModel>>();
                services.AddSingleton<IBackgroundTaskQueue<LogModel>, BackgroundTaskQueue<LogModel>>();
                _isHostedServicesAdded = true;
            }
        }
        public static void AddMongoLogger<T>(this IServiceCollection services, IConfiguration configuration) where T : BaseMongoModel
        {
            services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));


            services.AddTransient<ILogService<T>, ILogService<T>>();

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

            services.AddTransient<Func<string, ILogManager<T>>>((sp) =>
            {
                return new Func<string, LogManager<T>>(
                    (colName) =>
                    {
                        var op = sp.GetRequiredService<IOptions<LogDatabaseSettings>>();
                        var qm = sp.GetRequiredService<IQueueManager<T>>();
                        return (LogManager<T>)ActivatorUtilities.CreateInstance(sp, typeof(LogManager<T>), colName);
                    }
                );
            });
        }

        public static void AddMongoLogger(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));

            services.AddTransient<ILogService<LogModel>, LogService<LogModel>>();
            services.AddSingleton<IQueueManager<LogModel>, QueueManager<LogModel>>();
            services.AddSingleton<ILogManager<LogModel>, LogManager<LogModel>>();


            if (!_isHostedServicesAdded)
            {
                services.AddHttpContextAccessor();
                services.AddSingleton(typeof(IMdbLogger<>), typeof(LogManagerStandard<>));


                _isHostedServicesAdded = true;

                services.AddMvcCore(op =>
                {
                    op.Filters.Add(new AddTraceCodeToResponseHeader());
                });

            }
            services.AddHostedService<QueuedHostedService<LogModel>>();

            services.AddSingleton<IBackgroundTaskQueue<LogModel>, BackgroundTaskQueue<LogModel>>();



        }

        public static void AddMongoLoggerStandard(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<LogDatabaseSettings>(configuration.GetSection("LogDatabaseSettings"));

            services.AddTransient<ILogService<LogModel>, LogService<LogModel>>();
            services.AddSingleton<IQueueManager<LogModel>, QueueManager<LogModel>>();
            services.AddSingleton<ILogManager<LogModel>, LogManager<LogModel>>();


            if (!_isHostedServicesAdded)
            {
                services.AddSingleton(typeof(IMdbLogger<>), typeof(LogManagerStandard<>));
                _isHostedServicesAdded = true;
            }
            services.AddHostedService<QueuedHostedService<LogModel>>();

            services.AddSingleton<IBackgroundTaskQueue<LogModel>, BackgroundTaskQueue<LogModel>>();
        }

        public static void UseMongoLogger(this IApplicationBuilder app)
        {
            app.UseTraceId();


            BsonSerializer.RegisterSerializer(typeof(DateTime), new MyMongoDBDateTimeSerializer());
            BsonSerializer.RegisterSerializer(typeof(object), new ComplexTypeSerializer());



            LoggingAspect.SetHttpAccessor(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
            LoggingAspect.SetServiceProvider(app.ApplicationServices);
            if (!_isHttpAccessorAdded)
            {
                _isHttpAccessorAdded = true;
            }
        }

        public static void UseMongoLogger<T>(this IApplicationBuilder app) where T : BaseMongoModel
        {
            app.UseTraceId();

            BsonSerializer.RegisterSerializer(typeof(object), new ComplexTypeSerializer());

            LogManager<T>.SetHttpAccessor(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());

            if (!_isHttpAccessorAdded)
            {
                _isHttpAccessorAdded = true;
            }
        }

        public static void UseMongoLoggerStandard(this IServiceProvider sp)
        {
            BsonSerializer.RegisterSerializer(typeof(DateTime), new MyMongoDBDateTimeSerializer());
            LoggingAspect.SetServiceProvider(sp);

        }

        public static void UseMongoLoggerSql<T>(this IApplicationBuilder app) where T : BaseSqlModel
        {
            LogManagerSql<T>.SetHttpAccessor(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
            if (!_isHttpAccessorAdded)
            {
                _isHttpAccessorAdded = true;
            }
        }

    }
}
