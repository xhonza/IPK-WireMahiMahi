using System.Collections.Generic;
using System.Net.NetworkInformation;

/// <summary>
/// Provides functions to list network interfaces.
/// </summary>
public class Interfaces
{
    /// <summary>
    /// Lists all active network interfaces.
    /// </summary>
    public static List<string> ListActiveInterfaces()
    {
        var list = new List<string>();
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Check for active network interfaces
            if (nic.OperationalStatus == OperationalStatus.Up)
            {
                list.Add(nic.Name);
            }
        }
        return list;
    }
}
