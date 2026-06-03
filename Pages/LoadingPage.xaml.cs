using System;
using System.Threading.Tasks;

namespace PROJECT;

public partial class LoadingPage : ContentPage
{
    private readonly TaskCompletionSource<AppShell> _shellReady = new();
    private readonly TaskCompletionSource _dataReady = new();
    private bool _sideAnimRunning = false;

    private static readonly string[] HexChars =
    {
        "00","01","FF","A3","B7","C2","D9","E4","F1","08",
        "1C","2E","3A","4B","5D","6F","70","82","94","A6"
    };

    private readonly Label[] _leftLabels = new Label[20];
    private readonly Label[] _rightLabels = new Label[20];

    public LoadingPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = RunTickerAsync();
        // Собираем ссылки на боковые лейблы
        _leftLabels[0] = LeftCol1; _leftLabels[1] = LeftCol2; _leftLabels[2] = LeftCol3;
        _leftLabels[3] = LeftCol4; _leftLabels[4] = LeftCol5; _leftLabels[5] = LeftCol6;
        _leftLabels[6] = LeftCol7; _leftLabels[7] = LeftCol8; _leftLabels[8] = LeftCol9;
        _leftLabels[9] = LeftCol10; _leftLabels[10] = LeftCol11; _leftLabels[11] = LeftCol12;
        _leftLabels[12] = LeftCol13; _leftLabels[13] = LeftCol14; _leftLabels[14] = LeftCol15;
        _leftLabels[15] = LeftCol16; _leftLabels[16] = LeftCol17; _leftLabels[17] = LeftCol18;
        _leftLabels[18] = LeftCol19; _leftLabels[19] = LeftCol20;

        _rightLabels[0] = RightCol1; _rightLabels[1] = RightCol2; _rightLabels[2] = RightCol3;
        _rightLabels[3] = RightCol4; _rightLabels[4] = RightCol5; _rightLabels[5] = RightCol6;
        _rightLabels[6] = RightCol7; _rightLabels[7] = RightCol8; _rightLabels[8] = RightCol9;
        _rightLabels[9] = RightCol10; _rightLabels[10] = RightCol11; _rightLabels[11] = RightCol12;
        _rightLabels[12] = RightCol13; _rightLabels[13] = RightCol14; _rightLabels[14] = RightCol15;
        _rightLabels[15] = RightCol16; _rightLabels[16] = RightCol17; _rightLabels[17] = RightCol18;
        _rightLabels[18] = RightCol19; _rightLabels[19] = RightCol20;

        _ = Task.Run(async () =>
        {
            try
            {
       
                int created = await App.Database.ProcessScheduledTransactionsAsync();
                if (created > 0)
                    System.Diagnostics.Debug.WriteLine($"[SCHEDULED] Создано транзакций: {created}");

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB ERROR: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => _dataReady.TrySetResult());
            }
        });

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var shell = new AppShell();
            _shellReady.TrySetResult(shell);
        });

        _sideAnimRunning = true;
        _ = RunSideColumnsAsync();
        _ = RunBootAndSwitchAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _sideAnimRunning = false;
    }
    private async Task RunTickerAsync()
    {
        double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        double textWidth = 800; // примерная ширина текста
        TickerLabel.TranslationX = screenWidth;

        while (_sideAnimRunning)
        {
            await TickerLabel.TranslateTo(-textWidth, 0, 8000, Easing.Linear);
            TickerLabel.TranslationX = screenWidth;
        }
    }
    private async Task RunSideColumnsAsync()
    {
        var rng = new Random();
        int offsetLeft = 0;
        int offsetRight = 10; // смещение чтобы колонки не синхронизировались

        while (_sideAnimRunning)
        {
            for (int i = 0; i < 20; i++)
            {
                int leftIdx = (i + offsetLeft) % 20;
                int rightIdx = (i + offsetRight) % 20;

                // Обновляем текст
                _leftLabels[i].Text = HexChars[(leftIdx + rng.Next(3)) % HexChars.Length];
                _rightLabels[i].Text = HexChars[(rightIdx + rng.Next(3)) % HexChars.Length];

                // Подсвечиваем "активную" строку
                _leftLabels[i].TextColor = i == (offsetLeft % 20)
                    ? Color.FromArgb("#00F2FF")
                    : Color.FromArgb("#2A1B4E");

                _rightLabels[i].TextColor = i == (offsetRight % 20)
                    ? Color.FromArgb("#D946EF")
                    : Color.FromArgb("#2A1B4E");
            }

            offsetLeft = (offsetLeft + 1) % 20;
            offsetRight = (offsetRight + 1) % 20;

            await Task.Delay(120);
        }
    }

    private async Task RunBootAndSwitchAsync()
    {
        try
        {
            int created = await App.Database.ProcessScheduledTransactionsAsync();
            if (created > 0)
                System.Diagnostics.Debug.WriteLine($"[SCHEDULED] Создано транзакций: {created}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SCHEDULED ERROR]: {ex.Message}");
        }
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

        var shell = await _shellReady.Task;
        await Task.WhenAny(_dataReady.Task, Task.Delay(3000));

        _sideAnimRunning = false;
        _ = Task.Run(async () =>
        {
            try
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB ERROR: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => _dataReady.TrySetResult());
            }
        });
        // Сначала плавно гасим весь экран
        await this.FadeTo(0, 700, Easing.CubicIn);
        await Task.Delay(32);
        // Только после полного затухания переключаем страницу
        App.IsFirstLaunch = false;
        Application.Current!.MainPage = shell;
    }
}