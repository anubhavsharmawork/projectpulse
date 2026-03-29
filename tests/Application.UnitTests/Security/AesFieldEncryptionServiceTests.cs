using System;
using FluentAssertions;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.UnitTests.Security;

public class AesFieldEncryptionServiceTests
{
    private static IConfiguration MakeConfig(string? current = null, string? previous = null)
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["FIELD_ENCRYPTION_KEY"]).Returns(current);
        mock.Setup(c => c["FIELD_ENCRYPTION_KEY_PREVIOUS"]).Returns(previous);
        return mock.Object;
    }

    [Fact]
    public void When_NoKeyConfigured_IsDisabledAndPassThrough()
    {
        var config = MakeConfig();
        var logger = new Mock<ILogger<AesFieldEncryptionService>>();
        var svc = new AesFieldEncryptionService(config, logger.Object);

        svc.IsEnabled.Should().BeFalse();
        var tenant = Guid.NewGuid();
        var plain = "hello world";
        svc.Encrypt(plain, tenant).Should().Be(plain);
        svc.Decrypt(plain, tenant).Should().Be(plain);
    }

    [Fact]
    public void EncryptDecrypt_WithMasterKey_WorksForSameTenant()
    {
        // 32 bytes key
        var key = Convert.ToBase64String(new byte[32]);
        var config = MakeConfig(current: key);
        var logger = new Mock<ILogger<AesFieldEncryptionService>>();
        var svc = new AesFieldEncryptionService(config, logger.Object);

        svc.IsEnabled.Should().BeTrue();

        var tenant = Guid.NewGuid();
        var plain = "the quick brown fox";

        var cipher = svc.Encrypt(plain, tenant);
        cipher.Should().StartWith("ENC:");

        var decrypted = svc.Decrypt(cipher, tenant);
        decrypted.Should().Be(plain);
    }

    [Fact]
    public void Decrypt_FallsBackToPreviousKey_WhenRotationConfigured()
    {
        // Use two different keys to simulate rotation
        var key1Bytes = new byte[32];
        key1Bytes[0] = 1;
        var key2Bytes = new byte[32];
        key2Bytes[0] = 2;
        var key1 = Convert.ToBase64String(key1Bytes);
        var key2 = Convert.ToBase64String(key2Bytes);

        var configA = MakeConfig(current: key1);
        var logger = new Mock<ILogger<AesFieldEncryptionService>>();
        var svcA = new AesFieldEncryptionService(configA, logger.Object);

        var tenant = Guid.NewGuid();
        var plain = "rotate-me";
        var cipher = svcA.Encrypt(plain, tenant);

        // Now create a service with a new master key but previous set to key1
        var configB = MakeConfig(current: key2, previous: key1);
        var svcB = new AesFieldEncryptionService(configB, logger.Object);

        // Should decrypt successfully using previous key fallback
        var decrypted = svcB.Decrypt(cipher, tenant);
        decrypted.Should().Be(plain);
    }

    [Fact]
    public void Decrypt_WhenWrongKey_ReturnsCiphertext()
    {
        var key1Bytes = new byte[32];
        key1Bytes[0] = 3;
        var key2Bytes = new byte[32];
        key2Bytes[0] = 4;
        var key1 = Convert.ToBase64String(key1Bytes);
        var key2 = Convert.ToBase64String(key2Bytes);

        var configA = MakeConfig(current: key1);
        var logger = new Mock<ILogger<AesFieldEncryptionService>>();
        var svcA = new AesFieldEncryptionService(configA, logger.Object);

        var tenant = Guid.NewGuid();
        var plain = "sensitive";
        var cipher = svcA.Encrypt(plain, tenant);

        var configB = MakeConfig(current: key2);
        var svcB = new AesFieldEncryptionService(configB, logger.Object);

        // When decrypt fails and no previous key set, Decrypt should return the raw ciphertext string
        var result = svcB.Decrypt(cipher, tenant);
        result.Should().Be(cipher);
    }
}
