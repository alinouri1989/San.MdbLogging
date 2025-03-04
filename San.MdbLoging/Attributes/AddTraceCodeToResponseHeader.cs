using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using MongoLogger.Extensions;
using MongoLogger.Middleware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MongoLogger.Attributes
{
    public class AddTraceCodeToResponseHeader : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                context.HttpContext.Response.Headers.Add(MongoLoggerHeaderKey.TraceCode, TraceIdExplorer.TraceCode);
                context.HttpContext.Response.Headers.Add(MongoLoggerHeaderKey.ExternalRefrenceId, TraceIdExplorer.ExternalRefrenceNumber);
            }
            catch
            {

            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                if (!context.HttpContext.Response.Headers.TryGetValue(MongoLoggerHeaderKey.TraceCode, out var _))
                    context.HttpContext.Response.Headers.Add(MongoLoggerHeaderKey.TraceCode, TraceIdExplorer.TraceCode);

                if (!context.HttpContext.Response.Headers.TryGetValue(MongoLoggerHeaderKey.ExternalRefrenceId, out var _))
                    context.HttpContext.Response.Headers.Add(MongoLoggerHeaderKey.ExternalRefrenceId, TraceIdExplorer.ExternalRefrenceNumber);

            }
            catch
            {
            }
        }
    }
}
