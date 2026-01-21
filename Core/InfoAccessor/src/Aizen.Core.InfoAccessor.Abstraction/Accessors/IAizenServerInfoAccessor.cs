using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenServerInfoAccessor
{
    public AizenServerInfo ServerInfo { get; }
}

public class AizenServerInfo : IAizenInfo
{
    public InfoLifeCycle LifeCycle => InfoLifeCycle.Singleton;

    public string MachineName { get; set; }
    public string OperatingSystem { get; set; }
    public string Processor { get; set; }
    public long TotalPhysicalMemory { get; set; }
    public long FreePhysicalMemory { get; set; }
    public long ProcessorCount { get; set; }
    public string GraphicsCard { get; set; }
    public string DisplayResolution { get; set; }
    public string Motherboard { get; set; }
    public string SystemDrive { get; set; }

    public AizenServerInfo()
    {
        PopulateAizenServerInfo();
    }

    private void PopulateAizenServerInfo()
    {
        MachineName = Environment.MachineName;
        OperatingSystem = RuntimeInformation.OSDescription;
        Processor = GetProcessorInfo();
        TotalPhysicalMemory = GetTotalPhysicalMemory();
        FreePhysicalMemory = GetFreePhysicalMemory();
        ProcessorCount = Environment.ProcessorCount;
        GraphicsCard = GetGraphicsCardInfo();
        DisplayResolution = GetDisplayResolution();
        Motherboard = GetMotherboardInfo();
        SystemDrive = GetSystemDrive();
    }

    private string GetProcessorInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWmicProperty("CPU", "Name");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "cat /proc/cpuinfo | grep 'model name' | uniq | cut -d ':' -f2";
            return ExecuteShellCommand(command);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var command = "sysctl -n machdep.cpu.brand_string";
            return ExecuteShellCommand(command);
        }

        return "Unknown";
    }

    private long GetTotalPhysicalMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWmicNumericProperty("ComputerSystem", "TotalPhysicalMemory");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "awk '/MemTotal/ {print $2}' /proc/meminfo";
            return Convert.ToInt64(ExecuteShellCommand(command)) * 1024;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var command = "sysctl -n hw.memsize";
            return Convert.ToInt64(ExecuteShellCommand(command));
        }

        return 0;
    }

    private long GetFreePhysicalMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWmicNumericProperty("OperatingSystem", "FreePhysicalMemory");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "awk '/MemAvailable/ {print $2}' /proc/meminfo";
            return Convert.ToInt64(ExecuteShellCommand(command)) * 1024;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetTotalPhysicalMemory();
        }

        return 0;
    }

    private string GetGraphicsCardInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWmicProperty("VideoController", "Name");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "lspci | grep -i vga | cut -d ':' -f3-";
            return ExecuteShellCommand(command);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var command = "system_profiler SPDisplaysDataType | grep 'Chipset Model' | cut -d ':' -f2";
            return ExecuteShellCommand(command);
        }

        return "Unknown";
    }

    private string GetDisplayResolution()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var command = "wmic path Win32_VideoController get CurrentHorizontalResolution,CurrentVerticalResolution";
            var output = ExecuteShellCommand(command);
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length > 1)
            {
                var horizontalResolution = string.Empty;
                var verticalResolution = string.Empty;
                var resolutionLine = lines[1];
                var resolutionValues = resolutionLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (resolutionValues.Length == 1)
                {
                    resolutionLine = lines.Last();
                    resolutionValues = resolutionLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    horizontalResolution = resolutionValues[0];
                    verticalResolution = resolutionValues[1];
                    return $"{horizontalResolution}x{verticalResolution}";
                }

                horizontalResolution = resolutionValues[0];
                verticalResolution = resolutionValues[1];
                return $"{horizontalResolution}x{verticalResolution}";
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "xdpyinfo | grep dimensions | awk '{print $2}'";
            return ExecuteShellCommand(command);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var command = "system_profiler SPDisplaysDataType | grep Resolution | awk '{print $2}'";
            return ExecuteShellCommand(command);
        }

        return "Unknown";
    }

    private string GetMotherboardInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var manufacturer = GetWmicProperty("BaseBoard", "Manufacturer");
            var product = GetWmicProperty("BaseBoard", "Product");
            return $"{manufacturer} {product}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "dmidecode -t baseboard | grep -E 'Manufacturer:|Product Name:' | cut -d ':' -f2";
            return ExecuteShellCommand(command);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var command = "system_profiler SPHardwareDataType | grep 'Model Identifier' | cut -d ':' -f2";
            return ExecuteShellCommand(command);
        }

        return "Unknown";
    }

    private string GetSystemDrive()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.GetPathRoot(Environment.SystemDirectory);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "df / | tail -1 | awk '{print $1}'";
            return ExecuteShellCommand(command);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var command = "df / | tail -1 | awk '{print $1}'";
            return ExecuteShellCommand(command);
        }

        return "Unknown";
    }

    private string GetWmicProperty(string className, string propertyName)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "wmic",
            Arguments = $"{className} get {propertyName}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processStartInfo))
        {
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > 1)
                {
                    return lines[1].Trim();
                }
            }
        }

        return "Unknown";
    }

    private long GetWmicNumericProperty(string className, string propertyName)
    {
        var value = GetWmicProperty(className, propertyName);
        if (string.IsNullOrEmpty(value) || value == "Unknown")
        {
            return 0;
        }

        return Convert.ToInt64(value);
    }

    private string ExecuteShellCommand(string command)

    {
        ProcessStartInfo psi;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi = new ProcessStartInfo("powershell", "-Command \"" + command + "\"");
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        using (var process = Process.Start(psi))
        {
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                return output.Trim();
            }
        }

        return "Unknown";
    }
}