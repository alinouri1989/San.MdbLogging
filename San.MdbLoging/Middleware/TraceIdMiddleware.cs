using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MongoLogger.Middleware
{
    public class TraceIdMiddleware
    {
        private readonly RequestDelegate _next;
        public TraceIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            Guid traceId = Guid.NewGuid();
            long refrenceNumber = DateTime.Now.Ticks;

            if (!context.Request.Headers.TryGetValue(MongoLoggerHeaderKey.TraceCode, out var _))
                context.Request.Headers.Add(MongoLoggerHeaderKey.TraceCode, traceId.ToString());

            if (!context.Request.Headers.TryGetValue(MongoLoggerHeaderKey.DefaultRefrenceNo, out var _))
                context.Request.Headers.Add(MongoLoggerHeaderKey.DefaultRefrenceNo, refrenceNumber.ToString());

            await _next(context);
        }
    }
    public static class UseTraceIdExtension
    {
        public static void UseTraceId(this IApplicationBuilder app)
        {
            HttpAppContext.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
            app.UseMiddleware<TraceIdMiddleware>();
        }
    }

    public class MongoLoggerHeaderKey
    {
        public const string TraceCode = "P_TraceCode";
        public const string DefaultRefrenceNo = "P_RefrenceNumber";

        public const string ExternalRefrenceId = "C_RefrenceCode";

    }

    public static class HttpAppContext
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static HttpContext Current => _httpContextAccessor!=null? _httpContextAccessor.HttpContext:null;
    }
}
