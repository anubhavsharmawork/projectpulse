using System;
using FluentAssertions;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Moq;
using Xunit;
using Application.Common.Interfaces;

namespace Application.UnitTests.Security;

public class EncryptedStringConverterTests
{
    [Fact]
    public void Converter_UsesEncryptionService_WithTenantAccessor()
    {
        var tenant = Guid.NewGuid();
        var encMock = new Mock<IEncryptionService>();
        encMock.Setup(e => e.Encrypt(It.IsAny<string>(), tenant)).Returns<string, Guid>((s, t) => "E:" + s);
        encMock.Setup(e => e.Decrypt(It.IsAny<string>(), tenant)).Returns<string, Guid>((s, t) => "D:" + s);

        var conv = new EncryptedStringConverter(encMock.Object, () => tenant);

        var toProvider = conv.ConvertToProviderExpression.Compile();
        var fromProvider = conv.ConvertFromProviderExpression.Compile();

        var plain = "hello";
        var enc = toProvider(plain);
        enc.Should().Be("E:" + plain);

        var dec = fromProvider(enc);
        dec.Should().Be("D:" + enc);

        encMock.Verify(e => e.Encrypt(plain, tenant), Times.Once);
        encMock.Verify(e => e.Decrypt(enc, tenant), Times.Once);
    }
}
