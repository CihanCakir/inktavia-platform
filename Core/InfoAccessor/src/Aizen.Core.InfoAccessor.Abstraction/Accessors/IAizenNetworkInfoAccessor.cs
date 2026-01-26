using System.Net;
using System.Net.NetworkInformation;

namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenNetworkInfoAccessor
{
    public AizenNetworkInfo NetworkInfo { get; }
}

public class AizenNetworkInfo : IAizenInfo
{
    public InfoLifeCycle LifeCycle => InfoLifeCycle.Singleton;

    public string Hostname { get; set; }

    public string IPAddress { get; set; }

    public string DefaultGateway { get; set; }

    public string MacAddress { get; set; }

    public AizenNetworkInfo()
    {
        PopulateNetworkInfo();
    }

    private void PopulateNetworkInfo()
    {
        Hostname = Dns.GetHostName();
        IPAddress = GetIPAddress();
        DefaultGateway = GetDefaultGateway();
        MacAddress = GetMacAddress();
    }

    private string GetIPAddress()
    {
        var ipAddress = string.Empty;
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var networkInterface in networkInterfaces)
        {
            var properties = networkInterface.GetIPProperties();
            var ipAddresses = properties.UnicastAddresses;

            foreach (var address in ipAddresses)
            {
                if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = address.Address.ToString();
                    return ipAddress;
                }
            }
        }

        return ipAddress;
    }

    private string GetDefaultGateway()
    {
        var gatewayAddress = string.Empty;
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var networkInterface in networkInterfaces)
        {
            var properties = networkInterface.GetIPProperties();

            foreach (var gateway in properties.GatewayAddresses)
            {
                if (gateway.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    gatewayAddress = gateway.Address.ToString();
                    return gatewayAddress;
                }
            }
        }

        return gatewayAddress;
    }

    private string GetMacAddress()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var networkInterface in networkInterfaces)
        {
            if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                networkInterface.OperationalStatus == OperationalStatus.Up)
            {
                return networkInterface.GetPhysicalAddress().ToString();
            }
        }

        return null;
    }
}