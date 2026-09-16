namespace Migrator.Domain.Interfaces;

public interface IChecksumService
{
    string ComputeChecksum(string content);
    string ComputeChecksum(byte[] data);
    bool AreEqual(string checksum1, string checksum2);
}