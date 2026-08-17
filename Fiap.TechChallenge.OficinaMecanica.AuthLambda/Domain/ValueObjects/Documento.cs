namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.ValueObjects;

public readonly record struct Documento
{
    public string Valor { get; }
    public string Tipo => IsCpf ? "CPF" : "CNPJ";
    public bool IsCpf => Valor.Length == 11;
    public bool IsCnpj => Valor.Length == 14;

    private Documento(string valor)
    {
        Valor = valor;
    }

    public static Documento Parse(string? valor)
    {
        var cleaned = OnlyDigits(valor);
        if (cleaned.Length == 11)
        {
            if (!ValidarCpfDigits(cleaned))
            {
                throw new ArgumentException("CPF invalido.");
            }

            return new Documento(cleaned);
        }

        if (cleaned.Length == 14)
        {
            if (!ValidarCnpjDigits(cleaned))
            {
                throw new ArgumentException("CNPJ invalido.");
            }

            return new Documento(cleaned);
        }

        throw new ArgumentException("Documento deve conter 11 (CPF) ou 14 (CNPJ) digitos.");
    }

    private static string OnlyDigits(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    private static bool ValidarCpfDigits(string digits)
    {
        if (digits.Length != 11 || digits.Distinct().Count() == 1)
        {
            return false;
        }

        var dv1 = CalcularDigito(digits[..9], new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 });
        var dv2 = CalcularDigito(digits[..10], new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 });
        return digits[9] == dv1 && digits[10] == dv2;
    }

    private static bool ValidarCnpjDigits(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        var dv1 = CalcularDigito(cnpj[..12], new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });
        var dv2 = CalcularDigito(cnpj[..13], new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });
        return cnpj[12] == dv1 && cnpj[13] == dv2;
    }

    private static char CalcularDigito(string baseDigits, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < baseDigits.Length; i++)
        {
            soma += (baseDigits[i] - '0') * pesos[i];
        }

        var resto = soma % 11;
        var digito = resto < 2 ? 0 : 11 - resto;
        return (char)('0' + digito);
    }

    public override string ToString()
    {
        return Valor;
    }
}
