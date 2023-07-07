using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace ProxyChecker
{
    class Program
    {
        static readonly string domain = "http://ip-api.com/json/";
        static readonly int[] counter = new int[5];

        static string CorrectSingleQuoteJSON(string s)
        {
            string rstr = "";
            bool escaped = false;

            foreach (char c in s)
            {
                if (c == '\'' && !escaped)
                {
                    c = '"';
                }
                else if (c == '\'' && escaped)
                {
                    rstr = rstr.Remove(rstr.Length - 1);
                }
                else if (c == '"')
                {
                    c = '\\' + c;
                }

                escaped = (c == '\\');
                rstr += c;
            }

            return rstr;
        }

        static void Xx(string proxy, string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:99.0) Gecko/20100101 Firefox/99.0");
                    client.DefaultRequestHeaders.Add("accept-language", "en");
                    client.DefaultRequestHeaders.Add("accept", "application/json");

                    using (HttpResponseMessage response = client.GetAsync(url).Result)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        string correctJson = CorrectSingleQuoteJSON(responseContent);
                        dynamic m = JsonConvert.DeserializeObject(correctJson);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK && responseContent.Contains("success"))
                        {
                            Console.WriteLine("[Valid] " + proxy + " " + (int)response.StatusCode + " " + m["country"] + " " + m["as"]);
                            counter[1]++;
                            counter[4]++;

                            using (StreamWriter writer = File.AppendText("http.txt"))
                            {
                                writer.WriteLine(proxy);
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            Console.WriteLine("[Blocked] " + proxy + " " + (int)response.StatusCode);
                            counter[2]++;
                            counter[4]++;
                        }
                        else
                        {
                            Console.WriteLine("[Bad] " + proxy + " " + (int)response.StatusCode);
                            counter[3]++;
                            counter[4]++;
                        }
                    }
                }
                catch (Exception)
                {
                    counter[3]++;
                    counter[4]++;
                }
            }
        }

        static void Main(string[] args)
        {
            if (File.Exists("http.txt"))
            {
                File.Delete("http.txt");
            }

            string fileproxy = "all.txt";

            if (!File.Exists(fileproxy))
            {
                Console.WriteLine("Error: Enter your Proxy List!");
                return;
            }

            Console.Clear();
            List<string> prox = new List<string>(File.ReadAllLines(fileproxy));

            Console.WriteLine("Starting Threads");
            Thread.Sleep(1000);
            List<Thread> threads = new List<Thread>();

            foreach (string proxy in prox)
            {
                Thread t = new Thread(() => Xx(proxy, domain));
                t.Start();
                threads.Add(t);
                counter[0]++;

                Console.Title = "Proxies: " + counter[4] + "/" + counter[0] +
                    " | Valid: " + counter[1] + " | Blocked: " + counter[2] +
                    " | Bad: " + counter[3];
                Thread.Sleep(10);
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
                Console.Title = "Proxies: " + counter[4] + "/" + counter[0] +
                    " | Valid: " + counter[1] + " | Blocked: " + counter[2] +
                    " | Bad: " + counter[3];
            }
        }
    }
}
