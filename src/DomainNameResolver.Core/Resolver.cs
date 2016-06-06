using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DomainNameResolver.Core
{
    public class Resolver
    {
        public QueryType QueryType { get; set; }
        public ICollection<IPAddress> DnsServers { get; set; }

        public Resolver()
        {
            DnsServers = new List<IPAddress>();
            GetLocalInterfacesDnsServers();
        }

        /// <summary>
        /// Retrieves the DNS server of each local network interface ad put them in the DnsServers list property.
        /// </summary>
        private void GetLocalInterfacesDnsServers()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                if (properties.DnsAddresses.Count > 0)
                    foreach (IPAddress ipAddress in properties.DnsAddresses)
                        DnsServers.Add(ipAddress);
            }
        }
        private byte[] PrepareRequestPayload(string domain)
        {
            List<byte> list = new List<byte>();
            list.AddRange(new byte[] { 88, 89, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0 });

            string[] tmp = domain.Split('.');
            foreach (string s in tmp)
            {
                list.Add(Convert.ToByte(s.Length));
                char[] chars = s.ToCharArray();
                foreach (char c in chars)
                    list.Add(Convert.ToByte(Convert.ToInt32(c)));
            }
            list.AddRange(new byte[] { 0, 0, Convert.ToByte(QueryType.ToString()), 0, 1 });

            byte[] req = new byte[list.Count];
            for (int i = 0; i < list.Count; i++) { req[i] = list[i]; }
            return req;
        }
        public async Task<string> SendQuery(string domain)
        {
            UdpClient client = new UdpClient();
            byte[] request = PrepareRequestPayload(domain);
            var receiveStatus = client.ReceiveAsync();
            await client.SendAsync(request, request.Length, new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53));

            // Response parsing
            var udpResp = await client.ReceiveAsync();
            int[] response = new int[udpResp.Buffer.Length];
            client.Dispose();

            for (int i = 0; i < response.Length; i++)
                response[i] = Convert.ToInt32(udpResp.Buffer[i]);

            // Check if bit Query Response is set
            if (response[3] != 128) throw new Exception(string.Format("{0}", response[3]));
            int answers = response[7];
            if (answers == 0) throw new Exception("No result");

            int pos = domain.Length + 18;
            while (answers > 0)
            {
                // int preference = resp[pos + 13];
                pos += 14; //offset
                return GetTxtRecord(pos, out pos, response);
                // answers--;
            }
            return null;
        }
        private string GetTxtRecord(int start, out int pos, int[] response)
        {
            StringBuilder sb = new StringBuilder();
            int len = response[start];
            while (len > 0)
            {
                if (len != 192)
                {
                    if (sb.Length > 0) sb.Append(".");
                    for (int i = start; i < start + len; i++)
                        sb.Append(Convert.ToChar(response[i + 1]));
                    start += len + 1;
                    len = response[start];
                }
                if (len == 192)
                {
                    int newpos = response[start + 1];
                    if (sb.Length > 0) sb.Append(".");
                    sb.Append(GetTxtRecord(newpos, out newpos, response));
                    start++;
                    break;
                }
            }
            pos = start + 1;
            return sb.ToString();
        }
    }
}
