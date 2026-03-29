using Application.Common.Exceptions;
using FluentAssertions;
using System;
using Xunit;

namespace Application.UnitTests.Common;

public class TenantExceptionsTests
{
    [Fact]
    public void TenantNotFoundException_MessageFromId()
    {
        var id = Guid.NewGuid();
        var ex = new TenantNotFoundException(id);
        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void TenantLimitExceededException_PropertiesSet()
    {
        var ex = new TenantLimitExceededException("projects", 5, 10);
        ex.Resource.Should().Be("projects");
        ex.CurrentCount.Should().Be(5);
        ex.MaxCount.Should().Be(10);
        ex.Message.Should().Contain("projects");
    }

    [Fact]
    public void TenantInactiveException_DefaultMessage()
    {
        var ex = new TenantInactiveException();
        ex.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void InvalidTenantContextException_DefaultMessage()
    {
        var ex = new InvalidTenantContextException();
        ex.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void UnauthorizedTenantAccessException_DefaultMessage()
    {
        var ex = new UnauthorizedTenantAccessException();
        ex.Message.Should().NotBeNullOrEmpty();
    }
}
