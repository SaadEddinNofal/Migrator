using System.Security.Cryptography;
using System.Text;

namespace Migrator.Domain.ValueObjects;

public sealed class ChecksumValue : IEquatable<ChecksumValue>
{
    public string Value { get; }

    private ChecksumValue(string value)
    {
        Value = value;
    }

    public static ChecksumValue Create(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ChecksumValue(ComputeSha256(Encoding.UTF8.GetBytes(content)));
    }

    public static ChecksumValue CreateFromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new ChecksumValue(ComputeSha256(data));
    }

    public static ChecksumValue FromHex(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        return new ChecksumValue(hex.ToLowerInvariant());
    }

    private static string ComputeSha256(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool Equals(ChecksumValue? other)
    {
        return other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ChecksumValue);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(ChecksumValue? left, ChecksumValue? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChecksumValue? left, ChecksumValue? right)
    {
        return !Equals(left, right);
    }
}