using PROJECT.Models;
using PROJECT.Services;

namespace PROJECT.Pages;

public partial class DebtTrackerPage : ContentPage
{
    private string _selectedDirection = "tome";
    private List<Debt> _debts = new();

    public DebtTrackerPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.TranslationY = 300;
        this.Opacity = 0;
        await Task.WhenAll(
            this.TranslateTo(0, 0, 350, Easing.CubicOut),
            this.FadeTo(1, 300, Easing.CubicOut)
        );
        await LoadDebts();
        
    }
    private bool _glowRunning = false;
    private async Task RunLogoGlowAsync()
    {
        _glowRunning = true;
        while (_glowRunning)
        {
            await BtnToMeBorder.ScaleTo(1.02, 800, Easing.SinInOut);
            await BtnToMeBorder.ScaleTo(1.0, 800, Easing.SinInOut);
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _glowRunning = false;
    }
    private async Task LoadDebts()
    {
        _debts = await App.Database.GetDebtsAsync();
        DebtsCollection.ItemsSource = _debts;

        var active = _debts.Where(d => !d.IsClosed).ToList();
        double toMe = active.Where(d => d.Direction == "tome").Sum(d => d.Amount);
        double fromMe = active.Where(d => d.Direction == "fromme").Sum(d => d.Amount);

        TotalToMeLabel.Text = $"{toMe:N0} ₽";
        TotalFromMeLabel.Text = $"{fromMe:N0} ₽";

        if (!active.Any())
            AtlasDebtLabel.Text = "Система мониторинга активна. Долгов не обнаружено — отличный финансовый показатель!";
        else if (fromMe > toMe)
            AtlasDebtLabel.Text = $"⚠️ Вы должны больше чем должны вам. Дефицит: {fromMe - toMe:N0} ₽. Рекомендую закрыть долги в первую очередь.";
        else if (toMe > fromMe)
            AtlasDebtLabel.Text = $"✅ Баланс положительный: вам должны на {toMe - fromMe:N0} ₽ больше. Напомните должникам о возврате.";
        else
            AtlasDebtLabel.Text = $"Долговой баланс нейтральный. Активных долгов: {active.Count}.";
    }

    private async void OnToMeTapped(object sender, TappedEventArgs e)
    {
        _selectedDirection = "tome";

        // Анимация нажатия
        await BtnToMeBorder.ScaleTo(0.95, 80, Easing.CubicIn);
        await BtnToMeBorder.ScaleTo(1.0, 80, Easing.CubicOut);

        // Активируем МНЕ ДОЛЖНЫ
        BtnToMeBorder.BackgroundColor = Color.FromArgb("#1A3D2A");
        BtnToMeBorder.StrokeThickness = 2.5;
        BtnToMeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#2DD4BF"));
        LblToMe.TextColor = Colors.White;

        // Сбрасываем Я ДОЛЖЕН
        BtnFromMeBorder.BackgroundColor = Colors.Transparent;
        BtnFromMeBorder.StrokeThickness = 1;
        BtnFromMeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#D946EF"));
        LblFromMe.TextColor = Color.FromArgb("#D946EF");
    }

    private async void OnFromMeTapped(object sender, TappedEventArgs e)
    {
        _selectedDirection = "fromme";

        // Анимация нажатия
        await BtnFromMeBorder.ScaleTo(0.95, 80, Easing.CubicIn);
        await BtnFromMeBorder.ScaleTo(1.0, 80, Easing.CubicOut);

        // Активируем Я ДОЛЖЕН
        BtnFromMeBorder.BackgroundColor = Color.FromArgb("#3D1A2A");
        BtnFromMeBorder.StrokeThickness = 2.5;
        BtnFromMeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#FF00FF"));
        LblFromMe.TextColor = Colors.White;

        // Сбрасываем МНЕ ДОЛЖНЫ
        BtnToMeBorder.BackgroundColor = Colors.Transparent;
        BtnToMeBorder.StrokeThickness = 1;
        BtnToMeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#2DD4BF"));
        LblToMe.TextColor = Color.FromArgb("#2DD4BF");
    }

    private async void OnAddDebtClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PersonEntry.Text) ||
            string.IsNullOrWhiteSpace(AmountEntry.Text))
        {
            await DisplayAlert("Ошибка", "Заполните имя и сумму", "OK");
            return;
        }

        if (!double.TryParse(AmountEntry.Text, out double amount))
        {
            await DisplayAlert("Ошибка", "Некорректная сумма", "OK");
            return;
        }

        var debt = new Debt
        {
            PersonName = PersonEntry.Text.Trim(),
            Amount = amount,
            Direction = _selectedDirection,
            Description = DescEntry.Text?.Trim() ?? "",
            Date = DateTime.Now
        };

        await App.Database.SaveDebtAsync(debt);

        PersonEntry.Text = "";
        AmountEntry.Text = "";
        DescEntry.Text = "";

        await LoadDebts();
    }

    private async void OnCloseDebtClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Debt debt)
        {
            bool confirm = await DisplayAlert(
                "Закрыть долг?",
                $"{debt.PersonName} — {debt.AmountText}\nЭто создаст транзакцию в истории.",
                "Да", "Нет");

            if (!confirm) return;

            // Закрываем долг в БД
            await App.Database.CloseDebtAsync(debt);

            // Создаём транзакцию
            var transaction = new PROJECT.Models.Transaction
            {
                Amount = (decimal)debt.Amount,
                IsIncome = debt.Direction == "tome", // мне вернули = доход, я вернул = расход
                Category = debt.Direction == "tome" ? "💰 Возврат долга" : "💸 Выплата долга",
                Description = $"Долг: {debt.PersonName}",
                Date = DateTime.Now
            };

            await App.Database.SaveTransactionAsync(transaction);

            // Обновляем глобальную историю
            MainThread.BeginInvokeOnMainThread(() =>
            {
                App.GlobalHistory.Add(transaction);
            });

            await LoadDebts();

            await DisplayAlert("✅ Готово",
                debt.Direction == "tome"
                    ? $"+{debt.AmountText} добавлено к балансу"
                    : $"-{debt.AmountText} списано с баланса",
                "OK");
        }
    }

    private async void OnDeleteDebtClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Debt debt)
        {
            bool confirm = await DisplayAlert("Удалить?", $"Удалить долг {debt.PersonName}?", "Да", "Нет");
            if (confirm)
            {
                await App.Database.DeleteDebtAsync(debt);
                await LoadDebts();
            }
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}