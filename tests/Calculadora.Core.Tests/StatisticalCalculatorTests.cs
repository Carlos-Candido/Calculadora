using Calculadora.Core;

namespace Calculadora.Core.Tests;

public sealed class StatisticalCalculatorTests
{
    [Theory]
    [InlineData(ConfidenceLevel.Percent90, 1.645, 0.1, 0.033812653881350394, 1.7161873461186496, 1.7838126538813504)]
    [InlineData(ConfidenceLevel.Percent95, 1.96, 0.05, 0.04028741739054516, 1.7097125826094548, 1.7902874173905452)]
    [InlineData(ConfidenceLevel.Percent99, 2.575, 0.01, 0.05292862233706825, 1.6970713776629317, 1.8029286223370683)]
    public void CalculateMean_UsesConfiguredCriticalValue(ConfidenceLevel confidence, double expectedZ, double expectedAlpha, double expectedMargin, double expectedLower, double expectedUpper)
    {
        var result = StatisticalCalculator.CalculateMean(1.75, 0.13, 40, confidence, DeviationKind.Population);
        AssertClose(expectedZ, result.Z);
        AssertClose(expectedAlpha, result.Alpha);
        AssertClose(expectedMargin, result.MarginOfError);
        AssertClose(expectedLower, result.LowerBound);
        AssertClose(expectedUpper, result.UpperBound);
        Assert.False(result.NormalApproximationWarning);
        Assert.False(result.OutsideProportionRange);
    }

    [Fact]
    public void CalculateMean_AcceptsSampleDeviationOnlyAboveThirty()
    {
        Assert.Throws<ArgumentException>(() => StatisticalCalculator.CalculateMean(1, 1, 30, ConfidenceLevel.Percent95, DeviationKind.Sample));
        Assert.Equal(1, StatisticalCalculator.CalculateMean(1, 1, 31, ConfidenceLevel.Percent95, DeviationKind.Sample).Estimate);
        AssertClose(1 / Math.Sqrt(30), StatisticalCalculator.CalculateMean(1, 1, 30, ConfidenceLevel.Percent95, DeviationKind.Population).StandardError);
    }

    [Fact]
    public void CalculateMean_ComputesServerExampleWithoutIntermediateRounding()
    {
        var result = StatisticalCalculator.CalculateMean(320, 45, 50, ConfidenceLevel.Percent95, DeviationKind.Population);
        AssertClose(12.473363620130698, result.MarginOfError);
        AssertClose(307.5266363798693, result.LowerBound);
        AssertClose(332.4733636201307, result.UpperBound);
    }

    [Fact]
    public void CalculateMean_ZeroDeviationProducesDegenerateInterval()
    {
        var result = StatisticalCalculator.CalculateMean(4, 0, 1, ConfidenceLevel.Percent95, DeviationKind.Population);
        AssertClose(4, result.LowerBound);
        AssertClose(4, result.UpperBound);
        AssertClose(0, result.MarginOfError);
    }

    [Theory]
    [InlineData(860, 2200, 0.02039030278793381, 0.3705187881211571, 0.41129939369702473)]
    [InlineData(35, 500, 0.02236462206253439, 0.04763537793746561, 0.0923646220625344)]
    [InlineData(315, 500, 0.05559853752572994, 0.57440146247427, 0.68559853752573)]
    public void CalculateProportion_ComputesExpectedUnroundedInterval(int successes, int sampleSize, double margin, double lower, double upper)
    {
        var confidence = successes == 315 ? ConfidenceLevel.Percent99 : ConfidenceLevel.Percent95;
        var result = StatisticalCalculator.CalculateProportion(successes, sampleSize, confidence);
        AssertClose(margin, result.MarginOfError);
        AssertClose(lower, result.LowerBound);
        AssertClose(upper, result.UpperBound);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 10)]
    public void CalculateProportion_ExtremeCountsAreValidAndWarn(int successes, int n)
    {
        var result = StatisticalCalculator.CalculateProportion(successes, n, ConfidenceLevel.Percent95);
        Assert.True(result.NormalApproximationWarning);
        Assert.False(result.OutsideProportionRange);
    }

    [Fact]
    public void CalculateProportion_PreservesOutOfRangeBoundsAndSetsBothWarnings()
    {
        var result = StatisticalCalculator.CalculateProportion(1, 10, ConfidenceLevel.Percent95);
        Assert.True(result.NormalApproximationWarning);
        Assert.True(result.OutsideProportionRange);
        Assert.True(result.LowerBound < 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateMean_RejectsNonPositiveSampleSize(int n) =>
        Assert.Equal("sampleSize", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateMean(1, 1, n, ConfidenceLevel.Percent95, DeviationKind.Population)).ParamName);

    [Fact]
    public void CalculateProportion_RejectsSuccessesOutsideSample()
    {
        Assert.Equal("successes", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateProportion(-1, 10, ConfidenceLevel.Percent95)).ParamName);
        Assert.Equal("successes", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateProportion(11, 10, ConfidenceLevel.Percent95)).ParamName);
    }

    private static void AssertClose(double expected, double actual)
    {
        Assert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= 1e-10,
            $"Esperado {expected:R}; obtido {actual:R}; tolerância absoluta: 1e-10.");
    }

    [Fact]
    public void Calculator_RejectsInvalidEnumsAndNonFiniteInputs()
    {
        Assert.Equal("confidence", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateMean(1, 1, 40, (ConfidenceLevel)80, DeviationKind.Population)).ParamName);
        Assert.Equal("deviationKind", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateMean(1, 1, 40, ConfidenceLevel.Percent95, (DeviationKind)99)).ParamName);
        Assert.Equal("mean", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateMean(double.NaN, 1, 40, ConfidenceLevel.Percent95, DeviationKind.Population)).ParamName);
        Assert.Equal("standardDeviation", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateMean(1, -1, 40, ConfidenceLevel.Percent95, DeviationKind.Population)).ParamName);
        Assert.Equal("standardDeviation", Assert.Throws<ArgumentOutOfRangeException>(() => StatisticalCalculator.CalculateMean(1, double.PositiveInfinity, 40, ConfidenceLevel.Percent95, DeviationKind.Population)).ParamName);
        Assert.Throws<ArithmeticException>(() => StatisticalCalculator.CalculateMean(double.MaxValue, double.MaxValue, 1, ConfidenceLevel.Percent99, DeviationKind.Population));
    }
}
