using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace PROJECT.Models
{
    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public int? GoalId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }

        [Ignore]
        public Color AmountColor => IsIncome ? Colors.MediumSeaGreen : Colors.IndianRed;
        [Ignore]
        public string DisplayAmount => IsIncome
    ? $"+{Amount * Subscription.CurrentRate:N0} {Subscription.CurrentSymbol}"
    : $"-{Amount * Subscription.CurrentRate:N0} {Subscription.CurrentSymbol}";

        public string Type { get; internal set; }
    }

    public class SavingsGoal : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Используем только свойства с большой буквы
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public decimal TargetAmount { get; set; }

        private decimal _currentAmount;
        public decimal CurrentAmount // <-- Главное свойство
        {
            get => _currentAmount;
            set
            {
                if (_currentAmount != value)
                {
                    _currentAmount = value;
                    OnPropertyChanged(); // Обновит UI для CurrentAmount
                    OnPropertyChanged(nameof(Progress)); // Обновит ProgressBar
                    OnPropertyChanged(nameof(ProgressText)); // Обновит Label
                }
            }
        }

        // События для обновления интерфейса
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        [Ignore]
        public double Progress => (double)(TargetAmount > 0 ? CurrentAmount / TargetAmount : 0);

        [Ignore]
        public string ProgressText => $"{CurrentAmount:N0} / {TargetAmount:N0} ₽";
    }
    public class Debt
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string PersonName { get; set; } = "";
        public double Amount { get; set; }
        public string Direction { get; set; } = "tome";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsClosed { get; set; } = false;

        public string DirectionIcon => Direction == "tome" ? "💰" : "💸";
        public string DirectionText => Direction == "tome" ? "должен мне" : "я должен";
        public string AmountText => $"{Amount:N0} ₽";
        public string StatusText => IsClosed ? "✅ Закрыт" : "⏳ Активен";
        public string AmountColor => Direction == "tome" ? "#2DD4BF" : "#D946EF";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
        public bool IsActive => !IsClosed;
    }
    public class Subscription
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; } 
        public int PaymentDay { get; set; } 
        public DateTime NextPaymentDate { get; set; } 
        public bool IsActive { get; set; }
        // Для вывода в списке
        [Ignore]
        public string PaymentInfo => $"Списание: {NextPaymentDate:dd.MM.yyyy}";

        public static decimal CurrentRate { get; set; } = 1.0m;
        public static string CurrentSymbol { get; set; } = "₽";

        [Ignore]
        public string DisplayPrice => $"{(Price * CurrentRate):N0} {CurrentSymbol}";
    }
    public class ChartItem
    {
        public string Category { get; set; }
        public float Sum { get; set; }
        public string AmountText { get; set; }
        public Color DisplayColor { get; set; }
    }
    public class ExpenseCategoryItem : INotifyPropertyChanged
    {
        public string Category { get; set; }

        // Используем decimal для точности денег
        public decimal Sum { get; set; }
        public string AmountText { get; set; }
        public Color DisplayColor { get; set; }

        private double _percentage;
        public double Percentage
        {
            get => _percentage;
            set { _percentage = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class Achievement
    {
        [SQLite.PrimaryKey] 
        public string Name { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsUnlocked { get; set; }
        public double CurrentValue { get; set; }
        public double TargetValue { get; set; }

        // Вычисляем процент для ProgressBar (от 0 до 1)
        public double Progress => TargetValue > 0 ? Math.Min(CurrentValue / TargetValue, 1.0) : 0;
        public string ProgressText => $"{CurrentValue:N0} / {TargetValue:N0}";
    }

    public partial class TransactionGroup : List<Transaction>
    {
        public string Date { get; private set; }
        public TransactionGroup(string date, List<Transaction> transactions) : base(transactions)
        {
            Date = date;
        }
    }
    public class StreakData
    {
        [PrimaryKey]
        public int Id { get; set; } = 1;
        public int CurrentStreak { get; set; } = 0;
        public int BestStreak { get; set; } = 0;
        public DateTime LastActivityDate { get; set; } = DateTime.MinValue;
    }
    public class NoteData
    {
        [PrimaryKey]
        public int Id { get; set; } = 1;
        public string Text { get; set; } = "";
    }
}