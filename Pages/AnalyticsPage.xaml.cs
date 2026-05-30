using PROJECT;
using Microcharts;
using PROJECT.Models;
using PROJECT.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PROJECT.Pages;

public partial class AnalyticsPage : ContentPage
{
    public ICommand GoToWalletCommand => new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
    public ICommand GoToHistoryCommand => new Command(async () => await Shell.Current.GoToAsync("//HistoryPage"));
    public ICommand GoToAnalysisCommand => new Command(async () => await Shell.Current.GoToAsync("//AnalysisPage"));
    public ICommand GoToForecastCommand => new Command(async () => await Shell.Current.GoToAsync("//ForecastPage"));
    public ICommand GoToAchievementsCommand => new Command(async () => await Shell.Current.GoToAsync("//AchievementsPage"));
    public AnalyticsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAnalyticsData();
    }

    private async Task LoadAnalyticsData()
    {
        try
        {
            var allTransactions = await App.Database.GetTransactionsAsync();
            var expenses = allTransactions?.Where(t => !t.IsIncome).ToList() ?? new List<Transaction>();

            if (expenses.Count == 0)
            {
                NoDataLabel.IsVisible = true;
                AnalyticsList.IsVisible = false;
                ChartV.Chart = null; // Очищаем график, если данных нет
                return;
            }

            NoDataLabel.IsVisible = false;
            AnalyticsList.IsVisible = true;

            decimal totalSum = expenses.Sum(x => x.Amount);

            var report = expenses.GroupBy(t => t.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    AmountText = $"{g.Sum(x => x.Amount):N0} ₽",
                    Percentage = totalSum > 0 ? (double)(g.Sum(x => x.Amount) / totalSum) : 0,
                    PercentageText = totalSum > 0 ? $"{(g.Sum(x => x.Amount) / totalSum):P0}" : "0%",
                    Color = GetColor(g.Key)
                })
                .OrderByDescending(x => x.Percentage)
                .ToList();

            var chartEntries = report.Select(r => new ChartEntry((float)expenses.Where(e => e.Category == r.Category).Sum(s => s.Amount))
            {
                Label = r.Category,
                ValueLabel = r.AmountText,
                Color = SKColor.Parse(r.Color.ToHex()),
                TextColor = SKColors.White
            }).ToArray();

            // ИСПРАВЛЕНО: Теперь используем имя ChartV из XAML
            ChartV.Chart = new DonutChart
            {
                Entries = chartEntries,
                HoleRadius = 0.7f,
                LabelMode = LabelMode.None,
                GraphPosition = GraphPosition.Center,
                BackgroundColor = SKColors.Transparent
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AnalyticsList.ItemsSource = report;
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnBackToWalletTapped(object sender, EventArgs e)
    {
        try
        {
            // Эффект нажатия
            await AnalyticsChartContainer.ScaleTo(0.96, 100);
            await AnalyticsChartContainer.ScaleTo(1, 100);

            // Переход на вкладку Кошелек
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка навигации: {ex.Message}");
        }
    }

    private async void OnResetStatsClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Полный сброс", "Удалить все данные, включая копилки и подписки?", "Да", "Нет");
        if (confirm)
        {
            await App.Database.ClearEverythingAsync(); 
            await LoadAnalyticsData(); // Обновляем текущую страницу
            await Shell.Current.GoToAsync("//MainPage"); // Возвращаемся, чтобы обнулить баланс
        }
    }

    private Color GetColor(string category)
    {
        return category switch
        {
            "🛒 Продукты" => Color.FromArgb("#2DD4BF"), // Бирюзовый
            "🚌 Транспорт" => Color.FromArgb("#3B82F6"), // Синий
            "🎮 Развлечения" => Color.FromArgb("#8B5CF6"), // Фиолетовый
            "🏠 Дом" => Color.FromArgb("#FB923C"),       // Оранжевый
            "💊 Здоровье" => Color.FromArgb("#EF4444"),   // Красный
            "🍔 Еда" => Color.FromArgb("#FFD700"),        // Золотой
            "👕 Одежда" => Color.FromArgb("#EC4899"),     // Розовый
            "📱 Связь" => Color.FromArgb("#06B6D4"),      // Голубой
            "💰 Зарплата" => Color.FromArgb("#22C55E"),   // Зеленый
            "🎁 Подарок" => Color.FromArgb("#F59E0B"),    // Янтарный
            _ => Color.FromArgb("#94A3B8")                // Серый (для остальных)
        };
    }
}