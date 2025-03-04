using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace MongoLogger.Extensions
{
    public static class MDbExtensions
    {
        public static string GetIP(HttpContext context)
        {
			try
			{
				if (context == null)
					return string.Empty;

				var currentRequest = context.Request;

                StringValues values;
                if(context.Request.Headers.TryGetValue("X-Real-IP", out  values))
                    return values.ToString().Split(',')[0];

                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out values))
                    return  values.ToString().Split(',')[0];

                if (context.Request.Headers.TryGetValue("REMOTE_ADDR", out values))
                    return values.ToString();

                 if(context.Connection.RemoteIpAddress != null)
                    return context.Connection.RemoteIpAddress.ToString();


                return string.Empty;

            }
			catch (Exception ex)
			{
                return "ex-::1";
			}

        }

      
    }
}
