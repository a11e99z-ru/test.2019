using System.Text;
using Microservices.Shared.Domain;
using Xunit;

namespace Microservices.Shared.Tests.Domain;

public class Str16Tests
{
    [Fact]
    public void Empty_ShouldHaveZeroLength()
    {
        var s = Str16.Empty;
        Assert.Equal(0, s.Length);
        Assert.Equal(string.Empty, s.ToString());
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("1234567890123456")]
    [InlineData("")]
    public void FromAscii_ShouldCreateCorrectString(string input)
    {
        var s = Str16.FromAscii(input);
        Assert.Equal(input, s.ToString());
        Assert.Equal(input.Length, s.Length);
    }

    [Fact]
    public void FromAscii_ShouldTruncateTo16Chars()
    {
        var input = "12345678901234567890";
        var s = Str16.FromAscii(input);
        Assert.Equal(input[..Str16.MaxLength], s.ToString());
        Assert.Equal(Str16.MaxLength, s.Length);
    }

    [Fact]
    public void Equals_ShouldWorkCorrectly()
    {
        var s1 = Str16.FromAscii("abc");
        var s2 = Str16.FromAscii("abc");
        var s3 = Str16.FromAscii("abd");
        var s4 = Str16.FromAscii("ab");

        Assert.True(s1.Equals(s2));
        Assert.True(s1 == s2);
        Assert.False(s1.Equals(s3));
        Assert.False(s1 == s3);
        Assert.False(s1.Equals(s4));
    }

    [Fact]
    public void Index_ShouldWorkCorrectly()
    {
        var s = Str16.FromAscii("aBcBa");

        Assert.Equal(s[0], s[^1]);
        Assert.Equal(s[1], s[^2]);
        Assert.Equal(s[2], s[^3]);
    }

    [Fact]
    public void GetHashCode_ShouldBeSameForEqualStrings()
    {
        var s1 = Str16.FromAscii("test");
        var s2 = Str16.FromAscii("TeSt").ToLower();

        Assert.Equal(s1.GetHashCode(), s2.GetHashCode());
    }

    [Theory]
    [InlineData("Hello", "hello")]
    [InlineData("ABC+def", "abc+def")]
    [InlineData("123!@#", "123!@#")]
    public void ToLower_ShouldWorkCorrectly(string input, string expected)
    {
        var s = Str16.FromAscii(input);
        var lower = s.ToLower();
        Assert.Equal(expected, lower.ToString());
        Assert.Equal(s.Length, lower.Length);
    }

    [Theory]
    [InlineData("hello", "HELLO")]
    [InlineData("abc+DEF", "ABC+DEF")]
    [InlineData("123!@#", "123!@#")]
    public void ToUpper_ShouldWorkCorrectly(string input, string expected)
    {
        var s = Str16.FromAscii(input);
        var upper = s.ToUpper();
        Assert.Equal(expected, upper.ToString());
        Assert.Equal(s.Length, upper.Length);
    }

    [Fact]
    public void Span_ShouldReturnCorrectBytes()
    {
        var s = Str16.FromAscii("abc");
        var span = s.Span;

        Assert.Equal(3, span.Length);
        Assert.Equal((byte)'a', span[0]);
        Assert.Equal((byte)'b', span[1]);
        Assert.Equal((byte)'c', span[2]);
    }

    [Fact]
    public void Constructor_FromByteSpan_ShouldWork()
    {
        var bytes = "abc............."u8;
        
        var s = new Str16(bytes[0..3]);
        Assert.Equal("abc", s.ToString());
        Assert.Equal(3, s.Length);
    }

    [Fact]
    public void Equals_ShouldOnlyDependOnContentAndLength()
    {
        var buf1 = "abcX............"u8;
        var buf2 = "abcY............"u8;
        var buf3 = "abcZ............"u8;

        var s1 = new Str16(buf1[0..3]);
        var s2 = Str16.FromBytes(buf2[0..3]);
        var s3 = Str16.FromBytesUnsafe(buf3[0..3]);

        Assert.True(s1.Equals(s2));
        Assert.True(s1 == s2);
        Assert.True(s1.Equals(s3));
        Assert.True(s1 == s3);
    }
    
    [Theory]
    [InlineData("\"h3llo \",.....", "h3llo ")]
    [InlineData("\"hello 1234,\"].........", "hello 1234,")]
    [InlineData("\"hell0-W0RLD h1-TH3R3\",", "hell0-W0RLD h1-T")]
    public void FromJsonUnsafe_ShouldReadTillQuoteOr15CharsOnly(string input, string expected)
    {
        Span<byte> buf = stackalloc byte[100];
        Encoding.UTF8.GetBytes(input).CopyTo(buf);

        var str = Str16.FromJsonUnsafe(buf[1..], out var consumed);
        
        Assert.Equal(expected, str.ToString());
        Assert.Equal(expected.Length, str.Length);
        Assert.Equal(expected.Length, consumed);
    }
    
    [Theory]
    [InlineData("\"h3llo \",.....")]
    [InlineData("\"hello 1234,\"].........")]
    public void FromJsonUnsafe_SameAsReadingSpanTillQuote(string input)
    {
        Span<byte> buf = stackalloc byte[100];
        Encoding.UTF8.GetBytes(input).CopyTo(buf);

        var str = Str16.FromJsonUnsafe(buf[1..], out var consumed);
        // make span[1..\"]
        buf = buf[1..]; 
        var span = buf[..buf.IndexOf((byte)'\"')];
        
        Assert.True(span.SequenceEqual(str.Span));
    }
}
