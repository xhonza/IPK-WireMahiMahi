// Import necessary namespaces
using System;
using PacketDotNet;
using SharpPcap;

public class PacketHandler
{
    /// <summary>
    /// Handles the arrival of a packet.
    /// </summary>
    /// <param name="packet">The captured packet.</param>
    public static Packet HandlePacket(RawCapture packet,string Id)
    {
        if (ConvertPacket(packet) is EthernetPacket ethernetPacket)
        {
            // Extract Ethernet information
            var timeStamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
            var srcMac = ethernetPacket.SourceHardwareAddress.ToString();
            var dstMac = ethernetPacket.DestinationHardwareAddress.ToString();
            var frameLength = ethernetPacket.Bytes.Length;

            Console.WriteLine($"timestamp: {timeStamp}");
            Console.WriteLine($"src MAC: {Utils.ConvertMacAddress(srcMac)}");
            Console.WriteLine($"dst MAC: {Utils.ConvertMacAddress(dstMac)}");
            Console.WriteLine($"frame length: {frameLength} bytes");

            if (ethernetPacket.PayloadPacket is IPPacket ipPacket)
            {
                // Extract IP information
                var srcIp = ipPacket.SourceAddress.ToString();
                var dstIp = ipPacket.DestinationAddress.ToString();

                Console.WriteLine($"src IP: {srcIp}");
                Console.WriteLine($"dst IP: {dstIp}");

                if (ipPacket.PayloadPacket != null)
                {
                    // Extract protocol-specific information
                    switch (ipPacket.PayloadPacket)
                    {
                        case TcpPacket tcpPacket:
                            if (tcpPacket.PayloadData != null)
                            {
                                // Extract TCP information
                                var srcPort = tcpPacket.SourcePort.ToString();
                                var dstPort = tcpPacket.DestinationPort.ToString();
                                Console.WriteLine($"Protocol: TCP");
                                Console.WriteLine($"src port: {srcPort}");
                                Console.WriteLine($"dst port: {dstPort}");
                            }
                            break;

                        case UdpPacket udpPacket:
                            if (udpPacket.PayloadData != null)
                            {
                                // Extract UDP information
                                var srcPort = udpPacket.SourcePort.ToString();
                                var dstPort = udpPacket.DestinationPort.ToString();
                                Console.WriteLine($"Protocol: UDP");
                                Console.WriteLine($"src port: {srcPort}");
                                Console.WriteLine($"dst port: {dstPort}");
                            }
                            break;

                        case IcmpV4Packet icmpV4Packet:
                            // Extract ICMPv4 information
                            var icmpType = icmpV4Packet.TypeCode;
                            // TODO var icmpCode = icmpV4Packet.Data.ToString();
                            Console.WriteLine($"Protocol: ICMPv4");
                            Console.WriteLine($"ICMPv4 Type: {icmpType}");
                            // TODO Console.WriteLine($"ICMPv4 Code: {icmpCode}");
                            break;

                        case IcmpV6Packet icmpV6Packet:
                            // Extract ICMPv6 information
                            var icmpv6Type = icmpV6Packet.Type;
                            Console.WriteLine($"Protocol: ICMPv6 of type {icmpV6Packet.Type}");
                            break;

                        case IgmpV3MembershipReportPacket igmpPacket:
                            Console.WriteLine($"Protocol: IGMPv3");
                            Console.WriteLine($"IGMP Type: Membership Report");
                            // Handle IGMPv3 packet
                            break;

                        case IgmpV3MembershipQueryPacket igmpPacket:
                            Console.WriteLine($"Protocol: IGMPv3");
                            Console.WriteLine($"IGMP Type: Querry Report");
                            // Handle IGMPv3 packet
                            break;

                        case IgmpV2Packet igmpPacket:
                            Console.WriteLine($"Protocol: IGMPv2");
                            // Handle IGMPv2 packet
                            break;

                        default:
                            // Handle other protocols if necessary                          
                            Console.WriteLine("Packet payload doesn't recognized");
                            break;
                    }
                }

                // Print packet content
                PrintPacketContent(packet.Data);
            }
            else
            {
                switch (ethernetPacket.PayloadPacket)
                {
                    case ArpPacket ArpPacket:
                        // Extract ARP information
                        var arpOperation = ArpPacket.Operation;
                        var arpSenderMac = ArpPacket.SenderHardwareAddress;
                        var arpSenderIp = ArpPacket.SenderProtocolAddress;
                        var arpTargetMac = ArpPacket.TargetHardwareAddress;
                        var arpTargetIp = ArpPacket.TargetProtocolAddress;
                        Console.WriteLine($"Protocol: ARP");
                        Console.WriteLine($"ARP Operation: {arpOperation}");
                        Console.WriteLine($"ARP Sender MAC: {arpSenderMac}");
                        Console.WriteLine($"ARP Sender IP: {arpSenderIp}");
                        Console.WriteLine($"ARP Target MAC: {arpTargetMac}");
                        Console.WriteLine($"ARP Target IP: {arpTargetIp}");
                        break;

                    default:
                        break;
                }
            }
        }

        Console.WriteLine();
        return new Packet(Id,DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),"CC:32:E5:72:1A:8D","DC:21:48:DD:96:AE","UDP","82 bytes","Content");
    }

    /// <summary>
    /// Prints the content of the packet.
    /// </summary>
    /// <param name="payloadData">The payload data of the packet.</param>
    public static void PrintPacketContent(byte[] payloadData)
    {
        if (payloadData == null || payloadData.Length == 0)
        {
            return;
        }

        int byteOffset = 0;
        Console.WriteLine();
        while (byteOffset < payloadData.Length)
        {
            int bytesRemaining = Math.Min(16, payloadData.Length - byteOffset);
            byte[] line = new byte[bytesRemaining];
            Array.Copy(payloadData, byteOffset, line, 0, bytesRemaining);

            Console.Write($"0x{byteOffset:X4}: ");
            for (int i = 0; i < bytesRemaining; i++)
            {
                Console.Write($"{line[i]:X2}");

                // Add two spaces after every 8 bytes
                if ((i + 1) % 8 == 0)
                {
                    Console.Write("  ");
                }
                else
                {
                    Console.Write(" ");
                }
            }

            // Calculate padding for alignment of the last line
            int padding = (16 - bytesRemaining) * 3;
            if (bytesRemaining < 16)
            {
                if (bytesRemaining < 8)
                {
                    padding += 3;
                }
                else
                {
                    padding += 2;
                }
            }
            else
            {
                padding += 1; // Add one more character for alignment
            }
            Console.Write(new string(' ', padding));

            // Print printable characters, replace non-printable characters with '.'
            for (int i = 0; i < bytesRemaining; i++)
            {
                char c = (char)line[i];
                if (i == 8) Console.Write(" ");
                Console.Write(char.IsControl(c) ? '.' : c);
            }

            Console.WriteLine();

            byteOffset += 16;
        }
    }

    /// <summary>
    /// Converts a SharpPcap RawCapture to a PacketDotNet.Packet.
    /// </summary>
    /// <param name="rawCapture">The RawCapture to convert.</param>
    /// <returns>The converted Packet.</returns>
    public static PacketDotNet.Packet ConvertPacket(RawCapture rawCapture)
    {
        // Extract raw packet data from SharpPcap RawCapture
        byte[] packetData = rawCapture.Data;

        // Create a PacketDotNet.Packet from the raw packet data
        PacketDotNet.Packet packet = PacketDotNet.Packet.ParsePacket(rawCapture.LinkLayerType, packetData);

        return packet;
    }
}
