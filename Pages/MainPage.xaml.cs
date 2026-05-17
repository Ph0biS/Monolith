using PROJECT;
using Microcharts;
using PROJECT.Models;
using PROJECT.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
namespace PROJECT.Pages;

public partial class MainPage : ContentPage
{
    private double _usdRate = 92.50; // Значение по умолчанию
    private double _eurRate = 101.20; // Значение по умолчанию
    private string _selectedCurrency = "RUB";
    #region --- 1. ПЕРЕМЕННЫЕ И КОЛЛЕКЦИИ ---
    private Dictionary<string, double> _rates = new() { { "USD", 92.0 }, { "EUR", 100.0 } };
    private readonly CurrencyService _currencyService = new();
    private bool _isReady = false;
    private bool isIncomeMode = false;
    private bool _isGlowRunning = false;
    private string _currentCurrency = "RUB";
    private IEnumerable<ChartEntry> entries;
    private bool _isAnimating = false;
    private bool _isPageVisible;
    // Те самые коллекции, которые теперь 100% привязаны к UI

    public ObservableCollection<Transaction> TransactionHistory { get; set; } = new();
    public ObservableCollection<SavingsGoal> UserSavings { get; set; } = new();
    public ObservableCollection<Subscription> Subscriptions { get; set; } = new();

    private readonly List<string> expenseCategories = new()
    {
        "🛒 Продукты", "🚌 Транспорт", "🎮 Развлечения",
        "🏠 Дом", "💊 Здоровье", "🍔 Еда", "👕 Одежда", "📱 Связь"
    };

    private readonly List<string> incomeCategories = new()
    {
        "💰 Зарплата", "🎁 Подарок", "📈 Инвестиции", "💰 Подработка"
    };

    #endregion

    #region --- 2. КОНСТРУКТОР И ЖИЗНЕННЫЙ ЦИКЛ ---
    public ICommand GoToWalletCommand => new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
    public ICommand GoToHistoryCommand => new Command(async () => await Shell.Current.GoToAsync("//HistoryPage"));
    public ICommand GoToAnalysisCommand => new Command(async () => await Shell.Current.GoToAsync("//AnalysisPage"));
    public ICommand GoToForecastCommand => new Command(async () => await Shell.Current.GoToAsync("//ForecastPage"));
    public ICommand GoToAchievementsCommand => new Command(async () => await Shell.Current.GoToAsync("//AchievementsPage"));
    private static bool _isFirstLoad = true;
    public MainPage()
    {
        InitializeComponent();
        _isReady = true;
    
        BindingContext = this;

        CategoryPicker.ItemsSource = expenseCategories;
        CurrencyPicker.SelectedIndex = 0;

        // Явная привязка источников данных для стабильности
        SavingsCollection.ItemsSource = UserSavings;
        SubsCollectionView.ItemsSource = Subscriptions;
    }

    private bool _isPulseRunning = false; // Флаг, чтобы не запускать анимацию дважды

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageVisible = true;
        isIncomeMode = true;
        CategoryPicker.ItemsSource = incomeCategories;

        if (App.IsFirstLaunch)
        {
            LoadingOverlay.IsVisible = true;
            LoadingOverlay.Opacity = 1;

            // Запускаем твой метод
            await RunCyberpunkBootloader();

            // Помечаем, что больше показывать не нужно
            App.IsFirstLaunch = false;
        }
        else
        {
            // Если не первый запуск, просто убеждаемся, что оверлей скрыт
            LoadingOverlay.IsVisible = false;
        }
        // ОБЯЗАТЕЛЬНО: Даем UI-потоку «продышаться» после перехода
        await Task.Delay(20);

