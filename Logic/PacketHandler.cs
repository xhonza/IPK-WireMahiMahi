// Import necessary namespaces
using System;
using System.Text;
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
        string content = "";
        if (ConvertPacket(packet) is EthernetPacket ethernetPacket)
        {
            // Extract Ethernet information
            var timeStamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
            var srcMac = ethernetPacket.SourceHardwareAddress.ToString();
            var dstMac = ethernetPacket.DestinationHardwareAddress.ToString();
            var frameLength = ethernetPacket.Bytes.Length;

            if (ethernetPacket.PayloadPacket is IPPacket ipPacket)
            {
                // Extract IP information
                var srcIp = ipPacket.SourceAddress.ToString();
                var dstIp = ipPacket.DestinationAddress.ToString();

                content = GetPacketContent(packet.Data);

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
                                var descriptionTCP= $"Source IP: {srcIp}\nDestination IP: {dstIp}\nSource port: {srcPort}\nDestination port: {dstPort}";
                                return new Packet(Id,timeStamp,srcMac,dstMac,"TCP",frameLength,content,descriptionTCP);
                            }
                            break;

                        case UdpPacket udpPacket:
                            if (udpPacket.PayloadData != null)
                            {
                                // Extract UDP information
                                var srcPort = udpPacket.SourcePort.ToString();
                                var dstPort = udpPacket.DestinationPort.ToString();
                                var descriptionUDP= $"Source IP: {srcIp}\nDestination IP: {dstIp}\nSource port: {srcPort}\nDestination port: {dstPort}";
                                return new Packet(Id,timeStamp,srcMac,dstMac,"UDP",frameLength,content,descriptionUDP);
                            }
                            break;

                        case IcmpV4Packet icmpV4Packet:
                            // Extract ICMPv4 information
                            var icmpType = icmpV4Packet.TypeCode;
                            var description4= $"Source IP: {srcIp}\nDestination IP: {dstIp}\nType: {icmpType}\n";
                            return new Packet(Id,timeStamp,srcMac,dstMac,"ICMPv4",frameLength,content,description4);
                           

                        case IcmpV6Packet icmpV6Packet:
                            // Extract ICMPv6 information
                            var icmpv6Type = icmpV6Packet.Type;
                            var description6= $"Source IP: {srcIp}\nDestination IP: {dstIp}\nType: {icmpv6Type}\n";
                            return new Packet(Id,timeStamp,srcMac,dstMac,"ICMPv6",frameLength,content,description6);
                            

                        case IgmpV3MembershipReportPacket igmpPacket:
                            var description3= "Source IP: {srcIp}\nDestination IP: {dstIp}\nIGMP Type: Membership Report\n";
                            return new Packet(Id,timeStamp,srcMac,dstMac,"IGMPv3",frameLength,content,description3);
                            // Handle IGMPv3 packet
                            

                        case IgmpV3MembershipQueryPacket igmpPacket:
                            var description32= "Source IP: {srcIp}\nDestination IP: {dstIp}\nIGMP Type: Querry Report\n";
                            return new Packet(Id,timeStamp,srcMac,dstMac,"IGMPv3",frameLength,content,description32);
                            // Handle IGMPv3 packet
                            

                        case IgmpV2Packet igmpPacket:
                            return new Packet(Id,timeStamp,srcMac,dstMac,"IGMPv2",frameLength,content,$"Source IP: {srcIp}\nDestination IP: {dstIp}\n");
                            // Handle IGMPv2 packet
                            

                        default:
                            break;
                    }
                }
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
                        var descriptionArp = $"Operation: {arpOperation}\nSender MAC: {arpSenderMac}\nARP Sender IP: {arpSenderIp}\nTarget MAC: {arpTargetMac}\nTarget IP: {arpTargetIp}\n";
                        return new Packet(Id,timeStamp,srcMac,dstMac,"ARP",frameLength,content,descriptionArp);
                        

                    default:
                        break;
                }
            }
        }
        return new Packet(Id,"","","","",0,"","");       
    }

   public static string GetPacketContent(byte[] payloadData)
{
    if (payloadData == null || payloadData.Length == 0)
    {
        return string.Empty;
    }

    StringBuilder output = new StringBuilder();

    int byteOffset = 0;
    output.AppendLine();
    while (byteOffset < payloadData.Length)
    {
        int bytesRemaining = Math.Min(16, payloadData.Length - byteOffset);
        byte[] line = new byte[bytesRemaining];
        Array.Copy(payloadData, byteOffset, line, 0, bytesRemaining);

        output.AppendFormat("0x{0:X4}: ", byteOffset);
        for (int i = 0; i < bytesRemaining; i++)
        {
            output.AppendFormat("{0:X2}", line[i]);

            // Add two spaces after every 8 bytes
            if ((i + 1) % 8 == 0)
            {
                output.Append("  ");
            }
            else
            {
                output.Append(" ");
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
        output.Append(new string(' ', padding));

        // Print printable characters, replace non-printable characters with '.'
        for (int i = 0; i < bytesRemaining; i++)
        {
            char c = (char)line[i];
            if (i == 8) output.Append(" ");
            output.Append(char.IsControl(c) ? '.' : c);
        }

        output.AppendLine();

        byteOffset += 16;
    }

    return output.ToString();
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
