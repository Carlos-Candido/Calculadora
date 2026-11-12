using System.Globalization;

namespace Calculadora.Core;

public static class NumericInput
{
    public static bool TryParseDecimal(string? text, out double value)
    {
        value = 0;
        if (text is null)
            return false;

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
            return false;

        var commaCount = 0;
        var dotCount = 0;
        var digitCount = 0;
        for (var index = 0; index < trimmed.Length; index++)
        {
            var character = trimmed[index];
            if (character is >= '0' and <= '9')
                digitCount++;
            else if (character == ',')
                commaCount++;
            else if (character == '.')
                dotCount++;
            else if ((character is '+' or '-') && index == 0)
                continue;
            else
                return false;
        }

        if (digitCount == 0 || commaCount > 1 || dotCount > 1 || (commaCount > 0 && dotCount > 0))
            return false;

        var normalized = commaCount == 1 ? trimmed.Replace(',', '.') : trimmed;
        return double.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }

    public static bool TryParseInteger(string? text, out int value)
    {
        value = 0;
        if (text is null)
            return false;
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
            return false;
        foreach (var character in trimmed)
            if (character is < '0' or > '9')
                return false;
        return int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
