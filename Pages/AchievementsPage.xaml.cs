using PROJECT;
using PROJECT.Models;
using PROJECT.Services;
using System.Windows.Input;

namespace PROJECT.Pages;

// Обязательно добавь : ContentPage, чтобы C# понимал, что это страница
public partial class AchievementsPage : ContentPage
{
    public ICommand GoToWalletCommand => new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
    public ICommand GoToHistoryCommand => new Command(async () => await Shell.Current.GoToAsync("//HistoryPage"));
    public ICommand GoToAnalysisCommand => new Command(async () => await Shell.Current.GoToAsync("//AnalysisPage"));
    public ICommand GoToForecastCommand => new Command(async () => await Shell.Current.GoToAsync("//ForecastPage"));
    public ICommand GoToAchievementsCommand => new Command(async () => await Shell.Current.GoToAsync("//AchievementsPage"));
    public AchievementsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    // Метод OnAppearing срабатывает каждый раз, когда ты открываешь эту вкладку
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var history = await App.Database.GetTransactionsAsync();
        var result = AchievementService.CalculateAchievements(history);
        int unlocked = result.Count(a => a.IsUnlocked);
        TotalStatsLabel.Text = $"Разблокировано: {unlocked} / {result.Count}";
        AchievementsCollectionView.ItemsSource = result;
        // ОТЛАДКА: Если ачивок 0, ты увидишь алерт при переходе на вкладку
        if (result == null || result.Count == 0)
        {
            await DisplayAlert("Debug", "Список ачивок пуст!", "OK");
        }

        AchievementsCollectionView.ItemsSource = result;
    }

    private async Task RefreshAchievements()
    {
        try
        {
            // 1. Получаем всю историю транзакций из твоей базы
            var history = await App.Database.GetTransactionsAsync();

            // 2. Пропускаем историю через сервис ачивок
            // (Убедись, что метод в сервисе называется CalculateAchievements)
            var achievements = AchievementService.CalculateAchievements(history);

            // 3. Привязываем результат к CollectionView
            // AchievementsCollectionView — это x:Name из твоего XAML
            AchievementsCollectionView.ItemsSource = achievements;
        }
        catch (Exception ex)
        {
            // На случай, если база еще пуста или произошел сбой
            await DisplayAlert("Ошибка", "Не удалось загрузить достижения", "OK");
        }
    }
}