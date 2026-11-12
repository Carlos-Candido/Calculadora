using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Calculadora.Core;

namespace Calculadora.Desktop;

public partial class MainWindow : Window
{
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");
    private bool _isUpdatingControls;
    private CalculationInput? _calculationInput;

    public MainWindow()
    {
        _isUpdatingControls = true;
        InitializeComponent();

        FormulaSelector.SelectionChanged += FormulaSelector_OnSelectionChanged;
        ConfidenceSelector.SelectionChanged += Selection_OnSelectionChanged;
        DeviationKindSelector.SelectionChanged += Selection_OnSelectionChanged;
        MeanInput.TextChanged += Input_OnTextChanged;
        SampleSizeMeanInput.TextChanged += Input_OnTextChanged;
        DeviationInput.TextChanged += Input_OnTextChanged;
        SuccessesInput.TextChanged += Input_OnTextChanged;
        SampleSizeProportionInput.TextChanged += Input_OnTextChanged;
        CalculateButton.Click += CalculateButton_OnClick;
        ClearButton.Click += ClearButton_OnClick;
        Opened += (_, _) => FocusFirstActiveInput();

        UpdateInputVisibility();
        _isUpdatingControls = false;
    }

    private bool IsMeanMode => FormulaSelector.SelectedIndex == 0;

    private void Calculate()
    {
        InvalidateResult();

        var isMean = IsMeanMode;
        var isValid = true;
        Control? firstInvalidControl = null;
        var mean = 0d;
        var standardDeviation = 0d;
        var sampleSize = 0;
        var successes = 0;

        if (isMean)
        {
            if (!NumericInput.TryParseDecimal(MeanInput.Text, out mean))
            {
                SetInputError(MeanError, MeanInput,
                    "Informe um número válido, usando vírgula ou ponto decimal, sem separador de milhares.",
                    ref firstInvalidControl);
                isValid = false;
            }

            if (!NumericInput.TryParseInteger(SampleSizeMeanInput.Text, out sampleSize))
            {
                SetInputError(SampleSizeMeanError, SampleSizeMeanInput,
                    "Informe um número inteiro válido.", ref firstInvalidControl);
                isValid = false;
            }

            if (!NumericInput.TryParseDecimal(DeviationInput.Text, out standardDeviation))
            {
                SetInputError(DeviationError, DeviationInput,
                    "Informe um número válido, usando vírgula ou ponto decimal, sem separador de milhares.",
                    ref firstInvalidControl);
                isValid = false;
            }
        }
        else
        {
            if (!NumericInput.TryParseInteger(SuccessesInput.Text, out successes))
            {
                SetInputError(SuccessesError, SuccessesInput,
                    "Informe um número inteiro válido.", ref firstInvalidControl);
                isValid = false;
            }

            if (!NumericInput.TryParseInteger(SampleSizeProportionInput.Text, out sampleSize))
            {
                SetInputError(SampleSizeProportionError, SampleSizeProportionInput,
                    "Informe um número inteiro válido.", ref firstInvalidControl);
                isValid = false;
            }
        }

        if (!isValid)
        {
            ErrorSummary.Text = "Corrija os campos indicados antes de calcular.";
            ErrorSummary.IsVisible = true;
            firstInvalidControl?.Focus();
            return;
        }

        var confidence = GetConfidenceLevel();
        try
        {
            IntervalResult result;
            if (isMean)
            {
                var deviationKind = DeviationKindSelector.SelectedIndex == 1
                    ? DeviationKind.Sample
                    : DeviationKind.Population;
                result = StatisticalCalculator.CalculateMean(
                    mean, standardDeviation, sampleSize, confidence, deviationKind);
                _calculationInput = new CalculationInput(
                    true, mean, standardDeviation, sampleSize, 0, deviationKind);
            }
            else
            {
                result = StatisticalCalculator.CalculateProportion(successes, sampleSize, confidence);
                _calculationInput = new CalculationInput(
                    false, 0, 0, sampleSize, successes, DeviationKind.Population);
            }

            RenderResult(result);
        }
        catch (ArgumentException exception)
        {
            ShowCoreValidationError(exception.ParamName, exception.Message, isMean);
        }
        catch (ArithmeticException exception)
        {
            ErrorSummary.Text = exception.Message;
            ErrorSummary.IsVisible = true;
        }
    }

    private void Clear()
    {
        _isUpdatingControls = true;
        try
        {
            MeanInput.Text = string.Empty;
            SampleSizeMeanInput.Text = string.Empty;
            DeviationInput.Text = string.Empty;
            SuccessesInput.Text = string.Empty;
            SampleSizeProportionInput.Text = string.Empty;
            ConfidenceSelector.SelectedIndex = 1;
            DeviationKindSelector.SelectedIndex = 0;
            UpdateInputVisibility();
        }
        finally
        {
            _isUpdatingControls = false;
        }

        InvalidateResult();
        FocusFirstActiveInput();
    }

