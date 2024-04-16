using System;
using System.Collections.Generic;
using SharpPcap;


    /// <summary>
    /// Provides utility functions for the sniffer application.
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Converts a MAC address string to standard MAC address format.
        /// </summary>
        /// <param name="macAddress">The MAC address string to convert.</param>
        /// <returns>The MAC address in standard format.</returns>
        public static string ConvertMacAddress(string macAddress)
        {
            if (macAddress == null)
            {
                throw new ArgumentNullException("Invalid MAC null");
            }

            // Ensure the input is valid
            if (macAddress.Length != 12)
            {
                throw new ArgumentException("Invalid MAC address format");
            }

            // Split the MAC address into pairs
            var pairs = Enumerable.Range(0, macAddress.Length / 2)
                                  .Select(i => macAddress.Substring(i * 2, 2));

            // Join the pairs with colons
            return string.Join(":", pairs);
        }
    }

