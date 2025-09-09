using System;
using System.Collections.Generic;

namespace GalaShow.Common
{
    public static class ResponseHeaders
    {
        public static Dictionary<string, string> Get()
        {
            var origin = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGIN") ?? "";
            return new Dictionary<string, string>
            {
                { "Content-Type", "application/json; charset=utf-8" },
                { "Access-Control-Allow-Origin", origin },
                { "Access-Control-Allow-Credentials", "true" }
            };
        }
    }
}
