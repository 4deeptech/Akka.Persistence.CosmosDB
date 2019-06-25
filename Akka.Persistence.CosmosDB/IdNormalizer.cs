using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Akka.Persistence.CosmosDB
{
    public static class IdNormalizer
    {
        public static string Normalize(string id)
        {
            //return Regex.Replace(id, "/?#\\", "-");
            return id.Replace("/", "-")
                .Replace("?", "-")
                .Replace("\\", "-")
                .Replace("#", "-");
        }
    }
}
