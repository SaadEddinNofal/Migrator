using System.Security.Cryptography;
using System.Text;
using Migrator.Domain.Interfaces;

namespace Migrator.Infrastructure.Checksum;

public sealed class Sha256ChecksumService : IChecksumService
{
    public string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return ComputeChecksum(bytes);
    }

    public string ComputeChecksum(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    public bool AreEqual(string checksum1, string checksum2)
    {
        return string.Equals(checksum1, checksum2, StringComparison.OrdinalIgnoreCase);
    }
}