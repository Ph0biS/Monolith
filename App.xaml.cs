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
    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

#if WINDOWS
        
        const int width = 900; 
        const int height = 1000;

        window.Width = width;
        window.Height = height;

        
        window.MinimumWidth = width;
        window.MaximumWidth = width;
        window.MinimumHeight = height;
        window.MaximumHeight = height;

        
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        window.X = (displayInfo.Width / displayInfo.Density - width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - height) / 2;
#endif

        return window;
    }
}