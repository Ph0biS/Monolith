using System.Windows.Input;

namespace PROJECT;

public partial class AppShell : Shell
{
    public ICommand GoToCommand { get; }

    public AppShell()
    {
        InitializeComponent();

        // УДАЛИ регистрации MainPage, HistoryPage, AnalyticsPage, ForecastPage, AchievementsPage.
        // Они уже есть в XAML (ShellContent Route="...").

        // ОСТАВЬ только StatisticsPage, так как её нет в нижнем меню:
        Routing.RegisterRoute("StatisticsPage", typeof(Pages.StatisticsPage));
        VisualStateManager.SetVisualStateGroups(this, new VisualStateGroupList());
        GoToCommand = new Command<string>(async (route) =>
        {
            try
            {
                // Для вкладок лучше использовать префикс /// или //
                // Если route прилетает как "MainPage", превращаем его в "///MainPage"
                string finalRoute = route.StartsWith("//") ? route : $"///{route}";
                await Shell.Current.GoToAsync(finalRoute);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        });

        BindingContext = this;
    }
    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        // Принудительно уведомляем систему, что состояние вкладок изменилось
        // Это лечит баг, когда две вкладки подсвечены одновременно
        OnPropertyChanged("CurrentItem");
    }
}