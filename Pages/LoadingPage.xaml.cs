using System;
using System.Threading.Tasks;

namespace PROJECT;

public partial class LoadingPage : ContentPage
{
    private readonly TaskCompletionSource<AppShell> _shellReady = new();
    private readonly TaskCompletionSource _dataReady = new();

    public LoadingPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Параллельная задача 1: загрузка данных из БД
        _ = Task.Run(async () =>
        {
            var transactions = await App.Database.GetTransactionsAsync();
            var goals = await App.Database.GetGoalsAsync();
            var subs = await App.Database.GetSubscriptionsAsync();

            App.PreloadedTransactions = transactions;
            App.PreloadedGoals = goals;
            App.PreloadedSubscriptions = subs;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                App.GlobalHistory.Clear();
                foreach (var t in transactions)
                    App.GlobalHistory.Add(t);

                _dataReady.TrySetResult();
            });
        });

        // Параллельная задача 2: создание AppShell на UI-потоке
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var shell = new AppShell();
            _shellReady.TrySetResult(shell);
        });

        // Запуск анимации (не блокирует)
        _ = RunBootAndSwitchAsync();
    }

    private async Task RunBootAndSwitchAsync()
    {
        await SplashLogo.FadeTo(1.0, 800, Easing.BounceOut);
        LogoGlow.Radius = 25;
        LogoGlow.Opacity = 0.8f;

        string[] logs =
        {
        "> INITIALIZING DATABASE...",
        "> LOADING ASSETS...",
        "> SECURING CONNECTION...",
        "> LOADING USER SETTINGS...",
        "> SYSTEM READY."
    };

        double maxWidth = 250.0;
        TerminalText.Text = "";

        for (int i = 0; i < logs.Length; i++)
        {
            TerminalText.Text += (i == 0 ? "" : Environment.NewLine) + logs[i];
            double targetWidth = maxWidth * ((i + 1.0) / logs.Length);
            BootProgressBar.AbortAnimation("progress");
            BootProgressBar.Animate("progress",
                d => BootProgressBar.WidthRequest = d,
                BootProgressBar.WidthRequest,
                targetWidth, 16, 400, Easing.CubicOut);
            await Task.Delay(600);
        }

        await Task.Delay(300);

        // Ждём shell и данные
        var shell = await _shellReady.Task;
        await _dataReady.Task;

        // Ключевой мом: переключаем MainPage ДО анимации затухания
        // LoadingPage продолжает быть видна поверх — она ещё не погасла
        App.IsFirstLaunch = false;
        Application.Current!.MainPage = shell;

        // Даём Windows один кадр чтобы отрендерить shell позади
        await Task.Delay(32);

        // Теперь плавно гасим загрузчик — под ним уже готовый shell с данными
        await this.FadeTo(0, 600, Easing.CubicIn);
    }
}