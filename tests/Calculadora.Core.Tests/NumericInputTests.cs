using Calculadora.Core;

namespace Calculadora.Core.Tests;

public sealed class NumericInputTests
{
    [Theory]
    [InlineData("1,75", 1.75)]
    [InlineData("1.75", 1.75)]
    [InlineData(" 0,13 ", 0.13)]
    [InlineData("-2.5", -2.5)]
    [InlineData("+3", 3)]
    public void TryParseDecimal_AcceptsSingleDecimalSeparator(string text, double expected)
    {
        Assert.True(NumericInput.TryParseDecimal(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.234,56")]
    [InlineData("1,2,3")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1 000")]
    [InlineData("1e3")]
    public void TryParseDecimal_RejectsUnsupportedInput(string? text)
    {
        Assert.False(NumericInput.TryParseDecimal(text, out var value));
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("40", 40)]
    [InlineData(" 0 ", 0)]
    [InlineData("2147483647", int.MaxValue)]
    public void TryParseInteger_AcceptsDigitsInRange(string text, int expected)
    {
        Assert.True(NumericInput.TryParseInteger(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("40,0")]
    [InlineData("40.0")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("2147483648")]
    [InlineData("1 0")]
    public void TryParseInteger_RejectsNonDigitsOrOutOfRange(string? text)
    {
        Assert.False(NumericInput.TryParseInteger(text, out var value));
        Assert.Equal(0, value);
    }
}
