using PROJECT;
using Microcharts;
using SkiaSharp;
using System.Text;
using System.Windows.Input;
namespace PROJECT.Pages;

public partial class StatisticsPage : ContentPage
{
    public ICommand GoToWalletCommand => new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
    public ICommand GoToHistoryCommand => new Command(async () => await Shell.Current.GoToAsync("//HistoryPage"));
    public ICommand GoToAnalysisCommand => new Command(async () => await Shell.Current.GoToAsync("//AnalysisPage"));
    public ICommand GoToForecastCommand => new Command(async () => await Shell.Current.GoToAsync("//ForecastPage"));
    public ICommand GoToAchievementsCommand => new Command(async () => await Shell.Current.GoToAsync("//AchievementsPage"));
    public StatisticsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStatistics();
    }

    private async Task LoadStatistics()
    {
        // 1. Получаем данные из базы
        var allTransactions = await App.Database.GetTransactionsAsync();

        if (allTransactions == null || !allTransactions.Any())
        {
            ShowEmptyState();
            return;
        }

        // 2. Фильтруем только расходы
        var expenses = allTransactions.Where(t => !t.IsIncome && t.Amount > 0).ToList();

        if (!expenses.Any())
        {
            ShowEmptyState();
            return;
        }

        // 3. Подготавливаем данные ОДИН раз
        // Группируем, чтобы не делать это дважды
        var groupedData = expenses.GroupBy(t => t.Category)
            .Select(g => new
            {
                Name = g.Key,
                Amount = (float)g.Sum(x => x.Amount),
                Color = GetColorForCategory(g.Key) // Получаем родной Microsoft.Maui.Graphics.Color
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // 4. Создаем записи для ДИАГРАММЫ (используем SKColor)
        var chartEntries = groupedData.Select(d => new ChartEntry(d.Amount)
        {
            Label = d.Name,
            ValueLabel = $"{d.Amount:N0} ₽",
            Color = SKColor.Parse(d.Color.ToHex()), // Конвертация для библиотеки графиков
            TextColor = SKColors.White,
            ValueLabelColor = SKColors.White
        }).ToArray();

        // 5. Создаем данные для ЛЕГЕНДЫ (имена свойств ДОЛЖНЫ совпадать с Binding в XAML)
        var legendItems = chartEntries.Select(e => new
        {
            Label = e.Label,        // Для Binding Label
            ValueLabel = e.ValueLabel, // Для Binding ValueLabel
            Color = Color.FromRgba(e.Color.Red, e.Color.Green, e.Color.Blue, e.Color.Alpha) // Для Binding Color
        }).ToList();

        // 6. Привязка
        StatisticsDonutChart.Chart = new DonutChart
        {
            Entries = chartEntries,
            HoleRadius = 0.7f,
            BackgroundColor = SKColors.Transparent,
            LabelMode = LabelMode.None,
            GraphPosition = GraphPosition.Center
        };
        // 7. Обновляем текстовые метки (Лидер и общее кол-во)
        if (chartEntries.Any())
        {
            // Находим категорию с максимальной суммой
            var topEntry = chartEntries.OrderByDescending(e => e.Value).First();

            TopCategoryLabel.Text = $"🏆 Лидер трат: {topEntry.Label} ({topEntry.Value:N0} ₽)";

            // Считаем общее количество операций (всех, включая доходы)
            TotalTransactionsLabel.Text = $"Всего операций в базе: {allTransactions.Count}";
        }
        else
        {
            TopCategoryLabel.Text = "🏆 Лидер: нет данных";
            TotalTransactionsLabel.Text = "Операций: 0";
        }
        // Привязываем именно список с MAUI-цветами
        StatisticsLegend.ItemsSource = legendItems;
    }

    // Тот самый метод-синхронизатор цветов (скопирован с MainPage)
    private Color GetColorForCategory(string cat) => cat switch
    {
        "🛒 Продукты" => Color.FromArgb("#2DD4BF"),
        "🚌 Транспорт" => Color.FromArgb("#3B82F6"),
        "🎮 Развлечения" => Color.FromArgb("#8B5CF6"),
        "🏠 Дом" => Color.FromArgb("#FB923C"),
        "💊 Здоровье" => Color.FromArgb("#EF4444"),
        "🍔 Еда" => Color.FromArgb("#FFD700"),
        "👕 Одежда" => Color.FromArgb("#EC4899"),
        "📱 Связь" => Color.FromArgb("#06B6D4"),
        _ => Color.FromArgb("#94A3B8")
    };
    public class LegendItem
    {
        public string Category { get; set; }
        public string AmountText { get; set; }
        public string ColorHex { get; set; } // HEX-код для индикатора
    }
    private void ShowEmptyState()
    {
        StatisticsDonutChart.Chart = null;
        StatisticsLegend.ItemsSource = null;
        TopCategoryLabel.Text = "Нет данных для анализа";
        TotalTransactionsLabel.Text = "Добавьте расходы в кошельке";
    }

    // Переход К статистике (вызывается из MainPage)
    private async void OnChartTapped(object sender, EventArgs e)
    {
        // Относительный путь, так как страница зарегистрирована в RegisterRoute
        await Shell.Current.GoToAsync("StatisticsPage", animate: false);
    }

    // Возврат ИЗ статистики (вызывается в StatisticsPage по тапу на график)
    private async void OnChartTappedBack(object sender, EventArgs e)
    {
        // Безопасный возврат назад
        await Shell.Current.GoToAsync("..");
    }

    private async void OnShareReportClicked(object sender, EventArgs e)
    {
        try
        {
            var transactions = await App.Database.GetTransactionsAsync();
            var expenses = transactions.Where(t => !t.IsIncome).ToList();

            if (!expenses.Any())
            {
                await DisplayAlert("Внимание", "Нет данных по расходам для отчета", "OK");
                return;
            }

            // Формируем красивый текст отчета
            var sb = new StringBuilder();
            sb.AppendLine("📊 ФИНАНСОВЫЙ ОТЧЕТ");
            sb.AppendLine($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("---------------------------");

            var grouped = expenses.GroupBy(x => x.Category)
                                  .Select(g => new { Name = g.Key, Sum = g.Sum(s => s.Amount) })
                                  .OrderByDescending(x => x.Sum);

            foreach (var item in grouped)
            {
                sb.AppendLine($"{item.Name}: {item.Sum:N0} ₽");
            }

            sb.AppendLine("---------------------------");
            sb.AppendLine($"ИТОГО ТРАТ: {expenses.Sum(x => x.Amount):N0} ₽");

            // Сохраняем во временный файл
            string fn = "FinanceReport.txt";
            string file = Path.Combine(FileSystem.CacheDirectory, fn);
            File.WriteAllText(file, sb.ToString());

            // Открываем меню "Поделиться"
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Мой финансовый отчет",
                File = new ShareFile(file)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", "Не удалось создать отчет", "OK");
        }
    }
}