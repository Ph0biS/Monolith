using System.Text.Json;

namespace PROJECT.Services;

public class CurrencyService
{
    // Используем бесплатное API без регистрации для примера
    private const string ApiUrl = "https://api.exchangerate-api.com/v4/latest/RUB";

    public async Task<Dictionary<string, double>> GetExchangeRatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync(ApiUrl);
            var data = JsonDocument.Parse(response);

            // Вытаскиваем курсы валют
            var rates = data.RootElement.GetProperty("rates");
            return new Dictionary<string, double>
            {
                { "USD", 1 / rates.GetProperty("USD").GetDouble() }, // Пересчет из RUB
                { "EUR", 1 / rates.GetProperty("EUR").GetDouble() }
            };
        }
        catch
        {
            // Если нет интернета, возвращаем примерные курсы
            return new Dictionary<string, double> { { "USD", 92.0 }, { "EUR", 100.0 } };
        }
    }
}