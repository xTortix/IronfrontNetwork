using System.Net;
using System.Xml.Linq;

const string defaultIpCheckUrl = "https://api.ipify.org";
const string namecheapUpdateBaseUrl = "https://dynamicdns.park-your-domain.com/update";

string domain = GetOption(args, "--domain") 
                ?? Environment.GetEnvironmentVariable("IRONFRONT_DDNS_DOMAIN") 
                ?? "warlabs.net";

string host = GetOption(args, "--host") 
              ?? Environment.GetEnvironmentVariable("IRONFRONT_DDNS_HOST") 
              ?? "gateway";

string? password = GetOption(args, "--password") 
                   ?? Environment.GetEnvironmentVariable("IRONFRONT_DDNS_PASSWORD");

string ipCheckUrl = GetOption(args, "--ip-check-url") 
                    ?? Environment.GetEnvironmentVariable("IRONFRONT_DDNS_IP_CHECK_URL") 
                    ?? defaultIpCheckUrl;

if (string.IsNullOrWhiteSpace(password))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Missing DDNS password.");
    Console.ResetColor();
    Console.WriteLine("Set it with:");
    Console.WriteLine("setx IRONFRONT_DDNS_PASSWORD \"your-namecheap-ddns-password\"");
    return 1;
}

using HttpClient httpClient = new();

string publicIp = await httpClient.GetStringAsync(ipCheckUrl);
publicIp = publicIp.Trim();

if (!IPAddress.TryParse(publicIp, out IPAddress? parsedIp) ||
    parsedIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Invalid IPv4 address received: {publicIp}");
    Console.ResetColor();
    return 1;
}

string requestUrl =
    $"{namecheapUpdateBaseUrl}" +
    $"?host={Uri.EscapeDataString(host)}" +
    $"&domain={Uri.EscapeDataString(domain)}" +
    $"&password={Uri.EscapeDataString(password)}" +
    $"&ip={Uri.EscapeDataString(publicIp)}";

Console.WriteLine($"Updating {host}.{domain} to {publicIp}...");

string response = await httpClient.GetStringAsync(requestUrl);

Console.WriteLine("Namecheap response:");
Console.WriteLine(response);

try
{
    XDocument xml = XDocument.Parse(response);

    string? errorCount = xml.Root?.Element("ErrCount")?.Value?.Trim();
    string? done = xml.Root?.Element("Done")?.Value?.Trim();
    string? error = xml.Root?.Element("Err")?.Value?.Trim();

    if (errorCount == "0" && string.Equals(done, "true", StringComparison.OrdinalIgnoreCase))
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("DDNS update successful.");
        Console.ResetColor();
        return 0;
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"DDNS update may have failed. Error: {error ?? "Unknown"}");
    Console.ResetColor();
    return 2;
}
catch
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Could not parse Namecheap response as XML.");
    Console.ResetColor();
    return 2;
}

static string? GetOption(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}