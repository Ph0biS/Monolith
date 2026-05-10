using PROJECT;
using Microsoft.Maui.Controls;
using PROJECT.Models;
using PROJECT.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace PROJECT.Pages;

public partial class HistoryPage : ContentPage
{
    // Список для хранения всех данных из базы (для поиска)
    private List<PROJECT.Models.Transaction> _allTransactions = new List<PROJECT.Models.Transaction>();
    public ICommand GoToWalletCommand => new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
    public ICommand GoToHistoryCommand => new Command(async () => await Shell.Current.GoToAsync("//HistoryPage"));
    public ICommand GoToAnalysisCommand => new Command(async () => await Shell.Current.GoToAsync("//AnalysisPage"));
    public ICommand GoToForecastCommand => new Command(async () => await Shell.Current.GoToAsync("//ForecastPage"));
    public ICommand GoToAchievementsCommand => new Command(async () => await Shell.Current.GoToAsync("//AchievementsPage"));
    public HistoryPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryData();
    }

    private async Task LoadHistoryData()
    {
        // 1. Получаем данные из БД
        var transactions = await App.Database.GetTransactionsAsync();

        // 2. Сохраняем в локальный список для поиска
        _allTransactions = transactions.OrderByDescending(t => t.Date).ToList();

        // 3. Обновляем визуальный список
        HistoryList.ItemsSource = _allTransactions;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            HistoryList.ItemsSource = _allTransactions;
        }
        else
        {
            // Теперь поиск ищет и по категории, и по описанию
            HistoryList.ItemsSource = _allTransactions
                .Where(t => (t.Category != null && t.Category.ToLower().Contains(text)) ||
                            (t.Description != null && t.Description.ToLower().Contains(text)))
                .ToList();
        }
    }

    private async void OnDeleteTransaction(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.BindingContext is PROJECT.Models.Transaction t)
        {
            bool confirm = await DisplayAlert("Удаление", "Вы уверены?", "Да", "Нет");
            if (confirm)
            {
                await App.Database.DeleteTransactionAsync(t);
                await LoadHistoryData();
            }
        }
    }
}