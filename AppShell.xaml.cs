using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PROJECT;

public partial class AppShell : Shell, INotifyPropertyChanged
{
    public ICommand GoToCommand { get; }

    private string _currentTime = DateTime.Now.ToString("HH:mm");
    public string CurrentTime
    {
        get => _currentTime;
        private set
        {
            if (_currentTime != value)
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }
    }

    private readonly System.Timers.Timer _clockTimer;

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("StatisticsPage", typeof(Pages.StatisticsPage));
        VisualStateManager.SetVisualStateGroups(this, new VisualStateGroupList());

        GoToCommand = new Command<string>(async (route) =>
        {
            try
            {
                string finalRoute = route.StartsWith("//") ? route : $"///{route}";
                await Shell.Current.GoToAsync(finalRoute);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        });

        // Таймер реального времени
        _clockTimer = new System.Timers.Timer(1000);
        _clockTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm");
            });
        };
        _clockTimer.Start();

        BindingContext = this;
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        OnPropertyChanged("CurrentItem");
    }

    // INotifyPropertyChanged
    public new event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}