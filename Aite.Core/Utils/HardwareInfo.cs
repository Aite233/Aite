using System.Management;

namespace Aite.Core.Utils;

public class HardwareInfo
{
    public static string GetCpuSerialNumber()
    {
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["ProcessorId"].ToString();
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }
    
    public static string GetMotherboardSerialNumber()
    {
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["SerialNumber"].ToString();
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }
    
    public static string GetHardDiskSerialNumber()
    {
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE InterfaceType='IDE'");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["SerialNumber"].ToString();
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }
}