    private void InvalidateResult()
    {
        _calculationInput = null;
        ResultPanel.IsVisible = false;
        ResultDetails.Text = string.Empty;
        IntervalText.Text = string.Empty;
        StepsText.Text = string.Empty;
        WarningText.Text = string.Empty;
        WarningPanel.IsVisible = false;
        SourceNote.Text = string.Empty;

        ClearFieldError(MeanError);
        ClearFieldError(SampleSizeMeanError);
        ClearFieldError(DeviationKindError);
        ClearFieldError(DeviationError);
        ClearFieldError(SuccessesError);
        ClearFieldError(SampleSizeProportionError);
        ErrorSummary.Text = string.Empty;
        ErrorSummary.IsVisible = false;
    }

    private void RenderResult(IntervalResult result)
    {
        var input = _calculationInput
            ?? throw new InvalidOperationException("Os valores de entrada do cálculo não estão disponíveis.");
        var confidence = (int)GetConfidenceLevel();
        var alpha = (result.Alpha * 100).ToString("F0", BrazilianCulture) + "%";
        var z = result.Z.ToString("0.###", BrazilianCulture);

        if (input.IsMean)
        {
            var lower = FormatDecimal(result.LowerBound);
            var upper = FormatDecimal(result.UpperBound);
            var deviationSymbol = input.DeviationKind == DeviationKind.Population ? "σ" : "s";
            var deviationName = input.DeviationKind == DeviationKind.Population
                ? "desvio padrão populacional (σ)"
                : "desvio padrão amostral (s)";

            IntervalText.Text =
                $"Intervalo de confiança de {confidence}% para a média populacional: de {lower} a {upper}.\n" +
                $"IC = ({lower}; {upper})";
            ResultDetails.Text =
                $"Estimativa (x̄): {FormatDecimal(result.Estimate)}\n" +
                $"α: {alpha}\n" +
                $"Z: {z}\n" +
                $"Erro padrão: {FormatDecimal(result.StandardError)}\n" +
                $"Margem de erro: {FormatDecimal(result.MarginOfError)}";
            StepsText.Text =
                $"Fórmula: Em = Z × {deviationSymbol} / √n\n" +
                $"Substituição: Em = {z} × {FormatDecimal(input.StandardDeviation)} / √{input.SampleSize} = {FormatDecimal(result.MarginOfError)}\n\n" +
                "Fórmula: IC = x̄ ± Em\n" +
                $"Substituição: IC = {FormatDecimal(input.Mean)} ± {FormatDecimal(result.MarginOfError)} = ({lower}; {upper})\n\n" +
                $"Entrada usada: {deviationName}.";
            SourceNote.Text = "Fonte: Intervalo de Confiança - Média, página 5.";
        }
        else
        {
            var estimatePercent = FormatPercent(result.Estimate);
            var lower = FormatDecimal(result.LowerBound);
            var upper = FormatDecimal(result.UpperBound);
            var lowerPercent = FormatPercent(result.LowerBound);
            var upperPercent = FormatPercent(result.UpperBound);
            var standardErrorPercent = FormatPercent(result.StandardError);
            var marginPercentagePoints = FormatPercentNumber(result.MarginOfError * 100);
            var successes = FormatInteger(input.Successes);
            var sampleSize = FormatInteger(input.SampleSize);

            IntervalText.Text =
                $"Intervalo de confiança de {confidence}% para a proporção populacional: de {lowerPercent} a {upperPercent}.\n" +
                $"IC = ({lower}; {upper}) = ({lowerPercent}; {upperPercent})";
            ResultDetails.Text =
                $"Estimativa (p): {FormatDecimal(result.Estimate)} ({estimatePercent})\n" +
                $"α: {alpha}\n" +
                $"Z: {z}\n" +
                $"Erro padrão: {FormatDecimal(result.StandardError)} ({standardErrorPercent})\n" +
                $"Margem de erro: {FormatDecimal(result.MarginOfError)} ({marginPercentagePoints} pontos percentuais)";
            StepsText.Text =
                $"Fórmula: p = k/n\n" +
                $"Substituição: p = {successes}/{sampleSize} = {FormatDecimal(result.Estimate)} ({estimatePercent})\n\n" +
                "Fórmula: Ep = Z × √(p × (1−p)/n)\n" +
                $"Substituição: Ep = {z} × √(({successes}/{sampleSize}) × (1−{successes}/{sampleSize})/{sampleSize}) = {FormatDecimal(result.MarginOfError)} ({marginPercentagePoints} pontos percentuais)\n\n" +
                "Fórmula: IC = p ± Ep\n" +
                $"Substituição: IC = {FormatDecimal(result.Estimate)} ± {FormatDecimal(result.MarginOfError)} = ({lower}; {upper})";
            SourceNote.Text =
                "Fonte: Intervalo de Confiança - Proporção, página 2. Os resultados são calculados sem arredondamentos intermediários; alguns exemplos dos slides contêm divergências.";
        }

        var warnings = new List<string>(2);
        if (result.NormalApproximationWarning)
            warnings.Add("A aproximação normal pode ser inadequada: há menos de 5 ocorrências ou de 5 não ocorrências.");
        if (result.OutsideProportionRange)
            warnings.Add("A fórmula normal produziu limites fora de 0% a 100%; interprete com cautela.");
        WarningText.Text = string.Join(Environment.NewLine, warnings);
        WarningPanel.IsVisible = warnings.Count > 0;
        ResultPanel.IsVisible = true;
    }

