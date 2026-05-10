using PROJECT;
using System.Windows.Input;

namespace PROJECT.Pages;

public partial class ForecastPage : ContentPage
{
    private bool _isInitialized = false; // Флаг готовности
    public ICommand GoToWalletCommand => new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
    public ICommand GoToHistoryCommand => new Command(async () => await Shell.Current.GoToAsync("//HistoryPage"));
    public ICommand GoToAnalysisCommand => new Command(async () => await Shell.Current.GoToAsync("//AnalysisPage"));
    public ICommand GoToForecastCommand => new Command(async () => await Shell.Current.GoToAsync("//ForecastPage"));
    public ICommand GoToAchievementsCommand => new Command(async () => await Shell.Current.GoToAsync("//AchievementsPage"));
    public ForecastPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInitialized = false; // Блокируем расчеты при загрузке

        double savedLimit = Preferences.Default.Get("MonthlyLimit", 0.0);
        LimitEntry.Text = savedLimit > 0 ? savedLimit.ToString() : string.Empty;

        _isInitialized = true; // Разрешаем расчеты
        await CalculateBudget();
    }

    private async void OnLimitTextChanged(object sender, TextChangedEventArgs e)
    {
        // Если страница еще грузится — выходим
        if (!_isInitialized) return;

        string val = string.IsNullOrEmpty(e.NewTextValue) ? "0" : e.NewTextValue;

        if (double.TryParse(val, out double newLimit))
        {
            Preferences.Default.Set("MonthlyLimit", newLimit);
            await CalculateBudget();
        }
    }

    private async void OnLimitChanged(object sender, EventArgs e)
    {
        await CalculateBudget();
    }

    private async Task CalculateBudget()
    {
        try
        {
            var transactions = await App.Database.GetTransactionsAsync();
            double currentMonthExpenses = 0;
            var now = DateTime.Now;

            if (transactions != null)
            {
                foreach (var t in transactions)
                {
                    // 1. Убираем проверку IsNullOrWhiteSpace, так как DateTime не может быть пустым.
                    // 2. Оставляем только проверку на доход, если ты считаешь только расходы.
                    if (t.IsIncome) continue;

                    // 3. Теперь нам не нужен TryParseExact, так как t.Date — это уже готовый объект DateTime.
                    // Мы можем просто сравнить месяц напрямую.
                    if (t.Date.Month == DateTime.Now.Month && t.Date.Year == DateTime.Now.Year)
                    {
                        currentMonthExpenses += (double)t.Amount;
                    }
                }
            }

            double limit = Preferences.Default.Get("MonthlyLimit", 0.0);

            // Обновляем UI строго через MainThread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateUI(currentMonthExpenses, limit);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private void UpdateUI(double spent, double limit)
    {
        SpentLabel.Text = $"Потрачено: {spent:N0} ₽";

        if (limit > 0)
        {
            double remaining = limit - spent;
            double progress = spent / limit;

            RemainingLabel.Text = remaining > 0 ? $"Осталось: {remaining:N0} ₽" : "Лимит исчерпан!";
            RemainingLabel.TextColor = remaining > 0 ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");

            BudgetProgressBar.ProgressTo(Math.Min(progress, 1.0), 500, Easing.Linear);

            BudgetProgressBar.ProgressColor = progress switch
            {
                < 0.7 => Color.FromArgb("#4CAF50"),
                < 0.9 => Color.FromArgb("#FFC107"),
                _ => Color.FromArgb("#F44336")
            };

            StatusMessageLabel.Text = progress > 1
                ? "Внимание! Превышение бюджета!"
                : $"Использовано {progress:P0} лимита";
        }
        else
        {
            BudgetProgressBar.Progress = 0;
            RemainingLabel.Text = "Осталось: 0 ₽";
            StatusMessageLabel.Text = "Установите лимит для расчета";
        }
        // Считаем сколько дней прошло с начала месяца (минимум 1, чтобы не делить на 0)
        int daysPassed = Math.Max(DateTime.Now.Day, 1);
        double dailyAverage = spent / daysPassed;

        DailyAverageLabel.Text = $"Средний расход в этом месяце: {dailyAverage:N0} ₽/день";

        if (limit > 0 && spent > 0)
        {
            double remaining = limit - spent;
            if (remaining > 0)
            {
                int daysCanLast = (int)(remaining / dailyAverage);
                DaysLeftLabel.Text = $"Денег хватит примерно на {daysCanLast} дн.";
                DaysLeftLabel.TextColor = daysCanLast < 5 ? Colors.OrangeRed : Colors.White;
            }
            else
            {
                DaysLeftLabel.Text = "Бюджет закончился!";
                DaysLeftLabel.TextColor = Colors.IndianRed;
            }
        }
    }
    private async void OnResetLimitClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Сброс данных",
            "Это удалит все расходы за месяц и обнулит лимит. Продолжить?", "Да", "Нет");

        if (confirm)
        {
            try
            {
                // 1. Очищаем транзакции в БД
                await App.Database.ClearAllTransactionsAsync();

                // 2. Сбрасываем лимит в памяти
                Preferences.Set("MonthlyLimit", 0.0);

                // 3. Обновляем интерфейс (если есть методы обновления)
                // LoadForecastData(); 

                await DisplayAlert("Успех", "Данные и лимит сброшены", "OK");

                // Возвращаемся на главную, чтобы баланс тоже обновился
                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }
}