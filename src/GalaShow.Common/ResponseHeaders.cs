using System;
using System.Collections.Generic;

namespace GalaShow.Common
{
    public static class ResponseHeaders
    {
        public static Dictionary<string, string> Get()
        {
            return new Dictionary<string, string>
            {
                { "Content-Type", "application/json; charset=utf-8" }
            };
        }
    }
}
