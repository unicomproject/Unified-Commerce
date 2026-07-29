using System.Net;
using System.Net.Sockets;

namespace E_POS.LocalPrintAgent.Security;

public sealed class NetworkRangeAllowList
{
    private readonly IReadOnlyList<NetworkRange> _ranges;

    public NetworkRangeAllowList(IEnumerable<string> ranges)
    {
        _ranges = ranges.Select(NetworkRange.Parse).ToArray();
    }

    public bool IsAllowed(IPAddress? address)
    {
        if (address is null) return false;
        if (IPAddress.IsLoopback(address)) return true;
        return _ranges.Any(range => range.Contains(address));
    }

    private sealed record NetworkRange(IPAddress Network, int PrefixLength)
    {
        public static NetworkRange Parse(string value)
        {
            var parts = value.Trim().Split('/', 2);
            if (!IPAddress.TryParse(parts[0], out var network))
                throw new FormatException($"Allowed network range '{value}' has an invalid address.");
            var max = network.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            var prefix = parts.Length == 2 && int.TryParse(parts[1], out var parsed) ? parsed : max;
            if (prefix < 0 || prefix > max)
                throw new FormatException($"Allowed network range '{value}' has an invalid prefix.");
            return new NetworkRange(network, prefix);
        }

        public bool Contains(IPAddress candidate)
        {
            if (candidate.IsIPv4MappedToIPv6) candidate = candidate.MapToIPv4();
            var network = Network.IsIPv4MappedToIPv6 ? Network.MapToIPv4() : Network;
            if (network.AddressFamily != candidate.AddressFamily) return false;
            var left = network.GetAddressBytes();
            var right = candidate.GetAddressBytes();
            var fullBytes = PrefixLength / 8;
            var remainingBits = PrefixLength % 8;
            for (var index = 0; index < fullBytes; index++)
                if (left[index] != right[index]) return false;
            if (remainingBits == 0) return true;
            var mask = (byte)(0xFF << (8 - remainingBits));
            return (left[fullBytes] & mask) == (right[fullBytes] & mask);
        }
    }
}
