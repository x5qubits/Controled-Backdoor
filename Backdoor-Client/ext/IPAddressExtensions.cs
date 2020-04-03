using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

public static class IPAddressExtensions
{

    public static string AddressAndMaskToBroadcast(string IP, string subnet)
    {
        if (IPAddress.TryParse(IP, out IPAddress address))
        {
            if (IPAddress.TryParse(subnet, out IPAddress subnetMask))
            {
                byte[] ipAdressBytes = address.GetAddressBytes();
                byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

                if (ipAdressBytes.Length != subnetMaskBytes.Length)
                    return null;

                byte[] broadcastAddress = new byte[ipAdressBytes.Length];

                for (int i = 0; i < broadcastAddress.Length; i++)
                {
                    broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
                }
                return new IPAddress(broadcastAddress).ToString();
            }
        }
        return null;
    }
    public static string GetNetwork(string IP, string subnet)
    {
        if (IPAddress.TryParse(IP, out IPAddress address))
        {
            if (IPAddress.TryParse(subnet, out IPAddress subnetMask))
            {
                byte[] ipAdressBytes = address.GetAddressBytes();
                byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

                if (ipAdressBytes.Length != subnetMaskBytes.Length)
                    return null;

                byte[] broadcastAddress = new byte[ipAdressBytes.Length];

                for (int i = 0; i < broadcastAddress.Length; i++)
                {
                    broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
                }
                return new IPAddress(broadcastAddress).ToString();
            }
        }
        return null;
    }
    public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
    {
        byte[] ipAdressBytes = address.GetAddressBytes();
        byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAdressBytes.Length != subnetMaskBytes.Length)
            return null;

        byte[] broadcastAddress = new byte[ipAdressBytes.Length];
        for (int i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
        }
        return new IPAddress(broadcastAddress);
    }

    public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
    {
        byte[] ipAdressBytes = address.GetAddressBytes();
        byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAdressBytes.Length != subnetMaskBytes.Length)
            throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

        byte[] broadcastAddress = new byte[ipAdressBytes.Length];
        for (int i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
        }
        return new IPAddress(broadcastAddress);
    }

    public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
    {
        IPAddress network1 = address.GetNetworkAddress(subnetMask);
        IPAddress network2 = address2.GetNetworkAddress(subnetMask);

        return network1.Equals(network2);
    }

    public static string GetNetwork(this string IP)
    {
        string[] i = IP.Split('.');
        return i[0] + "." + i[1] + "." + i[2] + ".";
    }

    public static long IPtoLong(System.Net.IPAddress theIP) // convert IP to number
    {
        byte[] IPb = theIP.GetAddressBytes(); // get the octets
        long addr = 0; // accumulator for address

        for (int x = 0; x <= 3; x++)
        {
            addr |= (System.Convert.ToInt64(IPb[x]) << (3 - x) * 8);
        }
        return addr;
    }

    public static string LongToIp(long theIP) // convert number back to IP
    {
        byte[] IPb = new byte[4]; // 4 octets
        string addr = ""; // accumulator for address

        long mask8 = MaskFromCidr(8); // create eight bit mask

        for (var x = 0; x <= 3; x++) // get the octets
        {
            IPb[x] = System.Convert.ToByte((theIP & mask8) >> ((3 - x) * 8));
            mask8 = mask8 >> 8;
            addr += IPb[x].ToString() + "."; // add current octet to string
        }
        return addr.TrimEnd('.');
    }

    private static long MaskFromCidr(int CIDR)
    {
        return System.Convert.ToInt64(Math.Pow(2, ((32 - CIDR))) - 1) ^ 4294967295L;
    }

    public static IPAddress CreateByHostBitLength(this int hostpartLength)
    {
        int hostPartLength = hostpartLength;
        int netPartLength = 32 - hostPartLength;

        if (netPartLength < 2)
            throw new ArgumentException("Number of hosts is to large for IPv4");

        byte[] binaryMask = new byte[4];

        for (int i = 0; i < 4; i++)
        {
            if (i * 8 + 8 <= netPartLength)
                binaryMask[i] = (byte)255;
            else if (i * 8 > netPartLength)
                binaryMask[i] = (byte)0;
            else
            {
                int oneLength = netPartLength - i * 8;
                string binaryDigit =
                    string.Empty.PadLeft(oneLength, '1').PadRight(8, '0');
                binaryMask[i] = Convert.ToByte(binaryDigit, 2);
            }
        }
        return new IPAddress(binaryMask);
    }

    public static IPAddress CreateByNetBitLength(this int netpartLength)
    {
        int hostPartLength = 32 - netpartLength;
        return CreateByHostBitLength(hostPartLength);
    }

    public static IPAddress CreateByHostNumber(this int numberOfHosts)
    {
        int maxNumber = numberOfHosts + 1;

        string b = Convert.ToString(maxNumber, 2);

        return CreateByHostBitLength(b.Length);
    }

    public static int GetNetMask(this IPAddress IP, IPAddress NetMask)
    {
        long ipL = IPtoLong(IP);
        long maskL = IPtoLong(NetMask);

        // Convert  Mask to CIDR(1-30)
        long oneBit = 0x80000000L;
        int CIDR = 0;

        for (int x = 31; x >= 0; x += -1)
        {
            if ((maskL & oneBit) == oneBit)
                CIDR += 1;
            else
                break;
            oneBit = oneBit >> 1;
        }
        return CIDR;
    }
    static Regex r;
    static public bool ValidateMac(this string mac)
    {
        if (r == null)
            r = new Regex(@"^[0-9a-fA-F]{2}(((:[0-9a-fA-F]{2}){5})|((:[0-9a-fA-F]{2}){5}))$");

        if (r.IsMatch(mac))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    static public bool ValidateIP(this string ipString)
    {
        if (IPAddress.TryParse(ipString, out IPAddress address))
        {
            switch (address.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return true;
                case AddressFamily.InterNetworkV6:
                    // we have IPv6
                    break;
                default:
                    // umm... yeah... I'm going to need to take your red packet and...
                    break;
            }
        }
        return false;
    }
    static public string ConvertToValidDNS(this string ipString)
    {
        if (ipString.Contains(","))
        {
            return string.Join(" ", ipString.Split(',').Where(c => c != null).ToArray());
        }
        return ipString;
    }

    private static readonly Random Random = new Random();

    public static string GetSignatureRandomMac(string generic = "AA")
    {
        string[] macBytes = new[]
        {
            generic,
            generic,
            generic,
            Random.Next(1, 256).ToString("X"),
            Random.Next(1, 256).ToString("X"),
            Random.Next(1, 256).ToString("X")
        };

        return string.Join("-", macBytes);
    }

    public static string GetRandomMac()
    {
        string[] macBytes = new[]
        {
            Random.Next(1, 256).ToString("X"),
            Random.Next(1, 256).ToString("X"),
            Random.Next(1, 256).ToString("X"),
            Random.Next(1, 256).ToString("X"),
            Random.Next(1, 256).ToString("X"),
            Random.Next(1, 256).ToString("X")
        };

        return string.Join(":", macBytes);
    }

}