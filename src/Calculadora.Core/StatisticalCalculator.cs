namespace Calculadora.Core;

public enum ConfidenceLevel
{
    Percent90 = 90,
    Percent95 = 95,
    Percent99 = 99
}

public enum DeviationKind
{
    Population,
    Sample
}

public sealed record IntervalResult(
    double Estimate,
    double Alpha,
    double Z,
    double StandardError,
    double MarginOfError,
    double LowerBound,
    double UpperBound,
    bool NormalApproximationWarning,
    bool OutsideProportionRange);

public static class StatisticalCalculator
{
    public static IntervalResult CalculateMean(
        double mean, double standardDeviation, int sampleSize,
        ConfidenceLevel confidence, DeviationKind deviationKind)
    {
        ValidateConfidence(confidence);
        if (!Enum.IsDefined(deviationKind))
            throw new ArgumentOutOfRangeException(nameof(deviationKind), "O tipo de desvio padrão informado não é válido.");
        if (!double.IsFinite(mean))
            throw new ArgumentOutOfRangeException(nameof(mean), "A média deve ser um número finito.");
        if (!double.IsFinite(standardDeviation) || standardDeviation < 0)
            throw new ArgumentOutOfRangeException(nameof(standardDeviation), "O desvio padrão deve ser finito e não negativo.");
        ValidateSampleSize(sampleSize);
        if (deviationKind == DeviationKind.Sample && sampleSize <= 30)
            throw new ArgumentException("Para usar o desvio padrão amostral neste método, n deve ser maior que 30.", nameof(sampleSize));

        var (z, alpha) = GetCriticalValues(confidence);
        var standardError = standardDeviation / Math.Sqrt(sampleSize);
        var margin = z * standardError;
        var lower = mean - margin;
        var upper = mean + margin;
        EnsureFinite(standardError);
        EnsureFinite(margin);
        EnsureFinite(lower);
        EnsureFinite(upper);
        return new IntervalResult(mean, alpha, z, standardError, margin, lower, upper, false, false);
    }

    public static IntervalResult CalculateProportion(int successes, int sampleSize, ConfidenceLevel confidence)
    {
        ValidateConfidence(confidence);
        ValidateSampleSize(sampleSize);
        if (successes < 0 || successes > sampleSize)
            throw new ArgumentOutOfRangeException(nameof(successes), "O número de ocorrências deve estar entre zero e o tamanho da amostra.");

        var (z, alpha) = GetCriticalValues(confidence);
        var estimate = (double)successes / sampleSize;
        var standardError = Math.Sqrt(estimate * (1 - estimate) / sampleSize);
        var margin = z * standardError;
        var lower = estimate - margin;
        var upper = estimate + margin;
        EnsureFinite(estimate);
        EnsureFinite(standardError);
        EnsureFinite(margin);
        EnsureFinite(lower);
        EnsureFinite(upper);
        return new IntervalResult(estimate, alpha, z, standardError, margin, lower, upper,
            successes < 5 || sampleSize - successes < 5, lower < 0 || upper > 1);
    }

    private static void ValidateSampleSize(int sampleSize)
    {
        if (sampleSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleSize), "O tamanho da amostra deve ser maior que zero.");
    }

    private static void ValidateConfidence(ConfidenceLevel confidence)
    {
        if (!Enum.IsDefined(confidence))
            throw new ArgumentOutOfRangeException(nameof(confidence), "O nível de confiança informado não é válido.");
    }

    private static (double Z, double Alpha) GetCriticalValues(ConfidenceLevel confidence) => confidence switch
    {
        ConfidenceLevel.Percent90 => (1.645, 0.10),
        ConfidenceLevel.Percent95 => (1.96, 0.05),
        ConfidenceLevel.Percent99 => (2.575, 0.01),
        _ => throw new ArgumentOutOfRangeException(nameof(confidence), "O nível de confiança informado não é válido.")
    };

    private static void EnsureFinite(double value)
    {
        if (!double.IsFinite(value))
            throw new ArithmeticException("Os valores informados excedem o limite numérico do cálculo.");
    }
}
