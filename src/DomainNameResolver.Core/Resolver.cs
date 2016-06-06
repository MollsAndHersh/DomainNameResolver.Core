using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DomainNameResolver.Core
{
    public class Resolver
    {
        private readonly IPAddress server;

        public Resolver(IPAddress server)
        {
            this.server = server;
        }

        public async Task<string> SendQuery(string name, QueryType queryType, QueryClass queryClass = QueryClass.IN)
        {
            var query = CreateQueryPayload(name, queryType, queryClass);

            using (var client = new UdpClient())
            {
                await client.SendAsync(query, query.Length, new IPEndPoint(server, 53));
                var response = await client.ReceiveAsync();
                var data = response.Buffer;

                if (data[0] != query[0] || data[1] != query[1])
                    throw new Exception("The response ID does not match the query ID");

                // Check QR bit in 3rd byte
                if ((data[2] & 128) != 128)
                    throw new Exception("This is not a response");

                // Do not check other flags in bytes 2 and 3 (TODO)

                // Number of entries in questions section are bytes 4 and 5 but are skiped
                int questions = data[4] * 256 + data[5];
                // Number of answers are bytes 6 and 7
                int answers = data[6] * 256 + data[7];

                // Number of name server resource records are bytes 8 and 9 (not supported)
                // Number of resource records are bytes 10 and 11 (not supported)

                int position = 12;

                for (var i = 0; i< questions; i++)
                {
                    // Should be identical to our question, don't care
                    ReadQuestion(data, ref position);
                }

                string result = null;
                for (var i = 0; i < answers; i++)
                {
                    // TODO For now, only keep the last answer
                    result = ReadAnswer(data, ref position);
                }
                
                return result;
            }
        }
        
        private string ReadAnswer(byte[] data, ref int position)
        {
            var domainName = GetDomainName(data, ref position);
            var queryType = (QueryType)data[position + 1];
            position += 2;
            var queryClass = (QueryClass)data[position + 1];
            position += 2;
            int ttl = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, position));
            position += 4;
            int length = data[position] * 256 + data[position + 1];
            position += 2;

            string result;
            switch (queryType)
            {
                case QueryType.A:
                    if (queryClass == QueryClass.IN)
                        result = GetIpV4Address(data, ref position, length);
                    else
                        throw new NotImplementedException();
                    break;
                case QueryType.MX:
                    result = GetDomainName(data, ref position);
                    break;
                case QueryType.TXT:
                    // TODO not sure of the format
                    result = Encoding.ASCII.GetString(data, position, length);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return result;
        }

        private void ReadQuestion(byte[] data, ref int position)
        {
            var domainName = GetDomainName(data, ref position);
            position += 4; // Skip Type and Class which are 16 bits each
        }

        private string GetIpV4Address(byte[] resp, ref int start, int length)
        {
            if (length != 4) throw new Exception("Unexcepted length");

            StringBuilder sb = new StringBuilder();

            int len = resp[start];
            for (int i = start; i < start + len; i++)
            {
                if (sb.Length > 0) sb.Append(".");
                sb.Append(resp[i + 1]);
            }
            start += len + 1;
            return sb.ToString();
        }

        private string GetDomainName(byte[] response, ref int start)
        {
            var result = new StringBuilder();
            for(;;)
            {
                byte length = response[start++];
                if (length == 0) break;

                if ((length & 192) != 192)
                {
                    if (result.Length > 0) result.Append(".");
                    result.Append(Encoding.ASCII.GetString(response, start, length));
                    start += length;
                    length = response[start];
                }
                else
                {
                    // If bits 7 and 8 are set, it's a reference to another part of the message
                    int newpos = (length & 63) * 256 + response[start++];
                    if (result.Length > 0) result.Append(".");
                    result.Append(GetDomainName(response, ref newpos));
                    break;
                }
            }
            return result.ToString();
        }
        
        private static byte[] CreateQueryPayload(string name, QueryType queryType, QueryClass queryClass)
        {
            var payload = new List<byte>();

            // ID
            payload.Add(88);
            payload.Add(89);

            // |QR|   Opcode  |AA|TC|RD
            payload.Add(1); // RD = 1
            // RA|   Z    |   RCODE   |
            payload.Add(0);
            // QDCOUNT: a 16 bits integer with the number of entries in the question section
            payload.Add(0);
            payload.Add(1);
            // ANCOUNT: a 16 bits integer with the number of name server resource records in the authority records section
            payload.Add(0);
            payload.Add(0);

            // NSCOUNT: an unsigned 16 bit integer specifying the number of name
            //          server resource records in the authority records
            //          section.
            payload.Add(0);
            payload.Add(0);

            // ARCOUNT: an unsigned 16 bit integer specifying the number of
            // resource records in the additional records section.
            payload.Add(0);
            payload.Add(0);

            // The only question:
            // TODO support more than one question
            // Each part of a domain is a label
            var labels = name.Split('.');
            foreach (var label in labels)
            {
                payload.Add((byte)label.Length);
                payload.AddRange(label.ToCharArray().Select(x => (byte)x));
            }
            // Terminal zero
            payload.Add(0);

            // The query type: a 16 bits integer
            payload.Add(0);
            payload.Add((byte)queryType);
            // The query class: another 16 bits integer
            payload.Add(0);
            payload.Add((byte)queryClass);
            return payload.ToArray();
        }
    }
}
