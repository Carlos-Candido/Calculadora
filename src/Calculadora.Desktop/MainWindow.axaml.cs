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
    private TextBox? _activeInput;
    private int _selectionStart;
    private int _selectionEnd;

    public MainWindow()
    {
        _isUpdatingControls = true;
        InitializeComponent();

        FormulaSelector.SelectionChanged += FormulaSelector_OnSelectionChanged;
        ConfidenceSelector.SelectionChanged += Selection_OnSelectionChanged;
        DeviationKindSelector.SelectionChanged += Selection_OnSelectionChanged;
        RegisterNumericInput(MeanInput);
        RegisterNumericInput(SampleSizeMeanInput);
        RegisterNumericInput(DeviationInput);
        RegisterNumericInput(SuccessesInput);
        RegisterNumericInput(SampleSizeProportionInput);
        RegisterNumericInput(PlanningDeviationInput);
        RegisterNumericInput(MeanMarginInput);
        RegisterNumericInput(ExpectedProportionInput);
        RegisterNumericInput(ProportionMarginInput);
        CalculateButton.Click += CalculateButton_OnClick;
        ClearButton.Click += ClearButton_OnClick;
        BackspaceButton.Click += BackspaceButton_OnClick;
        ClearEntryButton.Click += ClearEntryButton_OnClick;
        SignButton.Click += SignButton_OnClick;
        DecimalButton.Click += DecimalButton_OnClick;
        Key0Button.Click += (_, _) => InsertDigit('0');
        Key1Button.Click += (_, _) => InsertDigit('1');
        Key2Button.Click += (_, _) => InsertDigit('2');
        Key3Button.Click += (_, _) => InsertDigit('3');
        Key4Button.Click += (_, _) => InsertDigit('4');
        Key5Button.Click += (_, _) => InsertDigit('5');
        Key6Button.Click += (_, _) => InsertDigit('6');
        Key7Button.Click += (_, _) => InsertDigit('7');
        Key8Button.Click += (_, _) => InsertDigit('8');
        Key9Button.Click += (_, _) => InsertDigit('9');
        Opened += (_, _) => FocusFirstActiveInput();

        UpdateInputVisibility();
        UpdateKeypadCapabilities();
        _isUpdatingControls = false;
    }

    private CalculationMode Mode => FormulaSelector.SelectedIndex switch
    {
        0 => CalculationMode.MeanInterval,
        1 => CalculationMode.ProportionInterval,
        2 => CalculationMode.MeanSampleSize,
        3 => CalculationMode.ProportionSampleSize,
        _ => CalculationMode.MeanInterval
    };

    private void Calculate()
    {
        InvalidateResult();
        var mode = Mode;
        if (mode is CalculationMode.MeanSampleSize or CalculationMode.ProportionSampleSize)
        {
            CalculateSampleSize(mode);
            return;
        }

        var isMean = mode == CalculationMode.MeanInterval;
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
    private void CalculateSampleSize(CalculationMode mode)
    {
        var isMean = mode == CalculationMode.MeanSampleSize;
        var isProportion = mode == CalculationMode.ProportionSampleSize;
        var isValid = true;
        Control? firstInvalidControl = null;
        var standardDeviation = 0d;
        var proportion = 0.5d;
        var marginOfError = 0d;
        var assumedProportion = isProportion && string.IsNullOrWhiteSpace(ExpectedProportionInput.Text);
        const string decimalInputError = "Informe um número válido, usando vírgula ou ponto decimal, sem separador de milhares.";

        if (isMean)
        {
            if (!NumericInput.TryParseDecimal(PlanningDeviationInput.Text, out standardDeviation))
            {
                SetInputError(PlanningDeviationError, PlanningDeviationInput, decimalInputError, ref firstInvalidControl);
                isValid = false;
            }

            if (!NumericInput.TryParseDecimal(MeanMarginInput.Text, out marginOfError))
            {
                SetInputError(MeanMarginError, MeanMarginInput, decimalInputError, ref firstInvalidControl);
                isValid = false;
            }
        }
        else
        {
            if (!assumedProportion)
            {
                if (!NumericInput.TryParseDecimal(ExpectedProportionInput.Text, out var enteredProportion))
                {
                    SetInputError(ExpectedProportionError, ExpectedProportionInput, decimalInputError, ref firstInvalidControl);
                    isValid = false;
                }
                else
                {
                    proportion = enteredProportion / 100d;
                }
            }

            if (!NumericInput.TryParseDecimal(ProportionMarginInput.Text, out var enteredMargin))
            {
                SetInputError(ProportionMarginError, ProportionMarginInput, decimalInputError, ref firstInvalidControl);
                isValid = false;
            }
            else
            {
                marginOfError = enteredMargin / 100d;
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
            var result = isMean
                ? StatisticalCalculator.CalculateMeanSampleSize(standardDeviation, marginOfError, confidence)
                : assumedProportion
                    ? StatisticalCalculator.CalculateProportionSampleSize(marginOfError, confidence)
                    : StatisticalCalculator.CalculateProportionSampleSize(proportion, marginOfError, confidence);
            RenderSampleSizeResult(
                result, mode, confidence, isMean ? standardDeviation : proportion, marginOfError, assumedProportion);
        }
        catch (ArgumentException exception)
        {
            var target = exception.ParamName switch
            {
                "standardDeviation" => (Control: (Control)PlanningDeviationInput, Error: PlanningDeviationError),
                "marginOfError" when isMean => (Control: (Control)MeanMarginInput, Error: MeanMarginError),
                "marginOfError" => (Control: (Control)ProportionMarginInput, Error: ProportionMarginError),
                "proportion" => (Control: (Control)ExpectedProportionInput, Error: ExpectedProportionError),
                _ => (Control: (Control?)null, Error: (TextBlock?)null)
            };
            if (target.Control is not null && target.Error is not null)
            {
                target.Error.Text = exception.Message;
                target.Error.IsVisible = true;
                target.Control.Focus();
            }

            ErrorSummary.Text = exception.Message;
            ErrorSummary.IsVisible = true;
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
            ClearNumericInputs();
            ConfidenceSelector.SelectedIndex = 1;
            DeviationKindSelector.SelectedIndex = 0;
            UpdateInputVisibility();
        }
        finally
        {
            _isUpdatingControls = false;
        }

        _activeInput = null;
        _selectionStart = 0;
        _selectionEnd = 0;
        InvalidateResult();
        FocusFirstActiveInput();
    }
    private void ClearNumericInputs()
    {
        MeanInput.Text = string.Empty;
        SampleSizeMeanInput.Text = string.Empty;
        DeviationInput.Text = string.Empty;
        SuccessesInput.Text = string.Empty;
        SampleSizeProportionInput.Text = string.Empty;
        PlanningDeviationInput.Text = string.Empty;
        MeanMarginInput.Text = string.Empty;
        ExpectedProportionInput.Text = string.Empty;
        ProportionMarginInput.Text = string.Empty;
    }

    private void InvalidateResult()
    {
        _calculationInput = null;
        ResultPanel.IsVisible = true;
        DetailsPanel.IsVisible = false;
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
        ClearFieldError(PlanningDeviationError);
        ClearFieldError(MeanMarginError);
        ClearFieldError(ExpectedProportionError);
        ClearFieldError(ProportionMarginError);
        ErrorSummary.Text = string.Empty;
        ErrorSummary.IsVisible = false;
        UpdateEntryDisplay();
    }

    private void RenderResult(IntervalResult result)
    {
        var input = _calculationInput
            ?? throw new InvalidOperationException("Os valores de entrada do cálculo não estão disponíveis.");
        var confidence = (int)GetConfidenceLevel();
        var alpha = (result.Alpha * 100).ToString("F0", BrazilianCulture) + "%";
        var z = result.Z.ToString("0.###", BrazilianCulture);

        DisplayModeText.Text = $"INTERVALO DE CONFIANÇA · {confidence}%";
        DisplayContextText.Text = input.IsMean ? "Média populacional" : "Proporção populacional";
        if (input.IsMean)
        {
            var lower = FormatDecimal(result.LowerBound);
            var upper = FormatDecimal(result.UpperBound);
            var deviationSymbol = input.DeviationKind == DeviationKind.Population ? "σ" : "s";
            var deviationName = input.DeviationKind == DeviationKind.Population
                ? "desvio padrão populacional (σ)"
                : "desvio padrão amostral (s)";

            DisplayText.Text = $"({lower}; {upper})";
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

            DisplayText.Text = $"({lowerPercent}; {upperPercent})";
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
        DetailsPanel.IsVisible = true;
        ResultPanel.IsVisible = true;
    }
    private void RenderSampleSizeResult(
        SampleSizeResult result,
        CalculationMode mode,
        ConfidenceLevel confidence,
        double parameter,
        double marginOfError,
        bool assumedProportion)
    {
        var confidencePercent = (int)confidence;
        var z = result.Z.ToString("G", BrazilianCulture);
        var parameterValue = parameter.ToString("G", BrazilianCulture);
        var marginValue = marginOfError.ToString("G", BrazilianCulture);
        var rawSampleSize = result.RawSampleSize.ToString("G", BrazilianCulture);
        var minimumSampleSize = result.MinimumSampleSize.ToString("N0", BrazilianCulture);
        var isMean = mode == CalculationMode.MeanSampleSize;

        DisplayModeText.Text = $"TAMANHO AMOSTRAL · {confidencePercent}%";
        DisplayContextText.Text = isMean ? "Média populacional" : "Proporção populacional";
        DisplayText.Text = minimumSampleSize;
        IntervalText.Text = $"Tamanho mínimo da amostra: {minimumSampleSize} observações.";

        ResultDetails.Text = isMean
            ? $"Z: {z}\nDesvio padrão populacional (σ): {parameterValue}\nMargem de erro máxima (E): {marginValue}"
            : $"Z: {z}\nProporção esperada (p): {(parameter * 100d).ToString("G", BrazilianCulture)}%\nMargem de erro: {(marginOfError * 100d).ToString("G", BrazilianCulture)} pontos percentuais";

        StepsText.Text = isMean
            ? $"Fórmula: n = (Z × σ / E)²\n" +
              $"Substituição: n = ({z} × {parameterValue} / {marginValue})² = {rawSampleSize}\n" +
              $"n mínimo = máx(1; arredondar para cima(n bruto)) = {minimumSampleSize}"
            : $"Fórmula: n = Z² × p × (1−p) / E²\n" +
              $"Substituição: n = {z}² × {parameterValue} × (1−{parameterValue}) / {marginValue}² = {rawSampleSize}\n" +
              $"n mínimo = máx(1; arredondar para cima(n bruto)) = {minimumSampleSize}";

        WarningText.Text = assumedProportion
            ? "Proporção esperada não informada; usado 50% (estimativa conservadora)."
            : string.Empty;
        WarningPanel.IsVisible = assumedProportion;
        SourceNote.Text = "Fonte: Tamanho da Amostra — AMS, páginas 3–7 e 9.";
        DetailsPanel.IsVisible = true;
        ResultPanel.IsVisible = true;
    }

    private void RegisterNumericInput(TextBox input)
    {
        input.ClearSelectionOnLostFocus = false;
        input.GotFocus += (_, _) => Input_OnGotFocus(input);
        input.LostFocus += (_, _) => CaptureInputSelection(input);
        input.TextChanged += Input_OnTextChanged;
    }

    private void Input_OnGotFocus(TextBox input)
    {
        if (ReferenceEquals(_activeInput, input))
            RestoreInputSelection(input);
        else
            CaptureSelection(input);

        _activeInput = input;
        UpdateKeypadCapabilities();
        UpdateEntryDisplay();
    }

    private void CaptureInputSelection(TextBox input)
    {
        if (ReferenceEquals(_activeInput, input))
            CaptureSelection(input);
    }

    private void CaptureSelection(TextBox input)
    {
        var textLength = input.Text?.Length ?? 0;
        _selectionStart = Math.Clamp(input.SelectionStart, 0, textLength);
        _selectionEnd = Math.Clamp(input.SelectionEnd, 0, textLength);
    }

    private void RestoreInputSelection(TextBox input)
    {
        var textLength = input.Text?.Length ?? 0;
        _selectionStart = Math.Clamp(_selectionStart, 0, textLength);
        _selectionEnd = Math.Clamp(_selectionEnd, 0, textLength);
        input.SelectionStart = _selectionStart;
        input.SelectionEnd = _selectionEnd;
    }

    private void SetInputSelection(TextBox input, int start, int end)
    {
        var textLength = input.Text?.Length ?? 0;
        _selectionStart = Math.Clamp(start, 0, textLength);
        _selectionEnd = Math.Clamp(end, 0, textLength);
        input.SelectionStart = _selectionStart;
        input.SelectionEnd = _selectionEnd;
    }

    private void InsertDigit(char digit)
    {
        var input = GetActiveInputForEditing();
        if (input is not null)
            ReplaceSelection(input, digit.ToString());
    }

    private void InsertDecimalSeparator()
    {
        var input = GetActiveInputForEditing();
        if (input is null || !IsDecimalInput(input))
            return;

        var text = input.Text ?? string.Empty;
        var (start, end) = GetSelectionRange(input);
        var remainingText = text.Remove(start, end - start);
        if (remainingText.IndexOf(',') >= 0 || remainingText.IndexOf('.') >= 0)
            return;

        ReplaceSelection(input, ",");
    }

    private void ToggleSign()
    {
        var input = GetActiveInputForEditing();
        if (input is null || !IsDecimalInput(input))
            return;

        var originalText = input.Text ?? string.Empty;
        var (start, end) = GetSelection(input);
        var text = originalText.Trim();
        var removedPrefixLength = originalText.Length - originalText.TrimStart().Length;
        start = Math.Clamp(start - removedPrefixLength, 0, text.Length);
        end = Math.Clamp(end - removedPrefixLength, 0, text.Length);

        string toggledText;
        if (text.StartsWith("-", StringComparison.Ordinal))
        {
            toggledText = text[1..];
            start = Math.Max(0, start - 1);
            end = Math.Max(0, end - 1);
        }
        else if (text.StartsWith("+", StringComparison.Ordinal))
        {
            toggledText = "-" + text[1..];
        }
        else
        {
            toggledText = "-" + text;
            start++;
            end++;
        }

        input.Text = toggledText;
        SetInputSelection(input, start, end);
        UpdateEntryDisplay();
    }

    private void Backspace()
    {
        var input = GetActiveInputForEditing();
        if (input is null)
            return;

        var text = input.Text ?? string.Empty;
        var (start, end) = GetSelectionRange(input);
        if (start == end)
        {
            if (start == 0)
                return;

            start--;
        }

        input.Text = text.Remove(start, end - start);
        SetInputSelection(input, start, start);
        UpdateEntryDisplay();
    }

    private void ClearActiveEntry()
    {
        var input = GetActiveInputForEditing();
        if (input is null)
            return;

        input.Text = string.Empty;
        SetInputSelection(input, 0, 0);
        UpdateEntryDisplay();
    }

    private void ReplaceSelection(TextBox input, string insertedText)
    {
        var text = input.Text ?? string.Empty;
        var (start, end) = GetSelectionRange(input);
        input.Text = string.Concat(text.AsSpan(0, start), insertedText, text.AsSpan(end));
        var caret = start + insertedText.Length;
        SetInputSelection(input, caret, caret);
        UpdateEntryDisplay();
    }

    private (int Start, int End) GetSelection(TextBox input)
    {
        if (!ReferenceEquals(_activeInput, input))
            CaptureSelection(input);
        else if (!input.IsFocused)
            RestoreInputSelection(input);
        else
            CaptureSelection(input);

        return (_selectionStart, _selectionEnd);
    }

    private (int Start, int End) GetSelectionRange(TextBox input)
    {
        var (start, end) = GetSelection(input);
        return (Math.Min(start, end), Math.Max(start, end));
    }

    private TextBox? GetActiveInputForEditing()
    {
        if (_activeInput is not null && IsInputActive(_activeInput))
        {
            var input = _activeInput;
            input.Focus();
            RestoreInputSelection(input);
            return input;
        }

        FocusFirstActiveInput();
        if (_activeInput is null || !IsInputActive(_activeInput))
            return null;

        var firstInput = _activeInput;
        firstInput.Focus();
        RestoreInputSelection(firstInput);
        return firstInput;
    }

    private void UpdateEntryDisplay()
    {
        if (DisplayModeText is null || DisplayContextText is null || DisplayText is null)
            return;

        var input = _activeInput is not null && IsInputActive(_activeInput)
            ? _activeInput
            : GetFirstActiveInput();
        DisplayModeText.Text = "ENTRADA";
        DisplayContextText.Text = input switch
        {
            _ when ReferenceEquals(input, MeanInput) => "Média amostral (x̄)",
            _ when ReferenceEquals(input, SampleSizeMeanInput) => "Tamanho da amostra (n)",
            _ when ReferenceEquals(input, DeviationInput) => "Desvio padrão",
            _ when ReferenceEquals(input, SuccessesInput) => "Ocorrências de interesse (k)",
            _ when ReferenceEquals(input, SampleSizeProportionInput) => "Tamanho da amostra (n)",
            _ when ReferenceEquals(input, PlanningDeviationInput) => "Desvio padrão populacional (σ)",
            _ when ReferenceEquals(input, MeanMarginInput) => "Margem de erro máxima (E)",
            _ when ReferenceEquals(input, ExpectedProportionInput) => "Proporção esperada (%)",
            _ when ReferenceEquals(input, ProportionMarginInput) => "Margem de erro (p.p.)",
            _ => "Entrada numérica"
        };
        DisplayText.Text = string.IsNullOrEmpty(input.Text) ? "—" : input.Text;
    }

    private void UpdateKeypadCapabilities()
    {
        if (SignButton is null || DecimalButton is null)
            return;

        var decimalInputActive = _activeInput is not null && IsInputActive(_activeInput) && IsDecimalInput(_activeInput);
        SignButton.IsEnabled = decimalInputActive;
        DecimalButton.IsEnabled = decimalInputActive;
    }

    private bool IsInputActive(TextBox input) => Mode switch
    {
        CalculationMode.MeanInterval =>
            ReferenceEquals(input, MeanInput) || ReferenceEquals(input, SampleSizeMeanInput) || ReferenceEquals(input, DeviationInput),
        CalculationMode.ProportionInterval =>
            ReferenceEquals(input, SuccessesInput) || ReferenceEquals(input, SampleSizeProportionInput),
        CalculationMode.MeanSampleSize =>
            ReferenceEquals(input, PlanningDeviationInput) || ReferenceEquals(input, MeanMarginInput),
        CalculationMode.ProportionSampleSize =>
            ReferenceEquals(input, ExpectedProportionInput) || ReferenceEquals(input, ProportionMarginInput),
        _ => false
    };

    private bool IsDecimalInput(TextBox input) =>
        ReferenceEquals(input, MeanInput) || ReferenceEquals(input, DeviationInput) ||
        ReferenceEquals(input, PlanningDeviationInput) || ReferenceEquals(input, MeanMarginInput) ||
        ReferenceEquals(input, ExpectedProportionInput) || ReferenceEquals(input, ProportionMarginInput);

    private TextBox GetFirstActiveInput() => Mode switch
    {
        CalculationMode.MeanInterval => MeanInput,
        CalculationMode.ProportionInterval => SuccessesInput,
        CalculationMode.MeanSampleSize => PlanningDeviationInput,
        CalculationMode.ProportionSampleSize => ExpectedProportionInput,
        _ => MeanInput
    };

    private void FocusFirstActiveInput() => GetFirstActiveInput().Focus();

    private void FormulaSelector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
            return;

        _isUpdatingControls = true;
        try
        {
            ClearNumericInputs();
            DeviationKindSelector.SelectedIndex = 0;
            UpdateInputVisibility();
        }
        finally
        {
            _isUpdatingControls = false;
        }

        _activeInput = null;
        _selectionStart = 0;
        _selectionEnd = 0;
        InvalidateResult();
        FocusFirstActiveInput();
    }

    private void Selection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingControls)
            InvalidateResult();
    }

    private void Input_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingControls)
            return;

        if (sender is TextBox input && ReferenceEquals(_activeInput, input))
            CaptureSelection(input);
        InvalidateResult();
    }

    private void CalculateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GetActiveInputForEditing();
        Calculate();
    }

    private void ClearButton_OnClick(object? sender, RoutedEventArgs e) => Clear();

    private void BackspaceButton_OnClick(object? sender, RoutedEventArgs e) => Backspace();

    private void ClearEntryButton_OnClick(object? sender, RoutedEventArgs e) => ClearActiveEntry();

    private void SignButton_OnClick(object? sender, RoutedEventArgs e) => ToggleSign();

    private void DecimalButton_OnClick(object? sender, RoutedEventArgs e) => InsertDecimalSeparator();

    private void UpdateInputVisibility()
    {
        MeanFieldsPanel.IsVisible = Mode == CalculationMode.MeanInterval;
        ProportionFieldsPanel.IsVisible = Mode == CalculationMode.ProportionInterval;
        MeanSampleSizeFieldsPanel.IsVisible = Mode == CalculationMode.MeanSampleSize;
        ProportionSampleSizeFieldsPanel.IsVisible = Mode == CalculationMode.ProportionSampleSize;
        UpdateKeypadCapabilities();
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
    private enum CalculationMode
    {
        MeanInterval,
        ProportionInterval,
        MeanSampleSize,
        ProportionSampleSize
    }


    private sealed record CalculationInput(
        bool IsMean,
        double Mean,
        double StandardDeviation,
        int SampleSize,
        int Successes,
        DeviationKind DeviationKind);
}