        // Визуальные состояния (кнопки)
        if (BtnIncomeBorder != null && BtnExpenseBorder != null)
        {
            // Сначала сбрасываем всё в Normal
            VisualStateManager.GoToState(BtnExpenseBorder, "Normal");
            VisualStateManager.GoToState(BtnIncomeBorder, "Normal");

            // А теперь выбираем Доход
            VisualStateManager.GoToState(BtnIncomeBorder, "Selected");

            // 4. Ручное управление цветом (на случай, если VSM капризничает)
            LblIncome.TextColor = Color.FromArgb("#140B2D"); // Темный (контраст)
            LblExpense.TextColor = Color.FromArgb("#D946EF"); // Розовый неон
        }

        // Загружаем данные из БД (асинхронно)
        await LoadDataFromDatabase();

        // Загружаем курсы, но НЕ ЖДЕМ их (чтобы не висеть, если интернет тупит)
        _ = Task.Run(async () => {
            try { await LoadCurrencyRates(); }
            catch { /* Логгируем ошибку в консоль */ }
        });

        // Запускаем заставку только если это первый вход
        if (_isFirstLoad)
        {
            LoadingOverlay.IsVisible = true;
            await RunCyberpunkBootloader();
            _isFirstLoad = false;
        }

        // Обновляем интерфейс и графики