    private void FormulaSelector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
            return;

        _isUpdatingControls = true;
        try
        {
            MeanInput.Text = string.Empty;
            SampleSizeMeanInput.Text = string.Empty;
            DeviationInput.Text = string.Empty;
            SuccessesInput.Text = string.Empty;
            SampleSizeProportionInput.Text = string.Empty;
            DeviationKindSelector.SelectedIndex = 0;
            UpdateInputVisibility();
        }
        finally
        {
            _isUpdatingControls = false;
        }

        InvalidateResult();
    }

    private void Selection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingControls)
            InvalidateResult();
    }

    private void Input_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_isUpdatingControls)
            InvalidateResult();
    }

    private void CalculateButton_OnClick(object? sender, RoutedEventArgs e) => Calculate();

    private void ClearButton_OnClick(object? sender, RoutedEventArgs e) => Clear();

    private void UpdateInputVisibility()
    {
        MeanFieldsPanel.IsVisible = IsMeanMode;
        ProportionFieldsPanel.IsVisible = !IsMeanMode;
    }

    private void FocusFirstActiveInput()
    {
        if (IsMeanMode)
            MeanInput.Focus();
        else
            SuccessesInput.Focus();
    }

    private ConfidenceLevel GetConfidenceLevel() => ConfidenceSelector.SelectedIndex switch
    {
        0 => ConfidenceLevel.Percent90,
        1 => ConfidenceLevel.Percent95,
        2 => ConfidenceLevel.Percent99,
        _ => throw new InvalidOperationException("O nível de confiança selecionado não é válido.")
    };

    private void SetInputError(
        TextBlock errorText,
        Control input,
        string message,
        ref Control? firstInvalidControl)
    {
        errorText.Text = message;
        errorText.IsVisible = true;
        firstInvalidControl ??= input;
    }

    private void ShowCoreValidationError(string? parameterName, string message, bool isMean)
    {
        var target = GetValidationTarget(parameterName, isMean);
        if (target is { } field)
        {
            field.Error.Text = message;
            field.Error.IsVisible = true;
            field.Control.Focus();
        }

        ErrorSummary.Text = message;
        ErrorSummary.IsVisible = true;
    }

    private (Control Control, TextBlock Error)? GetValidationTarget(string? parameterName, bool isMean)
    {
        if (isMean)
        {
            return parameterName switch
            {
                "mean" => (MeanInput, MeanError),
                "standardDeviation" => (DeviationInput, DeviationError),
                "sampleSize" => (SampleSizeMeanInput, SampleSizeMeanError),
                "deviationKind" => (DeviationKindSelector, DeviationKindError),
                _ => null
            };
        }

        return parameterName switch
        {
            "successes" => (SuccessesInput, SuccessesError),
            "sampleSize" => (SampleSizeProportionInput, SampleSizeProportionError),
            _ => null
        };
    }

    private static void ClearFieldError(TextBlock errorText)
    {
        errorText.Text = string.Empty;
        errorText.IsVisible = false;
    }

    private static string FormatDecimal(double value)
    {
        var formatted = value.ToString("F4", BrazilianCulture);
        return value != 0 && (formatted is "0,0000" or "-0,0000")
            ? value.ToString("E4", BrazilianCulture)
            : formatted;
    }

    private static string FormatPercent(double proportion) =>
        FormatPercentNumber(proportion * 100) + "%";

    private static string FormatPercentNumber(double percentage) 
    {
        var formatted = percentage.ToString("F2", BrazilianCulture);
        return percentage != 0 && (formatted is "0,00" or "-0,00")
            ? percentage.ToString("E4", BrazilianCulture)
            : formatted;
    }

    private static string FormatInteger(int value) => value.ToString("0", BrazilianCulture);

    private sealed record CalculationInput(
        bool IsMean,
        double Mean,
        double StandardDeviation,
        int SampleSize,
        int Successes,
        DeviationKind DeviationKind);
}
