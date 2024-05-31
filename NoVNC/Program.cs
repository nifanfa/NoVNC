using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

internal class Program
{
    private static void Main(string[] args)
    {
        if (!File.Exists("Settings.json"))
        {
            JsonObject jo = new JsonObject();
            jo["listen-port"] = 80;
            jo["vnc-server"] = "127.0.0.1:5900";
            File.WriteAllText("Settings.json", jo.ToJsonString(new System.Text.Json.JsonSerializerOptions() { WriteIndented = true }));
            Program.WriteLine("Please edit #Settings.json#", ConsoleColor.DarkGreen);
            Process.Start("cmd.exe", "/c TIMEOUT 10").WaitForExit();
            return;
        }

        int listen_port = -1;
        string vnc_server = null;
        if (File.Exists("Settings.json"))
        {
            JsonObject jo = (JsonObject)JsonNode.Parse(File.ReadAllText("Settings.json"));
            vnc_server = (string)jo["vnc-server"];
            listen_port = (int)jo["listen-port"];
        }
        Program.WriteLine($"NoVNC is running at #{listen_port} port#", ConsoleColor.DarkGreen);
        Program.WriteLine($"VNC-Server: #{vnc_server}#", ConsoleColor.DarkGreen);
        websockify.vnc_server = IPEndPoint.Parse(vnc_server);

        string filename = Directory.GetFiles(Environment.CurrentDirectory, "noVNC-*.*.*.zip").FirstOrDefault();
        if (filename == null)
        {
            Program.WriteLine($"#noVNC-*.*.*.zip# is not found!", ConsoleColor.DarkRed);
            Program.WriteLine("Go #https://github.com/novnc/noVNC/releases/# to get one!", ConsoleColor.Green);
            return;
        }
        Dictionary<string, byte[]> files = GetWebFiles(filename, Path.GetFileNameWithoutExtension(filename));

        HttpServer http = new HttpServer();
        http.AddWebSocketService<websockify>("/websockify");
        http.Start();
        http.OnGet += (sender, e) =>
        {
            var path = e.Request.RawUrl;
            if (path == "/")
            {
                Program.WriteLine($"HTTP#({GetUserRealIP(e.Request)})# Get",ConsoleColor.DarkGreen);
            }

            if (path == "/")
                path += "vnc.html";

            byte[] contents = null;
            if (!files.ContainsKey(path))
            {
                e.Response.StatusCode = (int)HttpStatusCode.NotFound;

                return;
            }
            else
            {
                contents = files[path];
            }

            if (path.EndsWith(".html"))
            {
                e.Response.ContentType = "text/html";
                e.Response.ContentEncoding = Encoding.UTF8;
            }
            else if (path.EndsWith(".js"))
            {
                e.Response.ContentType = "application/javascript";
                e.Response.ContentEncoding = Encoding.UTF8;
            }
            else if (path.EndsWith(".svg"))
            {
                e.Response.ContentType = "image/svg+xml";
                e.Response.ContentEncoding = Encoding.UTF8;
            }
            else if (path.EndsWith(".json"))
            {
                e.Response.ContentType = "application/json";
                e.Response.ContentEncoding = Encoding.UTF8;
            }

            e.Response.ContentLength64 = contents.LongLength;
            e.Response.Close(contents, true);
        };
        Console.ReadLine();
        http.Stop();

    }

    public static object WriteLock = new object();

    public static void WriteLine(string s, params ConsoleColor[] color)
    {
        lock (WriteLock)
        {
            Console.Write($"[{DateTime.Now}]: ");
            int index = 0;
            bool coloring = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '#')
                {
                    if (!coloring)
                    {
                        try
                        {
                            Console.BackgroundColor = color[index];
                        }
                        catch
                        {
                            Console.ResetColor();
                        }
                        index++;
                        coloring = true;
                    }
                    else
                    {
                        Console.ResetColor();
                        coloring = false;
                    }
                    continue;
                }
                Console.Write(s[i]);
            }
            Console.WriteLine();
        }
    }

    static Dictionary<string, byte[]> GetWebFiles(string zip, string prefix_to_remove = null)
    {
        Dictionary<string, byte[]> result = new Dictionary<string, byte[]>();
        ZipArchive novnc = ZipFile.OpenRead(zip);
        foreach (var file in novnc.Entries)
        {
            if (file.FullName.StartsWith(Path.GetFileNameWithoutExtension(zip)))
            {
                string name = file.FullName.Replace('\\', '/');
                if (prefix_to_remove != null)
                {
                    if (name.StartsWith(prefix_to_remove))
                    {
                        name = name.Replace(prefix_to_remove, string.Empty);
                    }
                }
                if (file.Length != 0)
                {
                    result.Add(name, file.Open().ToArray());
                }
            }
        }
        return result;
    }

    static string GetUserRealIP(HttpListenerRequest request)
    {
        string ip = request.Headers["HTTP_X_FORWARDED_FOR"];
        if (!string.IsNullOrEmpty(ip))
        {
            string[] temp = ip.Split(',');
            return temp[0];
        }
        else
        {
            return request.RemoteEndPoint.Address.ToString();
        }
    }
}