        if (!_isPulseRunning)
        {
            _isPulseRunning = true;
            //_ = PulseBalanceGlow();
        }
    }
    private async Task RunCyberpunkBootloader()
    {
        // Блокируем навигацию, пока идет загрузка (чтобы не уйти в момент анимации)
        _isAnimating = true;

        try
        {
            await SplashLogo.FadeTo(1.0, 800, Easing.BounceOut);

            LogoGlow.Radius = 25;
            LogoGlow.Opacity = 0.8f;

            string[] logs = {
            "> INITIALIZING DATABASE...",
            "> LOADING ASSETS...",
            "> SECURING CONNECTION...",
            "> SYSTEM READY."
        };

            double maxWidth = 250.0;
            TerminalText.Text = ""; // Очищаем перед стартом

            for (int i = 0; i < logs.Length; i++)
            {
                // Проверка: если страница закрывается, выходим из цикла немедленно
                if (!_isPageVisible) return;

                TerminalText.Text += (i == 0 ? "" : Environment.NewLine) + logs[i];

                double targetWidth = maxWidth * ((i + 1.0) / logs.Length);

                // Останавливаем предыдущую итерацию анимации перед запуском новой
                ProgressBar.AbortAnimation("progress");

                ProgressBar.Animate("progress",
                    d => ProgressBar.WidthRequest = d,
                    ProgressBar.WidthRequest,
                    targetWidth,
                    32, // Увеличили шаг (реже обновление - легче процессору)
                    400,
                    Easing.CubicOut);

                await Task.Delay(600);
            }

            await Task.Delay(300);
            await LoadingOverlay.FadeTo(0, 400);
            LoadingOverlay.IsVisible = false;

        }
        finally
        {
            _isAnimating = false; // Разблокируем всё
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isPageVisible = false;
        _isAnimating = false;
        _isPulseRunning = false;

        // Убиваем график при уходе. Когда вернешься через //, 
        // OnAppearing создаст его заново — это чище для процессора.
        
    }
    #endregion

    #region --- 3. РАБОТА С БАЗОЙ ДАННЫХ (ОБНОВЛЕНИЕ СПИСКОВ) ---

    private async Task LoadDataFromDatabase()
    {
        try
        {
            var transactions = await App.Database.GetTransactionsAsync();
            var goals = await App.Database.GetGoalsAsync();
            var subs = await App.Database.GetSubscriptionsAsync();

            // Работаем с ObservableCollection (они автоматически уведомляют UI)
            TransactionHistory.Clear();
            foreach (var t in transactions) TransactionHistory.Add(t);

            UserSavings.Clear();
            foreach (var g in goals) UserSavings.Add(g);

            Subscriptions.Clear();
            foreach (var s in subs) Subscriptions.Add(s);

            // Обновляем графики и баланс
            await UpdateDashboardUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DB ERROR]: {ex.Message}");
        }
    }
    // Этот метод вызывается каждый раз, когда ты меняешь период в Пикере
    private void OnPeriodChanged(object sender, EventArgs e)
    {
        // Проверяем, что метод загрузки данных существует и вызываем его
        // Он должен учитывать выбранный период при фильтрации
        _ = LoadDataFromDatabase();
    }
    #endregion

    #region --- 4. ЛОГИКА ТРАНЗАКЦИЙ ---

    private async void OnAddTransactionClicked(object sender, EventArgs e)
    {
        if (decimal.TryParse(AmountEntry.Text, out decimal amt))
        {
            var newTransaction = new Transaction
            {
                Description = string.IsNullOrWhiteSpace(DescriptionEntry.Text) ? "Без описания" : DescriptionEntry.Text,
                Amount = amt,
                IsIncome = isIncomeMode,
                Category = CategoryPicker.SelectedItem?.ToString() ?? "Прочее",
                Date = DateTime.Now
            };

            await App.Database.SaveTransactionAsync(newTransaction);
            // После сохранения транзакции
            var history = await App.Database.GetTransactionsAsync();
            var achievements = AchievementService.CalculateAchievements(history);

            // Проверяем, есть ли новые открытые ачивки, которых не было раньше
            // (Это простая версия, можно усложнить хранением в БД)
            if (newTransaction.Amount >= 50000 && !achievements.First(a => a.Name == "Saver").IsUnlocked)
            {
                await DisplayAlert("ACHIEVEMENT UNLOCKED!", "Вы получили ачивку: Сберегатель 💰", "КРУТО!");
            }
            AmountEntry.Text = string.Empty;
            DescriptionEntry.Text = string.Empty;
            CategoryPicker.SelectedIndex = -1;
            await CheckNewAchievements();
            await LoadDataFromDatabase(); // Мгновенное обновление
        }
    }
    private async Task CheckNewAchievements()
    {
        var history = await App.Database.GetTransactionsAsync();
        var currentBalance = history.Sum(t => t.IsIncome ? t.Amount : -t.Amount);

        // Проверяем конкретные условия
        // 1. Первая кровь (если это единственная запись)
        if (history.Count == 1)
        {
            await DisplayAlert("🏆 ДОСТИЖЕНИЕ!", "First Blood: Вы начали свой путь к богатству!", "КРУТО!");
        }

        // 2. Сберегатель (если эта транзакция перешагнула порог 50к)
        if (currentBalance >= 50000)
        {
            // Чтобы не спамить алертом каждый раз, можно добавить проверку 
            // через Preferences (сохранилась ли она раньше)
            bool alreadyGot = Preferences.Get("Achievement_Saver", false);
            if (!alreadyGot)
            {
                await DisplayAlert("💎 ЛЕГЕНДАРНО!", "Сберегатель: Баланс превысил 50 000 ₽!", "ЕСТЬ!");
                Preferences.Set("Achievement_Saver", true);
            }
        }

        // 3. Maniac (5 записей за день)
        var todayCount = history.Count(t => t.Date.Date == DateTime.Today);
        if (todayCount == 5)
        {
            await DisplayAlert("⚔️ MANIAC!", "5 операций за день! Вот это активность!", "УРА!");
        }
    }
    private void OnExpenseClicked(object s, EventArgs e)
    {
        isIncomeMode = false;
        if (BtnExpenseBorder == null || BtnIncomeBorder == null) return;

        VisualStateManager.GoToState(BtnExpenseBorder, "Selected");
        VisualStateManager.GoToState(BtnIncomeBorder, "Normal");

        // Меняем цвета текста вручную
        LblExpense.TextColor = Colors.White;
        LblIncome.TextColor = Color.FromArgb("#2DD4BF");

        CategoryPicker.ItemsSource = expenseCategories;
    }

    private void OnIncomeClicked(object s, EventArgs e)
    {
        isIncomeMode = true;
        if (BtnIncomeBorder == null || BtnExpenseBorder == null) return;

        VisualStateManager.GoToState(BtnIncomeBorder, "Selected");
        VisualStateManager.GoToState(BtnExpenseBorder, "Normal");

        // Меняем цвета текста вручную
        LblIncome.TextColor = Color.FromArgb("#140B2D"); // Темный для контраста на зеленом
        LblExpense.TextColor = Color.FromArgb("#D946EF");

        CategoryPicker.ItemsSource = incomeCategories;
    }

    #endregion

    #region --- 5. ЛОГИКА ЦЕЛЕЙ И КОПИЛОК ---

    // ДОБАВЛЕНИЕ КОПИЛКИ
    private async void OnAddGoalClicked(object sender, EventArgs e)
    {
        // Убираем возможные пробелы и меняем запятую на точку для парсинга
        string name = GoalNameEntry.Text?.Trim();
        string targetText = GoalTargetEntry.Text?.Replace(',', '.');

        if (!string.IsNullOrWhiteSpace(name) && decimal.TryParse(targetText, out decimal target))
        {
            var newGoal = new SavingsGoal
            {
                Name = name,
                TargetAmount = target,
                CurrentAmount = 0,
                Icon = "💰"
            };

            await App.Database.SaveGoalAsync(newGoal);

            // Очищаем поля и убираем клавиатуру
            GoalNameEntry.Text = GoalTargetEntry.Text = string.Empty;
            GoalTargetEntry.Unfocus();

            await LoadDataFromDatabase(); // Перерисовывает список на экране
        }
        else
        {
            await DisplayAlert("Ошибка", "Введите название и сумму цели", "OK");
        }
    }

    // ДОБАВЛЕНИЕ ПОДПИСКИ
    private async void OnAddSubscriptionClicked(object sender, EventArgs e)
    {
        string name = SubNameEntry.Text?.Trim();
        string priceText = SubPriceEntry.Text?.Replace(',', '.');

        if (!string.IsNullOrWhiteSpace(name) && decimal.TryParse(priceText, out decimal price))
        {
            var newSub = new Subscription
            {
                Name = name,
                Price = price,
                NextPaymentDate = SubDatePicker.Date,
                PaymentDay = SubDatePicker.Date.Day
            };

            await App.Database.SaveSubscriptionAsync(newSub);

            SubNameEntry.Text = SubPriceEntry.Text = string.Empty;
            SubPriceEntry.Unfocus();

            await LoadDataFromDatabase();
        }
    }

    private async void OnDepositGoalClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is SavingsGoal goal)
        {
            string result = await DisplayPromptAsync("Пополнение",
                $"Сколько добавим в '{goal.Name}'?", "Добавить", "Отмена", keyboard: Keyboard.Numeric);

            if (decimal.TryParse(result, out decimal amount) && amount > 0)
            {
                // 1. Обновляем саму цель (копилку)
                goal.CurrentAmount += amount;
                await App.Database.SaveGoalAsync(goal);

                // 2. СОЗДАЕМ ТРАНЗАКЦИЮ РАСХОДА (чтобы деньги ушли с баланса и попали в график)
                var goalTx = new Transaction
                {
                    Amount = amount,
                    IsIncome = false, // ВАЖНО: это расход
                    Category = "🎯 Цели",
                    Description = $"В копилку: {goal.Name}",
                    Date = DateTime.Now
                };

                await App.Database.SaveTransactionAsync(goalTx);

                // 3. Обновляем всё
                await LoadDataFromDatabase();

                // Теперь UpdateDashboardUI внутри LoadDataFromDatabase увидит новую транзакцию,
                // вычтет её из баланса и добавит в сектор диаграммы.
            }
        }
    }

    private async void OnDeleteGoalClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is SavingsGoal goal)
        {
            if (await DisplayAlert("Удаление", $"Удалить цель '{goal.Name}'?", "Да", "Нет"))
            {
                await App.Database.DeleteGoalAsync(goal);
                await LoadDataFromDatabase();
            }
        }
    }

    #endregion

    #region --- 6. ЛОГИКА ПОДПИСОК ---
    private async void OnDeleteSubscriptionClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is Subscription sub)
        {
            await App.Database.DeleteSubscriptionAsync(sub);
            await LoadDataFromDatabase();
        }
    }

    #endregion

    #region --- 7. ОБНОВЛЕНИЕ UI И ГРАФИКИ ---

    private async Task UpdateDashboardUI()
    {
        if (ChartV == null || BalanceLabel == null || TransactionHistory == null) return;

        // 1. Считаем всё в фоне
        var (convBal, symbol, expenseStats, hasAnyExpensesInHistory) = await Task.Run(() =>
        {
            decimal r = 1.0m;
            string s = "₽";
            if (_currentCurrency == "USD") { r = (decimal)_usdRate; s = "$"; }
            else if (_currentCurrency == "EUR") { r = (decimal)_eurRate; s = "€"; }

            // ПРОВЕРКА 1: Проверяем наличие РАСХОДОВ вообще во всей истории базы (игнорируя выбранный период/месяц)
            bool globalExpensesExist = TransactionHistory.Any(t => !t.IsIncome);

            // Расчет общего баланса
            decimal rawBal = TransactionHistory.Sum(t => t.IsIncome ? t.Amount : -t.Amount);
            decimal finalBal = rawBal / r;

            // Группируем расходы (локально для выбранного на UI периода)
            var stats = TransactionHistory
                .Where(t => !t.IsIncome)
                .GroupBy(t => t.Category)
                .Select(g => new ExpenseCategoryItem
                {
                    Category = g.Key,
                    Sum = (float)(g.Sum(t => t.Amount) / r),
                    AmountText = (g.Sum(t => t.Amount) / r).ToString("N0") + " " + s,
                    DisplayColor = GetColorForCategory(g.Key)
                })
                .OrderByDescending(x => x.Sum)
                .ToList();

            return (finalBal, s, stats, globalExpensesExist);
        });

        // 2. Обновляем UI в главном потоке
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BalanceLabel.Text = $"{convBal:N0} {symbol}";
            ChartLegendView.ItemsSource = expenseStats;

            // Если в истории приложения глобально есть расходы с прошлых разов
            if (hasAnyExpensesInHistory)
            {
                // ВКЛЮЧАЕМ всю область графика, ВЫКЛЮЧАЕМ плейсхолдер Атласа
                if (ChartVisualElements != null) ChartVisualElements.IsVisible = true;
                if (EmptyStatePlaceholder != null) EmptyStatePlaceholder.IsVisible = false;

                // Если есть конкретно расходы в выбранном периоде — отрисовываем кольцо диаграммы
                if (expenseStats != null && expenseStats.Any())
                {
                    var newEntries = expenseStats.Select(x => new ChartEntry(x.Sum)
                    {
                        Label = x.Category,
                        ValueLabel = x.AmountText,
                        Color = SKColor.Parse(x.DisplayColor.ToHex())
                    }).ToArray();

                    if (ChartV.Chart is DonutChart existingChart)
                    {
                        existingChart.Entries = newEntries;
                    }
                    else
                    {
                        ChartV.Chart = new DonutChart
                        {
                            Entries = newEntries,
                            HoleRadius = 0.7f,
                            BackgroundColor = SKColors.Transparent,
                            LabelMode = LabelMode.None,
                            Typeface = SKTypeface.FromFamilyName("Orbitron")
                        };
                    }
                }
                else
                {
                    // Если глобально расходы есть, но в выбранном пустом месяце их 0
                    ChartV.Chart = new DonutChart
                    {
                        Entries = new ChartEntry[] { new ChartEntry(1) { Color = SKColors.DarkGray, Label = "Нет данных" } },
                        HoleRadius = 0.7f,
                        BackgroundColor = SKColors.Transparent,
                        LabelMode = LabelMode.None
                    };
                }
            }
            else
            {
                // База абсолютно чистая (первый запуск) -> Показываем Атласа и скрываем область графика
                if (ChartVisualElements != null) ChartVisualElements.IsVisible = false;
                if (EmptyStatePlaceholder != null) EmptyStatePlaceholder.IsVisible = true;
                ChartV.Chart = null;
            }
        });
    }

    #region --- 8. СЕРВИСНЫЕ МЕТОДЫ ---

    private void StartBalanceGlow()
    {
        _isGlowRunning = true;
        BalanceLabel.FadeTo(0.6, 1500).ContinueWith(t => BalanceLabel.FadeTo(1.0, 1500));
    }

    private Color GetColorForCategory(string cat) => cat switch
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

    private void Switch_Toggled(object s, ToggledEventArgs e) => Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;

    private async void OnCurrencyChanged(object s, EventArgs e)
    {
        if (CurrencyPicker.SelectedItem == null) return;

        _currentCurrency = CurrencyPicker.SelectedItem.ToString();

        // ВАЖНО: вызываем обновление UI, чтобы пересчитать баланс и график 
        // с учетом новых курсов и выбранного символа
        await UpdateDashboardUI();
    }

    #endregion
    private async void OnExportDataClicked(object sender, EventArgs e)
    {
        // Даже если здесь пока будет пусто, ошибка исчезнет
        await DisplayAlert("Экспорт", "Функция экспорта в CSV будет добавлена позже", "OK");
    }
    private async void OnResetStatsClicked(object sender, EventArgs e)
    {
        // Спрашиваем подтверждение, чтобы случайно всё не удалить
        bool answer = await DisplayAlert("Очистка", "Вы уверены, что хотите удалить все записи о тратах и доходах?", "Да", "Нет");

        if (answer)
        {
            // Удаляем все транзакции из базы данных
            var allTransactions = await App.Database.GetTransactionsAsync();
            foreach (var t in allTransactions)
            {
                await App.Database.DeleteTransactionAsync(t);
            }

            // Обновляем списки и графики на экране
            await LoadDataFromDatabase();

            await DisplayAlert("Готово", "Статистика успешно сброшена", "OK");
        }
    }
    private async void OnMonthChanged(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        // Проверка _isReady важна, чтобы не срабатывать при инициализации страницы
        if (picker?.SelectedItem == null || !_isReady) return;

        try
        {
            string selected = picker.SelectedItem.ToString();

            // 1. Получаем данные в фоновом потоке, чтобы не вешать пикер
            var allTransactions = await Task.Run(async () => await App.Database.GetTransactionsAsync());

            List<Transaction> filtered;

            if (selected == "Все время")
            {
                filtered = allTransactions;
            }
            else
            {
                // Берем текущий год
                int currentYear = DateTime.Now.Year;
                // Индекс месяца (Январь = 1, если "Все время" на 0 позиции)
                int targetMonth = picker.SelectedIndex;

                filtered = allTransactions
                    .Where(t => t.Date.Month == targetMonth && t.Date.Year == currentYear)
                    .ToList();
            }

            // 2. Обновляем UI безопасно
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Отключаем на мгновение привязку, чтобы не спамить уведомлениями
                var tempHistory = filtered.ToList();

                TransactionHistory.Clear();
                foreach (var t in tempHistory)
                {
                    TransactionHistory.Add(t);
                }

                // После обновления коллекции вызываем обновление графиков ОДИН раз
                await UpdateDashboardUI();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка пикера: {ex.Message}");
        }
    }
    // ///////////////////////////////////////////////////////////
    // СТРАНИЦА: КОШЕЛЕК -> ПЕРЕХОД НА АНАЛИЗ
    // ///////////////////////////////////////////////////////////
    private async void OnChartTapped(object sender, EventArgs e)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        await ChartV.ScaleTo(0.95, 50);
        await ChartV.ScaleTo(1.0, 50);

        // УДАЛИЛИ ChartV.Chart = null;

        await Shell.Current.GoToAsync("StatisticsPage");
        _isAnimating = false;
    }

    private async void OnBackToChartClicked(object sender, EventArgs e)
    {
        // Очищаем график на текущей странице (если он там есть), чтобы облегчить память
        // Это критично для предотвращения фризов при переходе через //

        await Shell.Current.GoToAsync("..");
    }
    // ///////////////////////////////////////////////////////////
    // 
    // ///////////////////////////////////////////////////////////
    private void UpdateDonutChart(List<ExpenseCategoryItem> data)
    {
        if (data == null || !data.Any())
        {
            ChartV.Chart = null;
            return;
        }

        // Создаем записи для диаграммы на основе твоих данных из легенды
        var entries = data.Select(d => new ChartEntry(d.Sum)
        {
            Label = d.Category,
            ValueLabel = d.AmountText,
            Color = SKColor.Parse(d.DisplayColor.ToHex()),
            TextColor = SKColors.White, // Цвет текста подписей
        }).ToArray();

        // Настраиваем сам «бублик»
        ChartV.Chart = new DonutChart
        {
            Entries = entries,
            HoleRadius = 0.7f,              // Толщина кольца
            LabelTextSize = 35f,            // Размер текста подписей
            BackgroundColor = SKColors.Transparent,
            LabelMode = LabelMode.None,      // Скрываем лишние подписи, так как у нас есть своя легенда
            GraphPosition = GraphPosition.Center
        };
    }
    private async void OnShareReportClicked(object sender, EventArgs e)
    {
        try
        {
            // 1. Получаем все данные
            var transactions = await App.Database.GetTransactionsAsync();

            if (transactions == null || !transactions.Any())
            {
                await DisplayAlert("Ошибка", "Нет данных для экспорта", "OK");
                return;
            }

            // 2. Формируем CSV контент
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Дата,Категория,Описание,Сумма,Тип");

            foreach (var t in transactions)
            {
                string type = t.IsIncome ? "Доход" : "Расход";
                // Форматируем строку: Дата | Категория | Описание | Сумма | Тип
                csvBuilder.AppendLine($"{t.Date:dd.MM.yyyy},{t.Category},{t.Description},{t.Amount},{type}");
            }

            // 3. Сохранение файла
            byte[] fileBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            using var stream = new MemoryStream(fileBytes);

            // Используем встроенный в MAUI Share для мобильных устройств 
            // или FileSaver, если вы используете CommunityToolkit.
            // Самый универсальный способ для старта — сохранить во временную папку и «поделиться»
            string fileName = $"Отчет_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            string tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllBytesAsync(tempPath, fileBytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Экспорт данных",
                File = new ShareFile(tempPath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось сохранить файл: {ex.Message}", "OK");
        }
    }
    private async Task LoadCurrencyRates()
    {
        try
        {
            using (var client = new HttpClient())
            {
                // Используем простой API для получения курсов к рублю
                var response = await client.GetStringAsync("https://open.er-api.com/v6/latest/RUB");
                var data = JsonDocument.Parse(response);
                var rates = data.RootElement.GetProperty("rates");

                // API отдает сколько валюты в 1 рубле, поэтому делим 1 на это число
                _usdRate = 1 / rates.GetProperty("USD").GetDouble();
                _eurRate = 1 / rates.GetProperty("EUR").GetDouble();

                System.Diagnostics.Debug.WriteLine($"Курсы обновлены: USD {_usdRate:N2}, EUR {_eurRate:N2}");
            }
        }
        catch
        {
            // Если нет сети, оставляем дефолтные значения, чтобы приложение не упало
            _usdRate = 92.50;
            _eurRate = 101.20;
        }

    }
    private async void OnForecastTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ForecastPage");
    }

    private async void OnAchievementsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AchievementsPage");
    }

    private async void OnWalletTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//WalletPage");
    }

    private async void OnHistoryTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HistoryPage");
    }

    private async void OnStatisticsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//StatisticsPage");
    }
}
    #endregion