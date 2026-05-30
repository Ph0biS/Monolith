using Microcharts;
using Microsoft.VisualBasic;
using PROJECT;
using PROJECT.Models;
using PROJECT.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
namespace PROJECT.Pages;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private double _usdRate = 92.50; // Значение по умолчанию
    private double _eurRate = 101.20; // Значение по умолчанию
    private string _selectedCurrency = "RUB";
    #region --- 1. ПЕРЕМЕННЫЕ И КОЛЛЕКЦИИ ---
    private Dictionary<string, double> _rates = new() { { "USD", 92.0 }, { "EUR", 100.0 } };
    private readonly CurrencyService _currencyService = new();
    private bool _isReady = false;
    private bool? isIncomeMode = null;
    private bool _isGlowRunning = false;
    private string _currentCurrency = "RUB";
    private IEnumerable<ChartEntry> entries;
    private bool _isAnimating = false;
    private bool _isPageVisible;
    private string _selectedType = "";
    private CancellationTokenSource _animationCts;
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

        this.BindingContext = this;
        UserSavings = new ObservableCollection<SavingsGoal>();
        // Даем Mac Catalyst непустой список при старте, чтобы инициализировать оверлей
        CategoryPicker.ItemsSource = new List<string> { "[ Сначала выберите Доход или Расход ]" };
        CategoryPicker.SelectedIndex = 0;
        CurrencyPicker.SelectedItem = "RUB";
        PeriodPicker.SelectedItem = "Все время";
        BindableLayout.SetItemsSource(SavingsCollection, UserSavings);
        Subscriptions = new ObservableCollection<Subscription>();
    }
    private bool _isPulseRunning = false; // Флаг, чтобы не запускать анимацию дважды
    public int TotalDataCount => Subscriptions.Count + TransactionHistory.Count + UserSavings.Count;

    // Свойства для XAML (их теперь проще поддерживать)
    public bool HasData => TotalDataCount > 0;
    public bool HasNoData => TotalDataCount == 0;
    public void RefreshUI()
    {
        // Отладочный вывод
        Debug.WriteLine($"[DEBUG] RefreshUI: TotalCount = {TotalDataCount}, HasData = {HasData}, HasNoData = {HasNoData}");

        OnPropertyChanged(nameof(TotalDataCount));
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(HasNoData));
    }
    private async Task RunPulseAnimation(View view, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // Плавное изменение прозрачности (эффект пульсации)
                await Task.WhenAll(
    view.FadeTo(0.7, 1000, Easing.CubicInOut),
    view.ScaleTo(0.9, 1000, Easing.CubicInOut) // Немного уменьшаем
);
                await Task.WhenAll(
                    view.FadeTo(1.0, 1000, Easing.CubicInOut),
                    view.ScaleTo(1.0, 1000, Easing.CubicInOut) // Возвращаем в исходный размер
                );

                // Можно добавить небольшую паузу, если нужно
                // await Task.Delay(500, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Это ожидаемое исключение при отмене токена
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка анимации: {ex.Message}");
        }
    }
    private void Switch_Toggled(object sender, ToggledEventArgs e)
    {
        // 1. Получаем Grid из шаблона (если он назван)
        // Либо просто меняем общий ресурс, который использует шаблон

        // Если шаблон использует StaticResource/DynamicResource:
        Application.Current.Resources["ActiveBackground"] = e.Value ?
            Application.Current.Resources["DarkGradient"] :
            Application.Current.Resources["LightGradient"];
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageVisible = true;
        _animationCts = new CancellationTokenSource();
        _ = RunPulseAnimation(BalanceLabel, _animationCts.Token);
        // 1. Сразу сбрасываем всё в Нейтральное состояние
        SetNeutralState();
        NoteEditor.Text = await App.Database.GetNoteAsync();
        if (App.PreloadedTransactions != null)
        {
            var transactions = App.PreloadedTransactions;
            var goals = App.PreloadedGoals ?? new List<SavingsGoal>();
            var subs = App.PreloadedSubscriptions ?? new List<Subscription>();

            // Сбрасываем кэш сразу чтобы следующий OnAppearing пошёл в БД
            App.PreloadedTransactions = null;
            App.PreloadedGoals = null;
            App.PreloadedSubscriptions = null;

            // Заполняем коллекции напрямую без запроса к БД
            TransactionHistory.Clear();
            foreach (var t in transactions) TransactionHistory.Add(t);

            UserSavings.Clear();
            foreach (var g in goals) UserSavings.Add(g);

            Subscriptions.Clear();
            foreach (var s in subs) Subscriptions.Add(s);

            SubsCollectionView.ItemsSource = null;
            SubsCollectionView.ItemsSource = Subscriptions;

            // Обновляем UI без повторного похода в БД
            await UpdateDashboardUI();

            if (AtlasLoadingView != null)
                AtlasLoadingView.IsVisible = true;
        }
        else
        {
            // Обычный путь — возврат на страницу после навигации
            await LoadDataFromDatabase();

            if (AtlasLoadingView != null)
                AtlasLoadingView.IsVisible = true;
        }

        // 5. Валюты (фоном)
        _ = Task.Run(async () => {
            try { await LoadCurrencyRates(); }
            catch { /* Игнорируем */ }
        });

        
    }
    private async void OnNoteUnfocused(object sender, FocusEventArgs e)
    {
        await App.Database.SaveNoteAsync(NoteEditor.Text ?? "");
        NoteSavedLabel.IsVisible = true;
        await Task.Delay(2000);
        NoteSavedLabel.IsVisible = false;
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isPageVisible = false;
        _isAnimating = false;
        _isPulseRunning = false;
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        // Убиваем график при уходе. 
        // OnAppearing создаст его заново

    }
    #endregion

    #region --- 3. РАБОТА С БАЗОЙ ДАННЫХ (ОБНОВЛЕНИЕ СПИСКОВ) ---
   
    private async Task LoadDataFromDatabase()
    {
        try
        {
            // 1. Получаем данные
            var transactions = await App.Database.GetTransactionsAsync();
            var goals = await App.Database.GetGoalsAsync();
            var subs = await App.Database.GetSubscriptionsAsync();

            // 2. Обновление UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                
                // --- ОБНОВЛЕНИЕ КОЛЛЕКЦИЙ ---
                TransactionHistory.Clear();
                foreach (var t in transactions) TransactionHistory.Add(t);

                UserSavings.Clear();
                foreach (var g in goals) UserSavings.Add(g);

                Subscriptions.Clear();
                foreach (var s in subs) Subscriptions.Add(s);

                // Принудительное обновление для CollectionView
                SubsCollectionView.ItemsSource = null;
                SubsCollectionView.ItemsSource = Subscriptions;
                
                // --- ЛОГИКА ПОДПИСОК ---
                if (AtlasSubscriptionsPlaceholder != null)
                {
                    // Показываем блок подписок всегда, либо скрывай по условию (как нужно тебе)
                    AtlasSubscriptionsPlaceholder.IsVisible = true;

                    if (!Subscriptions.Any())
                    {
                        AtlasSubscriptionsTitleLabel.Text = "SYSTEM_MONITOR // ПОДПИСОК НЕ НАЙДЕНЫ";
                        AtlasSubscriptionsSuggestionLabel.Text = "Атлас готов отслеживать ваши регулярные списания. Добавьте первую подписку, чтобы активировать трекер.";
                    }
                    else
                    {
                        AtlasSubscriptionsTitleLabel.Text = "ATLAS_AI // МОНИТОРИНГ РЕГУЛЯРНЫХ СПИСАНИЙ";
                        decimal totalSubsCost = Subscriptions.Sum(s => s.Price);

                        if (totalSubsCost < 500)
                            AtlasSubscriptionsSuggestionLabel.Text = $"Общая нагрузка: {totalSubsCost:N0} ₽/мес. Подписки под контролем.";
                        else if (totalSubsCost <= 1500)
                            AtlasSubscriptionsSuggestionLabel.Text = $"Общая нагрузка: {totalSubsCost:N0} ₽/мес. Рекомендуется ревизия.";
                        else
                            AtlasSubscriptionsSuggestionLabel.Text = $"Внимание: {totalSubsCost:N0} ₽/мес. Высокая нагрузка!";
                    }
                }

                // --- ЛОГИКА ЦЕЛЕЙ ---
                if (AtlasGoalsAnalyticsBox != null)
                {
                    if (!UserSavings.Any())
                    {
                        AtlasGoalsSuggestionLabel.Text = "Протокол накоплений пуст. Поставьте цель, и я помогу рассчитать план.";
                        AtlasGoalsAnalyticsBox.IsVisible = true;
                    }
                    else
                    {
                        var mainGoal = UserSavings.First();
                        double percent = (mainGoal.TargetAmount > 0) ? (double)(mainGoal.CurrentAmount / mainGoal.TargetAmount) * 100 : 0;

                        if (percent == 0)
                            AtlasGoalsSuggestionLabel.Text = $"Цель '{mainGoal.Name}' запущена. Время начать накопления!";
                        else if (percent < 50)
                            AtlasGoalsSuggestionLabel.Text = $"Прогресс '{mainGoal.Name}': {percent:N1}%. Отличный старт!";
                        else
                            AtlasGoalsSuggestionLabel.Text = $"Экватор пройден! '{mainGoal.Name}' готова на {percent:N1}%. Финал близок.";

                        AtlasGoalsAnalyticsBox.IsVisible = true;
                    }
                }
                bool hasTransactions = TransactionHistory.Any();
                // Для hasStats можно просто передать true/false, если логика внутри UpdateDashboardUI
                // Но лучше вычислять тут, чтобы знать точно.
                bool hasStats = TransactionHistory.Any(t => !t.IsIncome);

                // 4. ФИНАЛЬНЫЙ ВЫЗОВ (в самом конце!)
                UpdateLayoutVisibility(hasTransactions, hasStats);
            });

            // 3. Обновляем графики (если метод вызывается отдельно)
            await UpdateDashboardUI();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DB ERROR]: {ex.Message}");
        }
    }

    private void OnPeriodChanged(object sender, EventArgs e)
    {
        _ = LoadDataFromDatabase();
    }
    #endregion

    #region --- 4. ЛОГИКА ТРАНЗАКЦИЙ ---

    private async void OnAddTransactionClicked(object sender, EventArgs e)
    {
        if (sender is View v)
        {
            await v.ScaleTo(0.96, 80, Easing.CubicIn);
            await v.ScaleTo(1.0, 80, Easing.CubicOut);
        }


        // 1. КРИТИЧЕСКАЯ ПРОВЕРКА: выбран ли режим и категория
        // Так как при старте ничего не горит, ItemsSource у пикера равен null.
        // ПРОВЕРКА: Если выбрана заглушка или ничего не выбрано — стопим процесс
        if (CategoryPicker.SelectedItem == null ||
            CategoryPicker.SelectedItem.ToString().StartsWith("[") ||
            CategoryPicker.ItemsSource == null)
        {
            await DisplayAlert("Monolith OS", "Пожалуйста, выберите режим (Доход или Расход) перед добавлением записи.", "OK");
            return;
        }

        // 2. Проверка корректности введенной суммы
        if (decimal.TryParse(AmountEntry.Text, out decimal amt))
        {
            // Создаем транзакцию с выбранной категорией
            var newTransaction = new Transaction
            {
                Description = string.IsNullOrWhiteSpace(DescriptionEntry.Text) ? "Без описания" : DescriptionEntry.Text,
                Amount = amt,
                IsIncome = (bool)isIncomeMode,
                Category = CategoryPicker.SelectedItem.ToString(),
                Date = DateTime.Now
            };

            // Сохраняем в локальную БД
            await App.Database.SaveTransactionAsync(newTransaction);
            await App.Database.UpdateStreakAsync();
            if (Shell.Current is AppShell appShell)
                await appShell.RefreshStreakAsync();
            // Получаем историю для расчета достижений
            var history = await App.Database.GetTransactionsAsync();
            var achievements = AchievementService.CalculateAchievements(history);

            // Проверка конкретной ачивки "Saver"
            var saverAchievement = achievements.FirstOrDefault(a => a.Name == "Saver");
            if (newTransaction.Amount >= 50000 && saverAchievement != null && !saverAchievement.IsUnlocked)
            {
                await DisplayAlert("ACHIEVEMENT UNLOCKED!", "Вы получили ачивку: Сберегатель 💰", "КРУТО!");
            }

            // 3. Очистка полей ввода
            AmountEntry.Text = string.Empty;
            SetNeutralState();
            DescriptionEntry.Text = string.Empty;

            // Сбрасываем выбранный элемент пикера
            CategoryPicker.ItemsSource = new List<string> { "[ Сначала выберите Доход или Расход ]" };
            CategoryPicker.SelectedIndex = 0;

            // Тушим кнопки обратно в нейтральное состояние
            BtnExpenseBorder.BackgroundColor = Color.FromArgb("#140B2D");
            BtnExpenseBorder.Stroke = Color.FromArgb("#3D1D4A");
            LblExpense.TextColor = Color.FromArgb("#7A2D6A");

            BtnIncomeBorder.BackgroundColor = Color.FromArgb("#140B2D");
            BtnIncomeBorder.Stroke = Color.FromArgb("#1F3A3A");
            LblIncome.TextColor = Color.FromArgb("#1E6B60");
            RefreshUI();
            // Дополнительный фикс для десктопа: если нужно полностью сбросить пикер в дефолт
#if MACCATALYST
        CategoryPicker.ItemsSource = null; // Полностью очищаем до следующего выбора Дохода/Расхода
#endif

            // Обновляем ачивки и интерфейс графиков
            await CheckNewAchievements();
            await LoadDataFromDatabase(); // Мгновенное обновление всей аналитики и баланса
        }
        else
        {
            // Если пользователь ввел буквы или оставил поле суммы пустым
            await DisplayAlert("Monolith OS", "Пожалуйста, введите корректную сумму операции.", "OK");
        }
    }
    private bool _glowAnimRunning = false;

    private async void OnAddBtnHoverEnter(object sender, PointerEventArgs e)
    {
        _glowAnimRunning = true;
        _ = RunGlowAnimation();
    }

    private async void OnAddBtnHoverExit(object sender, PointerEventArgs e)
    {
        _glowAnimRunning = false;
        GlowWrapper.Stroke = new SolidColorBrush(Colors.Transparent);
        //GlowWrapper.StrokeThickness = 0;
    }

    private async Task RunGlowAnimation()
    {
        while (_glowAnimRunning)
        {
            GlowWrapper.Stroke = new LinearGradientBrush(
                new GradientStopCollection {
                new GradientStop(Colors.Transparent, 0.0f),
                new GradientStop(Color.FromArgb("#00F2FF"), 0.5f),
                new GradientStop(Colors.Transparent, 1.0f)
                }, new Point(0, 0), new Point(1, 0));
            await Task.Delay(500);
            if (!_glowAnimRunning) break;

            GlowWrapper.Stroke = new LinearGradientBrush(
                new GradientStopCollection {
                new GradientStop(Colors.Transparent, 0.0f),
                new GradientStop(Color.FromArgb("#D946EF"), 0.5f),
                new GradientStop(Colors.Transparent, 1.0f)
                }, new Point(0, 0), new Point(0, 1));
            await Task.Delay(500);
            if (!_glowAnimRunning) break;

            GlowWrapper.Stroke = new LinearGradientBrush(
                new GradientStopCollection {
                new GradientStop(Colors.Transparent, 0.0f),
                new GradientStop(Color.FromArgb("#00F2FF"), 0.5f),
                new GradientStop(Colors.Transparent, 1.0f)
                }, new Point(1, 0), new Point(0, 0));
            await Task.Delay(500);
            if (!_glowAnimRunning) break;

            GlowWrapper.Stroke = new LinearGradientBrush(
                new GradientStopCollection {
                new GradientStop(Colors.Transparent, 0.0f),
                new GradientStop(Color.FromArgb("#D946EF"), 0.5f),
                new GradientStop(Colors.Transparent, 1.0f)
                }, new Point(0, 1), new Point(0, 0));
            await Task.Delay(500);
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
    private Brush GetNeonGradient(bool isIncome, bool isActive)
    {
        if (!isActive)
        {
            // Пассивное состояние: оставляем темный фон
            return new SolidColorBrush(Color.FromArgb("#140B2D"));
        }

        // Активное состояние: создаем градиент
        if (isIncome) // Доход (Бирюзово-Мятный)
        {
            return new LinearGradientBrush(
                new GradientStopCollection {
                new GradientStop(Color.FromArgb("#2DD4BF"), 0.0f), // Яркий бирюзовый
                new GradientStop(Color.FromArgb("#06B6D4"), 1.0f)  // Глубокий циановый
                },
                new Point(0, 0), new Point(1, 0));
        }
        else // Расход (Розово-Фиолетовый)
        {
            return new LinearGradientBrush(
                new GradientStopCollection {
                new GradientStop(Color.FromArgb("#D946EF"), 0.0f), // Розовый
                new GradientStop(Color.FromArgb("#8B5CF6"), 1.0f)  // Фиолетовый
                },
                new Point(0, 0), new Point(1, 0));
        }
    }
    private void UpdateButtonStyles(bool? isIncomeSelected)
    {
        // Одинаковый для всех "неактивный" стиль
        var inactiveBackground = new SolidColorBrush(Color.FromArgb("#140B2D"));
        var inactiveStroke = Color.FromArgb("#3D1D4A"); // Или свой цвет для границ
        var inactiveTextColor = Color.FromArgb("#7A2D6A");

        if (isIncomeSelected == null)
        {
            // СБРОС: Обе кнопки выглядят как "выключенные"
            BtnExpenseBorder.Background = inactiveBackground;
            BtnExpenseBorder.Stroke = inactiveStroke;
            LblExpense.TextColor = inactiveTextColor;

            BtnIncomeBorder.Background = inactiveBackground;
            BtnIncomeBorder.Stroke = inactiveStroke;
            LblIncome.TextColor = inactiveTextColor;
        }
        else if (isIncomeSelected == false)
        {
            // ВЫБРАН РАСХОД: Расход яркий, Доход тусклый
            BtnExpenseBorder.Background = GetNeonGradient(false, true); // Активный
            BtnExpenseBorder.Stroke = Colors.Transparent;
            LblExpense.TextColor = Colors.White;

            BtnIncomeBorder.Background = inactiveBackground; // Неактивный
            BtnIncomeBorder.Stroke = Color.FromArgb("#1F3A3A");
            LblIncome.TextColor = Color.FromArgb("#1E6B60");
        }
        else if (isIncomeSelected == true)
        {
            // ВЫБРАН ДОХОД: Доход яркий, Расход тусклый
            BtnExpenseBorder.Background = inactiveBackground; // Неактивный
            BtnExpenseBorder.Stroke = Color.FromArgb("#3D1D4A");
            LblExpense.TextColor = Color.FromArgb("#7A2D6A");

            BtnIncomeBorder.Background = GetNeonGradient(true, true); // Активный
            BtnIncomeBorder.Stroke = Colors.Transparent;
            LblIncome.TextColor = Colors.White;
        }
    }
    
    private void SetTransactionMode(bool isIncome)
    {
        isIncomeMode = isIncome;

        // 1. Обновляем визуальный стиль кнопок (подсветку)
        UpdateButtonStyles(isIncome);

        // 2. Выбираем нужный список
        // Важно: в списках incomeCategories и expenseCategories НЕ должно быть заглушек!
        var list = isIncome ? incomeCategories : expenseCategories;

        // 3. Обновляем Picker
        CategoryPicker.ItemsSource = list;
        CategoryPicker.IsEnabled = true; // Разблокируем пикер

        // 4. Устанавливаем выбор
        if (list != null && list.Count > 0)
        {
            CategoryPicker.SelectedIndex = 0; // Выбираем первую категорию
           //CategoryPicker.Title = "Выберите категорию"; // Подсказка при выборе
        }
    }

    private void SetNeutralState()
    {
        isIncomeMode = null;
        // 1. "Выключаем" визуальное выделение
        UpdateButtonStyles(null);

        // 2. Блокируем пикер
        CategoryPicker.IsEnabled = false;

        // Это хак для Mac Catalyst:
        // CategoryPicker.ItemsSource = new List<string> { "[ Сначала выберите Доход или Расход ]" };
        CategoryPicker.SelectedIndex = 0;
    }
    
    #endregion

    #region --- 5. ЛОГИКА ЦЕЛЕЙ И КОПИЛОК ---

    // ДОБАВЛЕНИЕ КОПИЛКИ
    private async void OnAddGoalClicked(object sender, EventArgs e)
    {
        if (sender is Button b2) await AnimateButton(b2);
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
        if (sender is Button btn)
        {
            await btn.ScaleTo(0.9, 80, Easing.CubicIn);
            await btn.ScaleTo(1.0, 80, Easing.CubicOut);
        }
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
    private decimal GetCurrentBalance()
    {
        // Парсим текст из BalanceLabel
        string balanceText = BalanceLabel.Text.Replace(" ₽", "").Trim();

        if (decimal.TryParse(balanceText, out decimal balance))
            return balance;

        return 0;
    }
    private async void OnDepositGoalClicked(object sender, EventArgs e)
    {
        if (sender is Button b3) await AnimateButton(b3);
        if ((sender as Button)?.CommandParameter is not SavingsGoal goal)
            return;

        string result = await DisplayPromptAsync(
            "Пополнение",
            $"Сколько добавим в '{goal.Name}'?",
            "Добавить", "Отмена",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(result))
            return;

        if (!decimal.TryParse(result, out decimal amount) || amount <= 0)
        {
            await DisplayAlert("Ошибка", "Введите корректную сумму", "OK");
            return;
        }

        try
        {
            // ВАЖНО: проверяем баланс перед пополнением
            var currentBalance = GetCurrentBalance();
            if (currentBalance < amount)
            {
                await DisplayAlert("Ошибка", "Недостаточно средств на балансе", "OK");
                return;
            }

            // 1. Обновляем саму цель (копилку)
            goal.CurrentAmount += amount;
            await App.Database.SaveGoalAsync(goal);

            // 2. СОЗДАЕМ ТРАНЗАКЦИЮ РАСХОДА (деньги уходят с баланса в копилку)
            var goalTransaction = new Transaction
            {
                Amount = amount,
                IsIncome = false,
                Category = "🎯 Цели",
                Description = $"В копилку: {goal.Name}",
                Date = DateTime.Now,
                GoalId = goal.Id  // Связываем с целью
            };

            await App.Database.SaveTransactionAsync(goalTransaction);

            // 3. Обновляем коллекцию целей
            int index = UserSavings.IndexOf(goal);
            if (index >= 0)
            {
                UserSavings[index] = goal;  // Обновляем элемент в коллекции
            }

            // 4. Перезагружаем все данные и обновляем UI
            await LoadDataFromDatabase();

            await DisplayAlert("Успех", $"Добавлено {amount} ₽ в '{goal.Name}'", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось пополнить цель: {ex.Message}", "OK");
        }
    }
    private async Task AnimateButton(Button btn)
    {
        await btn.ScaleTo(0.93, 80, Easing.CubicIn);
        await btn.ScaleTo(1.0, 80, Easing.CubicOut);
    }
    private async void OnDeleteGoalClicked(object sender, EventArgs e)
    {
        if (sender is Button b4) await AnimateButton(b4);
        if (sender is Button btn)
        {
            await btn.ScaleTo(0.85, 80, Easing.CubicIn);
            await btn.ScaleTo(1.0, 80, Easing.CubicOut);
        }
        if ((sender as Button)?.CommandParameter is not SavingsGoal goal)
            return;

        bool confirm = await DisplayAlert(
            "Удаление",
            $"Удалить цель '{goal.Name}'? Это действие необратимо.",
            "Да", "Нет");

        if (!confirm)
            return;

        try
        {
            await App.Database.DeleteGoalAsync(goal);
            UserSavings.Remove(goal);
            await LoadDataFromDatabase();

            await DisplayAlert("Успех", "Цель удалена", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось удалить цель: {ex.Message}", "OK");
        }
    }

    #endregion

    #region --- 6. ЛОГИКА ПОДПИСОК ---
    private async void OnDeleteSubscriptionClicked(object sender, EventArgs e)
    {
        if (sender is Button b8) await AnimateButton(b8);
        if ((sender as Button)?.CommandParameter is not Subscription sub)
            return;

        bool confirm = await DisplayAlert(
            "Удаление",
            $"Удалить подписку '{sub.Name}'?",
            "Да", "Нет");

        if (!confirm)
            return;

        try
        {
            await App.Database.DeleteSubscriptionAsync(sub);
            Subscriptions.Remove(sub);
            await LoadDataFromDatabase();

            await DisplayAlert("Успех", "Подписка удалена", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось удалить подписку: {ex.Message}", "OK");
        }
    }



    #endregion

    #region --- 7. ОБНОВЛЕНИЕ UI И ГРАФИКИ ---

    private async Task UpdateDashboardUI()
    {
        if (ChartV == null || BalanceLabel == null) return;

        // Снимок коллекции на главном потоке ДО Task.Run — устраняет "Collection was modified"
        var snapshot = TransactionHistory?.ToList() ?? new List<Transaction>();

        decimal r = 1.0m;
        string s = "₽";
        if (_currentCurrency == "USD") { r = (decimal)_usdRate; s = "$"; }
        else if (_currentCurrency == "EUR") { r = (decimal)_eurRate; s = "€"; }

        var result = await Task.Run(() =>
        {
            var stats = snapshot
                .Where(t => !t.IsIncome)
                .GroupBy(t => t.Category)
                .Select(g => new ExpenseCategoryItem
                {
                    Category = g.Key,
                    Sum = g.Sum(t => t.Amount) / r,
                    AmountText = (g.Sum(t => t.Amount) / r).ToString("N0") + " " + s,
                    DisplayColor = GetColorForCategory(g.Key)
                })
                .OrderByDescending(x => x.Sum)
                .ToList();

            decimal balance = snapshot.Sum(t => t.IsIncome ? t.Amount : -t.Amount) / r;

            return new { Balance = balance, Symbol = s, Stats = stats };
        });

        // Все UI-операции строго на главном потоке
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            BalanceLabel.Text = $"{result.Balance:N0} {result.Symbol}";

            bool hasTransactions = snapshot.Any();
            bool hasStats = result.Stats != null && result.Stats.Any();

            UpdateLayoutVisibility(hasTransactions, hasStats);

            if (hasStats)
            {
                var newEntries = result.Stats.Select(x => new ChartEntry((float)x.Sum)
                {
                    Label = x.Category,
                    ValueLabel = x.AmountText,
                    Color = new SKColor(
                        (byte)(x.DisplayColor.Red * 255),
                        (byte)(x.DisplayColor.Green * 255),
                        (byte)(x.DisplayColor.Blue * 255),
                        255)
                }).ToArray();

                // Переиспользуем существующий график если он уже есть — не пересоздаём
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
                ChartV.Chart = null;

                if (AtlasPeriodSuggestionLabel != null)
                {
                    string[] atlasPhrases = {
                    "Расходы за выбранный период равны 0. Идеальный баланс.",
                    "Анализ завершен: трат не зафиксировано.",
                    "В этом месяце чисто. Ваш кошелек в безопасности.",
                    "Система фиксирует нулевую активность расходов."
                };
                    AtlasPeriodSuggestionLabel.Text = atlasPhrases[new Random().Next(atlasPhrases.Length)];
                }
            }
        });
    }

    #region --- 8. СЕРВИСНЫЕ МЕТОДЫ ---

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
        "💰 Возврат долга" => Color.FromArgb("#D51848"),
        "💸 Выплата долга" => Color.FromArgb("#FFF700"),
        _ => Color.FromArgb("#94A3B8")                // Серый (для остальных)
    };

    // ===== HOVER: РАСХОД/ДОХОД =====
    private async void OnExpenseHoverEnter(object sender, PointerEventArgs e)
    {
        if (_selectedType == "expense") return;
        BtnExpenseBorder.Stroke = new SolidColorBrush(Color.FromArgb("#FF00FF"));
        BtnExpenseBorder.StrokeThickness = 2;
        LblExpense.TextColor = Color.FromArgb("#D946EF");
        await BtnExpenseBorder.ScaleTo(1.01, 120, Easing.CubicOut);
    }

    private async void OnExpenseHoverExit(object sender, PointerEventArgs e)
    {
        if (_selectedType == "expense") return;
        BtnExpenseBorder.Stroke = new SolidColorBrush(Color.FromArgb("#D946EF"));
        BtnExpenseBorder.StrokeThickness = 1;
        LblExpense.TextColor = Color.FromArgb("#D946EF");
        await BtnExpenseBorder.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    private async void OnIncomeHoverEnter(object sender, PointerEventArgs e)
    {
        if (_selectedType == "income") return;
        BtnIncomeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#00FFE5"));
        BtnIncomeBorder.StrokeThickness = 2;
        LblIncome.TextColor = Color.FromArgb("#2DD4BF");
        await BtnIncomeBorder.ScaleTo(1.01, 120, Easing.CubicOut);
    }

    private async void OnIncomeHoverExit(object sender, PointerEventArgs e)
    {
        if (_selectedType == "income") return;
        BtnIncomeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#2DD4BF"));
        BtnIncomeBorder.StrokeThickness = 1;
        LblIncome.TextColor = Color.FromArgb("#2DD4BF");
        await BtnIncomeBorder.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    private async void OnExpenseClicked(object sender, TappedEventArgs e)
    {
        _selectedType = "expense";

        // Сбрасываем доход
        BtnIncomeBorder.BackgroundColor = Colors.Transparent;
        BtnIncomeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#2DD4BF"));
        BtnIncomeBorder.StrokeThickness = 1;
        LblIncome.TextColor = Color.FromArgb("#2DD4BF");

        // Активируем расход
        BtnExpenseBorder.BackgroundColor = Color.FromArgb("#6B1580");
        BtnExpenseBorder.Stroke = new SolidColorBrush(Color.FromArgb("#FF00FF"));
        BtnExpenseBorder.StrokeThickness = 2;
        LblExpense.TextColor = Colors.White;

        await BtnExpenseBorder.ScaleTo(0.96, 80, Easing.CubicIn);
        await BtnExpenseBorder.ScaleTo(1.0, 80, Easing.CubicOut);

        SetTransactionMode(false);
    }

    private async void OnIncomeClicked(object sender, TappedEventArgs e)
    {
        _selectedType = "income";

        // Сбрасываем расход
        BtnExpenseBorder.BackgroundColor = Colors.Transparent;
        BtnExpenseBorder.Stroke = new SolidColorBrush(Color.FromArgb("#D946EF"));
        BtnExpenseBorder.StrokeThickness = 1;
        LblExpense.TextColor = Color.FromArgb("#D946EF");

        // Активируем доход
        BtnIncomeBorder.BackgroundColor = Color.FromArgb("#0D5C52");
        BtnIncomeBorder.Stroke = new SolidColorBrush(Color.FromArgb("#00FFE5"));
        BtnIncomeBorder.StrokeThickness = 2;
        LblIncome.TextColor = Colors.White;

        await BtnIncomeBorder.ScaleTo(0.96, 80, Easing.CubicIn);
        await BtnIncomeBorder.ScaleTo(1.0, 80, Easing.CubicOut);
        SetTransactionMode(true);
    }

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
        // Даже если здесь будет пусто, ошибка исчезнет
        await DisplayAlert("Экспорт", "Функция экспорта в CSV будет добавлена позже", "OK");
    }
    private async void OnResetStatsClicked(object sender, EventArgs e)
    {
        if (sender is Button b7) await AnimateButton(b7);
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
        await Shell.Current.GoToAsync("StatisticsPage");
        _isAnimating = false;
    }

    private async void OnBackToChartClicked(object sender, EventArgs e)
    {
        // Очищаем график на текущей странице (если он там есть), чтобы облегчить память
        // Это критично для предотвращения фризов при переходе через //

        await Shell.Current.GoToAsync("..");
    }
    private void UpdateDonutChart(List<ExpenseCategoryItem> data)
    {
        if (data == null || !data.Any())
        {
            ChartV.Chart = null;
            return;
        }

        // Создаем записи для диаграммы на основе данных из легенды
        var entries = data.Select(d => new ChartEntry((float)d.Sum)
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
            LabelMode = LabelMode.None,      // Скрываем лишние подписи
            GraphPosition = GraphPosition.Center
        };
    }
    private async void OnShareReportClicked(object sender, EventArgs e)
    {
        if (sender is Button b5) await AnimateButton(b5);
        try
        {
            var transactions = await App.Database.GetTransactionsAsync();

            if (transactions == null || !transactions.Any())
            {
                await DisplayAlert("Ошибка", "Нет данных для экспорта", "OK");
                return;
            }

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Дата,Категория,Описание,Сумма,Тип");

            foreach (var t in transactions)
            {
                string type = t.IsIncome ? "Доход" : "Расход";
                csvBuilder.AppendLine($"{t.Date:dd.MM.yyyy},{t.Category},{t.Description},{t.Amount},{type}");
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"Monolith_{DateTime.Now:dd-MM-yyyy_HHmm}.csv";
            string filePath = Path.Combine(desktop, fileName);

            await File.WriteAllTextAsync(filePath, csvBuilder.ToString(), Encoding.UTF8);
            await DisplayAlert("✅ Готово", $"CSV сохранён на рабочий стол:\n{fileName}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        if (sender is Button b6) await AnimateButton(b6);
        try
        {
            var document = new PdfSharpCore.Pdf.PdfDocument();
            document.Info.Title = "Monolith Report";

            var page = document.AddPage();
            var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
            var fontBold = new PdfSharpCore.Drawing.XFont("Arial", 20, PdfSharpCore.Drawing.XFontStyle.Bold);
            var fontNormal = new PdfSharpCore.Drawing.XFont("Arial", 12, PdfSharpCore.Drawing.XFontStyle.Regular);
            var fontSmall = new PdfSharpCore.Drawing.XFont("Arial", 10, PdfSharpCore.Drawing.XFontStyle.Regular);

            gfx.DrawString("MONOLITH — Финансовый отчёт", fontBold,
                PdfSharpCore.Drawing.XBrushes.Black,
                new PdfSharpCore.Drawing.XRect(0, 40, page.Width, 30),
                PdfSharpCore.Drawing.XStringFormats.TopCenter);

            gfx.DrawString($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}", fontSmall,
                PdfSharpCore.Drawing.XBrushes.Gray,
                new PdfSharpCore.Drawing.XRect(0, 75, page.Width, 20),
                PdfSharpCore.Drawing.XStringFormats.TopCenter);

            gfx.DrawLine(PdfSharpCore.Drawing.XPens.LightGray, 40, 100, page.Width - 40, 100);

            var transactions = App.GlobalHistory.ToList();
            double totalIncome = (double)transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            double totalExpense = (double)transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
            double balance = totalIncome - totalExpense;

            gfx.DrawString("БАЛАНС", fontBold, PdfSharpCore.Drawing.XBrushes.Black, 40, 130);
            gfx.DrawString($"Доходы:   +{totalIncome:N0} ₽", fontNormal, PdfSharpCore.Drawing.XBrushes.DarkGreen, 40, 160);
            gfx.DrawString($"Расходы:  -{totalExpense:N0} ₽", fontNormal, PdfSharpCore.Drawing.XBrushes.DarkRed, 40, 185);
            gfx.DrawString($"Итого:     {balance:N0} ₽", fontBold, PdfSharpCore.Drawing.XBrushes.Black, 40, 215);

            gfx.DrawLine(PdfSharpCore.Drawing.XPens.LightGray, 40, 235, page.Width - 40, 235);
            gfx.DrawString("ИСТОРИЯ ТРАНЗАКЦИЙ", fontBold, PdfSharpCore.Drawing.XBrushes.Black, 40, 255);

            double y = 285;
            foreach (var t in transactions.OrderByDescending(x => x.Date).Take(20))
            {
                if (y > page.Height - 60) break;
                var color = t.Amount > 0 ? PdfSharpCore.Drawing.XBrushes.DarkGreen : PdfSharpCore.Drawing.XBrushes.DarkRed;
                gfx.DrawString($"{t.Date:dd.MM.yy}  {t.Category,-20} {t.Amount:+#,##0;-#,##0} ₽", fontNormal, color, 40, y);
                y += 22;
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"Monolith_Report_{DateTime.Now:dd-MM-yyyy_HHmm}.pdf";
            string filePath = Path.Combine(desktop, fileName);

            using var stream = File.Create(filePath);
            document.Save(stream);

            await DisplayAlert("✅ Готово", $"PDF сохранён на рабочий стол:\n{fileName}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
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
    private void UpdateLayoutVisibility(bool hasTransactions, bool hasStats)
    {
        // 1. Уровень контейнеров (переключаем между "Пусто" и "Есть контент")
        ChartVisualElements.IsVisible = hasTransactions;
        EmptyStatePlaceholder.IsVisible = !hasTransactions;

        // 2. Если внутри контента есть данные, решаем, что показать
        if (hasTransactions)
        {
            // Если есть статистика (hasStats == true) -> Показываем графики и анализатор
            MainDashboardView.IsVisible = hasStats;
            AtlasLoadingView.IsVisible = hasStats; // Тот самый анализатор
            ChartInstructionBorder.IsVisible = hasStats;

            // Если статистики нет (hasStats == false) -> Показываем заглушку Атласа
            AtlasPeriodPlaceholder.IsVisible = !hasStats;
        }
    }
}
    #endregion