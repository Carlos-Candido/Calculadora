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

    [Theory]
    [InlineData(ConfidenceLevel.Percent90, 1.645, 423.0, 422.81640625)]
    [InlineData(ConfidenceLevel.Percent95, 1.96, 601.0, 600.25)]
    [InlineData(ConfidenceLevel.Percent99, 2.575, 1037.0, 1036.03515625)]
    public void CalculateMeanSampleSize_UsesConfiguredCriticalValueAndRoundsUp(
        ConfidenceLevel confidence, double expectedZ, double expectedMinimum, double expectedRaw)
    {
        var result = StatisticalCalculator.CalculateMeanSampleSize(1250, 100, confidence);

        AssertClose(expectedZ, result.Z);
        AssertClose(expectedRaw, result.RawSampleSize);
        Assert.Equal((int)expectedMinimum, result.MinimumSampleSize);
    }

    [Theory]
    [InlineData(0.30, 0.02, 2017, 2016.84)]
    [InlineData(0.15, 0.03, 545, 544.2266666666667)]
    [InlineData(0.50, 0.03, 1068, 1067.111111111111)]
    [InlineData(0.50, 0.02, 2401, 2401)]
    public void CalculateProportionSampleSize_ComputesPlanningExamples(
        double proportion, double marginOfError, int expectedMinimum, double expectedRaw)
    {
        var result = StatisticalCalculator.CalculateProportionSampleSize(
            proportion, marginOfError, ConfidenceLevel.Percent95);

        AssertClose(expectedRaw, result.RawSampleSize);
        Assert.Equal(expectedMinimum, result.MinimumSampleSize);
    }

    [Fact]
    public void CalculateMeanSampleSize_ZeroDeviationHasMinimumOfOne()
    {
        var result = StatisticalCalculator.CalculateMeanSampleSize(0, 100, ConfidenceLevel.Percent95);

        AssertClose(0, result.RawSampleSize);
        Assert.Equal(1, result.MinimumSampleSize);
    }

    [Fact]
    public void CalculateMeanSampleSize_DividesBeforeMultiplyingLargeFiniteInputs()
    {
        var result = StatisticalCalculator.CalculateMeanSampleSize(1e308, 1e308, ConfidenceLevel.Percent95);

        AssertClose(1.96, result.Z);
        AssertClose(1.96 * 1.96, result.RawSampleSize);
        Assert.Equal(4, result.MinimumSampleSize);
    }

    [Theory]
    [InlineData(0.9999999999999999, 1)]
    [InlineData(1, 1)]
    [InlineData(1.0000000000000002, 2)]
    public void CalculateMeanSampleSize_CeilingUsesUnroundedDoubleBoundary(double standardDeviation, int expectedMinimum)
    {
        var result = StatisticalCalculator.CalculateMeanSampleSize(
            standardDeviation, 1.96, ConfidenceLevel.Percent95);

        Assert.Equal(expectedMinimum, result.MinimumSampleSize);
    }

    [Fact]
    public void CalculateMeanSampleSize_RejectsMinimumAboveIntMaximum()
    {
        Assert.Throws<ArithmeticException>(() =>
            StatisticalCalculator.CalculateMeanSampleSize(24000, 1, ConfidenceLevel.Percent95));
    }

    [Fact]
    public void CalculateMeanSampleSize_AcceptsLargestRepresentableMinimumSampleSize()
    {
        var result = StatisticalCalculator.CalculateMeanSampleSize(23400, 1, ConfidenceLevel.Percent95);

        AssertClose(2103506496, result.RawSampleSize);
        Assert.Equal(2103506496, result.MinimumSampleSize);
    }

    [Fact]
    public void CalculateMeanSampleSize_RejectsNonFiniteRawSampleSize()
    {
        Assert.Throws<ArithmeticException>(() =>
            StatisticalCalculator.CalculateMeanSampleSize(1, double.Epsilon, ConfidenceLevel.Percent95));
    }

    [Fact]
    public void CalculateProportionSampleSize_UsesOneAsMinimumWhenRawSizeIsBelowOne()
    {
        var result = StatisticalCalculator.CalculateProportionSampleSize(0.5, 1, ConfidenceLevel.Percent95);

        AssertClose(0.9604, result.RawSampleSize);
        Assert.Equal(1, result.MinimumSampleSize);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    public void CalculateMeanSampleSize_RejectsInvalidStandardDeviation(double standardDeviation)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StatisticalCalculator.CalculateMeanSampleSize(standardDeviation, 1, ConfidenceLevel.Percent95));

        Assert.Equal("standardDeviation", exception.ParamName);
        Assert.Equal("O desvio padrão deve ser finito e não negativo.", exception.Message.Split(" (")[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CalculateMeanSampleSize_RejectsInvalidMarginOfError(double marginOfError)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StatisticalCalculator.CalculateMeanSampleSize(1, marginOfError, ConfidenceLevel.Percent95));

        Assert.Equal("marginOfError", exception.ParamName);
        Assert.Equal("A margem de erro deve ser finita e maior que zero.", exception.Message.Split(" (")[0]);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1.1)]
    public void CalculateProportionSampleSize_RejectsInvalidProportion(double proportion)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StatisticalCalculator.CalculateProportionSampleSize(proportion, 0.1, ConfidenceLevel.Percent95));

        Assert.Equal("proportion", exception.ParamName);
        Assert.Equal(
            "A proporção esperada deve estar entre 0% e 100%, sem incluir os extremos.",
            exception.Message.Split(" (")[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.0000000000000002)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CalculateProportionSampleSize_RejectsInvalidMarginOfError(double marginOfError)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StatisticalCalculator.CalculateProportionSampleSize(0.5, marginOfError, ConfidenceLevel.Percent95));

        Assert.Equal("marginOfError", exception.ParamName);
        Assert.Equal(
            "A margem de erro deve ser maior que 0 e no máximo 100 pontos percentuais.",
            exception.Message.Split(" (")[0]);
    }

    [Fact]
    public void SampleSizeCalculations_RejectInvalidConfidence()
    {
        var meanException = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StatisticalCalculator.CalculateMeanSampleSize(1, 1, (ConfidenceLevel)80));
        var proportionException = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StatisticalCalculator.CalculateProportionSampleSize(0.5, 0.1, (ConfidenceLevel)80));

        Assert.Equal("confidence", meanException.ParamName);
        Assert.Equal("confidence", proportionException.ParamName);
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
