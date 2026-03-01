using System.Globalization;
using System.Text;
using System.Text.Json;
using API.Converters;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.Converters;

public class Iso8601DateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public Iso8601DateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new Iso8601DateTimeConverter());
    }

    [Fact]
    public void Write_UtcDateTime_ShouldFormatWithZSuffix()
    {
        var dt = new DateTime(2026, 3, 15, 10, 30, 45, 123, DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(dt, _options);
        json.Should().Be("\"2026-03-15T10:30:45.123Z\"");
    }

    [Fact]
    public void Write_LocalDateTime_ShouldConvertToUtc()
    {
        var dt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Local);
        var json = JsonSerializer.Serialize(dt, _options);
        json.Should().EndWith("Z\"");
    }

    [Fact]
    public void Write_UnspecifiedKind_ShouldConvertToUtc()
    {
        var dt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var json = JsonSerializer.Serialize(dt, _options);
        json.Should().Contain("Z\"");
    }

    [Fact]
    public void Read_Iso8601String_ShouldParseAsUtc()
    {
        var json = "\"2026-03-15T10:30:45.123Z\"";
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(10);
    }

    [Fact]
    public void Read_DateOnlyString_ShouldParseAsUtcMidnight()
    {
        var json = "\"2026-03-15\"";
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Year.Should().Be(2026);
        result.Hour.Should().Be(0);
    }

    [Fact]
    public void Read_EmptyString_ShouldReturnDefault()
    {
        var json = "\"\"";
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);
        result.Should().Be(default(DateTime));
    }

    [Fact]
    public void Read_InvalidString_ShouldThrowJsonException()
    {
        var json = "\"not-a-date\"";
        var act = () => JsonSerializer.Deserialize<DateTime>(json, _options);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Read_Iso8601WithOffset_ShouldParseAsUtc()
    {
        var json = "\"2026-03-15T10:30:45+05:00\"";
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);
        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Hour.Should().Be(5); // 10:30 - 5:00 offset = 05:30 UTC
    }
}

public class Iso8601NullableDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public Iso8601NullableDateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new Iso8601NullableDateTimeConverter());
    }

    [Fact]
    public void Write_NullValue_ShouldWriteNull()
    {
        DateTime? dt = null;
        var json = JsonSerializer.Serialize(dt, _options);
        json.Should().Be("null");
    }

    [Fact]
    public void Write_HasValue_ShouldFormatWithZSuffix()
    {
        DateTime? dt = new DateTime(2026, 3, 15, 10, 30, 45, 123, DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(dt, _options);
        json.Should().Be("\"2026-03-15T10:30:45.123Z\"");
    }

    [Fact]
    public void Read_NullToken_ShouldReturnNull()
    {
        var json = "null";
        var result = JsonSerializer.Deserialize<DateTime?>(json, _options);
        result.Should().BeNull();
    }

    [Fact]
    public void Read_EmptyString_ShouldReturnNull()
    {
        var json = "\"\"";
        var result = JsonSerializer.Deserialize<DateTime?>(json, _options);
        result.Should().BeNull();
    }

    [Fact]
    public void Read_ValidIso8601_ShouldReturnUtcDateTime()
    {
        var json = "\"2026-03-15T10:30:45.123Z\"";
        var result = JsonSerializer.Deserialize<DateTime?>(json, _options);
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Read_DateOnly_ShouldReturnUtcDateTime()
    {
        var json = "\"2026-03-15\"";
        var result = JsonSerializer.Deserialize<DateTime?>(json, _options);
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Read_InvalidString_ShouldThrowJsonException()
    {
        var json = "\"not-a-date\"";
        var act = () => JsonSerializer.Deserialize<DateTime?>(json, _options);
        act.Should().Throw<JsonException>();
    }
}
