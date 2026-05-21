using System.Collections.ObjectModel;
using PROJECT.Models;
using PROJECT.Services;

namespace PROJECT;

public partial class App : Application
{
    public static bool IsFirstLaunch { get; set; } = true;
    private static DatabaseService _database;
    public static ObservableCollection<Transaction> GlobalHistory { get; set; } = new ObservableCollection<Transaction>();

    public static DatabaseService Database
    {
        get
        {
            if (_database == null) _database = new DatabaseService();
            return _database;
        }
    }

    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
    private async void OnTabTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers[0] is TapGestureRecognizer tap)
        {
            string targetPage = tap.CommandParameter as string;
            if (!string.IsNullOrEmpty(targetPage))
            {
                // Переходим по маршруту, который указан в AppShell
                await Shell.Current.GoToAsync($"//{targetPage}");
            }
        }
    }
    // Добавляем этот метод для управления окном
  protected override Window CreateWindow(IActivationState? activationState)
{
    var window = base.CreateWindow(activationState);

    // Базовые размеры для кроссплатформы
    window.Width = 900;
    window.Height = 1000;
    window.MinimumWidth = 950;
    window.MinimumHeight = 700;

#if MACCATALYST
    Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("CustomWindowSize", (handler, view) =>
    {
        var mauiWindow = handler.VirtualView;
        if (mauiWindow == window)
        {
            var nativeWindow = handler.PlatformView;
            
            // Задаем жесткие рамки для сцены macOS
            nativeWindow.WindowScene.SizeRestrictions.MinimumSize = new CoreGraphics.CGSize(900, 700);
            nativeWindow.WindowScene.SizeRestrictions.MaximumSize = new CoreGraphics.CGSize(1100, 800);
            
            // Напрямую меняем фрейм окна без использования SetFrame
            var frame = nativeWindow.Frame;
            nativeWindow.Frame = new CoreGraphics.CGRect(frame.X, frame.Y, 1100, 800);
        }
    });
#endif

    return window;
}
}