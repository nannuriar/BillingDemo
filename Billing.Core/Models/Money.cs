namespace Billing.Core.Models;

internal static class Money
{
    public static bool IsWholeCents(decimal amount) => amount == decimal.Round(amount, 2);

    public static void ValidateBalance(decimal amount, string parameterName)
    {
        if (amount < 0 || amount > 999_999_999.99m || !IsWholeCents(amount))
        {
            throw new ArgumentOutOfRangeException(parameterName,
                "A balance must be between 0 and 999,999,999.99 USD.");
        }
    }
}
