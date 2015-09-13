using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPScanner
{
    public class Scanner
    {
        static Random rnd = new Random();
        public static IPAddress GenerateIPAddress()
        {
            string separator = ".";
            var vals = new List<int>();
            vals.AddRange(Enumerable.Range(0, 4).Select(x => rnd.Next(0, 255)));
            string addr = string.Join(separator, vals.ToArray());
            return IPAddress.Parse(addr);
        }

        public async Task<HttpResponseMessage> ScanIPAddressAsync(IPAddress addr, CancellationToken ct)
        {
            try
            {
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(1000);
                var ipaddr = addr.ToString();
                var resp = client.GetAsync("http://" + ipaddr, ct);
                return await resp;
            }
            catch (OperationCanceledException)
            {
                /*
                I think this is always called because of setting a timeout
                for the HttpClient. Aka, when the timespan is exceeded 
                the task is 'cancelled'. 
                */
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
