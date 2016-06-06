using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainNameResolver.Core;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string domainName = "coincoin.acastaner.fr";
            Resolver dnr = new Resolver();
            dnr.QueryType = QueryType.TXT;
            var zob = dnr.SendQuery(domainName);
        }
    }
}
