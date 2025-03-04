using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.IO;
using MongoLogger.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace MongoLogger.Extensions
{
    public static class TraceIdExplorer
    {
        public static string TraceCode => GetTraceCodeFromHeader();
        public static string ExternalRefrenceNumber => GetExternalRefrenceIdFromHeader();
        public static long? RefrenceNumber => GetRefrenceNumberFromHeader();

        private static string GetTraceCodeFromHeader()
        {
            if (HttpAppContext.Current != null)
            {
                var context = HttpAppContext.Current.RequestServices.GetService<IHttpContextAccessor>();
                var tId = context.HttpContext.Request.Headers[MongoLoggerHeaderKey.TraceCode];
                return tId.ToString().ToLower().Replace("-", "");
            }
            return string.Empty;

        }

        private static string GetExternalRefrenceIdFromHeader()
        {
            try
            {
                if (HttpAppContext.Current != null)
                {
                    var context = HttpAppContext.Current.RequestServices.GetService<IHttpContextAccessor>();

                    var tId = context.HttpContext.Request.Headers[MongoLoggerHeaderKey.ExternalRefrenceId];

                    if (!string.IsNullOrEmpty(tId))
                    {
                        return tId;
                    }
                    return string.Empty;

                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private static long? GetRefrenceNumberFromHeader()
        {
            try
            {
                if (HttpAppContext.Current != null)
                {
                    var context = HttpAppContext.Current.RequestServices.GetService<IHttpContextAccessor>();

                    var tId = context.HttpContext.Request.Headers[MongoLoggerHeaderKey.DefaultRefrenceNo];

                    if (!string.IsNullOrEmpty(tId))
                    {
                        return Convert.ToInt64(tId);
                    }
                    return null;

                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
