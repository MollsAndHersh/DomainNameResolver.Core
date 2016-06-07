using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainNameResolver.Core;
using System.Net;
using System.Net.NetworkInformation;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                string domainName = "coincoin.acastaner.fr";
                var dnr = new Resolver(IPAddress.Parse("8.8.8.8"));
                var zob = await dnr.SendQuery(domainName, QueryType.TXT);
                //var dnr = new ResolverLegacy();
                //var zob = await dnr.SendQuery(domainName);
                Console.WriteLine(zob);
            }).GetAwaiter().GetResult();
            Console.ReadKey();
        }

        // TODO Utility function to put somewhere
        private List<IPAddress> GetLocalInterfacesDnsServers()
        {
            var results = new List<IPAddress>();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                if (properties.DnsAddresses.Count > 0)
                    foreach (IPAddress ipAddress in properties.DnsAddresses)
                        results.Add(ipAddress);
            }
            return results;
        }
    }
}
