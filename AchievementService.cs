using System;
using System.Collections.Generic;
using System.Linq;
using PROJECT.Models;

namespace PROJECT.Services
{
    public static class AchievementService
    {
        public static List<Achievement> CalculateAchievements(List<Transaction> history)
        {
            var achievements = new List<Achievement>();
            if (history == null) history = new List<Transaction>();

            var balance = (double)history.Sum(t => t.IsIncome ? t.Amount : -t.Amount);
            var expenses = history.Where(t => !t.IsIncome).ToList();
            var income = history.Where(t => t.IsIncome).ToList();

            // 1. First Blood
            achievements.Add(new Achievement
            {
                Name = "FirstBlood",
                Title = "First Blood",
                Description = "Первая запись в журнале",
                Icon = "🩸",
                IsUnlocked = history.Any(),
                CurrentValue = history.Any() ? 1 : 0,
                TargetValue = 1
            });

            // 2. Сберегатель
            achievements.Add(new Achievement
            {
                Name = "Saver",
                Title = "Сберегатель",
                Description = "Баланс выше 50 000 ₽",
                Icon = "💰",
                IsUnlocked = balance >= 50000,
                CurrentValue = balance > 0 ? balance : 0,
                TargetValue = 50000
            });

            // 3. Maniac
            var countsToday = history.Count(t => t.Date.Date == DateTime.Today);
            achievements.Add(new Achievement
            {
                Name = "Maniac",
                Title = "Maniac",
                Description = "5 операций за один день",
                Icon = "⚔️",
                IsUnlocked = countsToday >= 5,
                CurrentValue = countsToday,
                TargetValue = 5
            });

            // 4. Double Kill
            int hasBoth = (history.Any(t => t.Date.Date == DateTime.Today && t.IsIncome) &&
                          history.Any(t => t.Date.Date == DateTime.Today && !t.IsIncome)) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "DoubleKill",
                Title = "Double Kill",
                Description = "Доход и расход сегодня",
                Icon = "⚡",
                IsUnlocked = hasBoth == 1,
                CurrentValue = hasBoth,
                TargetValue = 1
            });

            // 5. Гурман
            var foodCount = expenses.Count(t => t.Category.Contains("Еда"));
            achievements.Add(new Achievement
            {
                Name = "Gourmet",
                Title = "Гурман",
                Description = "10 записей в категории 'Еда'",
                Icon = "🍕",
                IsUnlocked = foodCount >= 10,
                CurrentValue = foodCount,
                TargetValue = 10
            });

            // 6. Путешественник
            int travelDone = expenses.Any(t => t.Category.Contains("Транспорт") || t.Category.Contains("Такси")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Traveler",
                Title = "Путешественник",
                Description = "Поездка на такси или транспорте",
                Icon = "🚕",
                IsUnlocked = travelDone == 1,
                CurrentValue = travelDone,
                TargetValue = 1
            });

            // 7. Legendary Gamer
            var gameSpend = (double)expenses.Where(t => t.Category.Contains("Развлечения")).Sum(t => t.Amount);
            achievements.Add(new Achievement
            {
                Name = "Gamer",
                Title = "Legendary Gamer",
                Description = "Траты на развлечения > 10 000 ₽",
                Icon = "🎮",
                IsUnlocked = gameSpend >= 10000,
                CurrentValue = gameSpend,
                TargetValue = 10000
            });

            // 8. Инвестор
            int investorDone = history.Any(t => t.Category.Contains("Инвестиции")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Investor",
                Title = "Инвестор",
                Description = "Создана категория 'Инвестиции'",
                Icon = "📈",
                IsUnlocked = investorDone == 1,
                CurrentValue = investorDone,
                TargetValue = 1
            });

            // 9. Шопоголик
            achievements.Add(new Achievement
            {
                Name = "Shopaholic",
                Title = "Шопоголик",
                Description = "Более 20 покупок за всё время",
                Icon = "🛍️",
                IsUnlocked = expenses.Count >= 20,
                CurrentValue = expenses.Count,
                TargetValue = 20
            });

            // 10. Здоровье прежде всего
            int healthDone = expenses.Any(t => t.Category.Contains("Медицина") || t.Category.Contains("Аптека")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "HealthFirst",
                Title = "Здоровье прежде всего",
                Description = "Запись в медицине",
                Icon = "💊",
                IsUnlocked = healthDone == 1,
                CurrentValue = healthDone,
                TargetValue = 1
            });

            // 11. Домосед
            var houseSpend = (double)expenses.Where(t => t.Category.Contains("Дом")).Sum(t => t.Amount);
            achievements.Add(new Achievement
            {
                Name = "HouseMaster",
                Title = "Домосед",
                Description = "Траты на дом > 5 000 ₽",
                Icon = "🏠",
                IsUnlocked = houseSpend >= 5000,
                CurrentValue = houseSpend,
                TargetValue = 5000
            });

            // 12. Savage
            var maxIncome = (double)(income.Any() ? income.Max(t => t.Amount) : 0);
            achievements.Add(new Achievement
            {
                Name = "Savage",
                Title = "SAVAGE!",
                Description = "Разовый доход более 100 000 ₽",
                Icon = "🔥",
                IsUnlocked = maxIncome >= 100000,
                CurrentValue = maxIncome,
                TargetValue = 100000
            });

            // 13. Monster Kill
            var streak = GetCurrentExpenseStreak(history);
            achievements.Add(new Achievement
            {
                Name = "MonsterKill",
                Title = "Monster Kill",
                Description = "10 транзакций подряд без доходов",
                Icon = "👹",
                IsUnlocked = streak >= 10,
                CurrentValue = streak,
                TargetValue = 10
            });

            // 14. Devourer of Gods
            var totalSpend = (double)expenses.Sum(t => t.Amount);
            achievements.Add(new Achievement
            {
                Name = "Devourer",
                Title = "Devourer of Gods",
                Description = "Общие траты выше 500 000 ₽",
                Icon = "🌌",
                IsUnlocked = totalSpend >= 500000,
                CurrentValue = totalSpend,
                TargetValue = 500000
            });

            // 15. Unstoppable
            var currentStrike = GetCurrentStrike(history);
            achievements.Add(new Achievement
            {
                Name = "Unstoppable",
                Title = "Unstoppable",
                Description = "Пользоваться 7 дней подряд",
                Icon = "🏃",
                IsUnlocked = currentStrike >= 7,
                CurrentValue = currentStrike,
                TargetValue = 7
            });

            // 16. Godlike
            achievements.Add(new Achievement
            {
                Name = "Godlike",
                Title = "Godlike",
                Description = "Баланс выше 1 000 000 ₽",
                Icon = "👑",
                IsUnlocked = balance >= 1000000,
                CurrentValue = balance > 0 ? balance : 0,
                TargetValue = 1000000
            });

            // 17. Ночная сова
            int nightDone = history.Any(t => t.Date.Hour >= 0 && t.Date.Hour < 5) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "NightOwl",
                Title = "Ночная сова",
                Description = "Запись сделана ночью (00-05)",
                Icon = "🦉",
                IsUnlocked = nightDone == 1,
                CurrentValue = nightDone,
                TargetValue = 1
            });

            // 18. Ранняя пташка
            int morningDone = history.Any(t => t.Date.Hour >= 5 && t.Date.Hour < 8) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "EarlyBird",
                Title = "Ранняя пташка",
                Description = "Запись сделана утром (05-08)",
                Icon = "☀️",
                IsUnlocked = morningDone == 1,
                CurrentValue = morningDone,
                TargetValue = 1
            });

            // 19. День зарплаты
            int paydayDone = income.Any(t => t.Amount == 50000 || t.Amount == 100000 || t.Amount == 150000) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Payday",
                Title = "День зарплаты",
                Description = "Доход ровно 50, 100 или 150к",
                Icon = "🏦",
                IsUnlocked = paydayDone == 1,
                CurrentValue = paydayDone,
                TargetValue = 1
            });

            // 20. Коллекционер
            var catCount = expenses.Select(x => x.Category).Distinct().Count();
            achievements.Add(new Achievement
            {
                Name = "Collector",
                Title = "Коллекционер",
                Description = "Использовано 5 разных категорий",
                Icon = "🗃️",
                IsUnlocked = catCount >= 5,
                CurrentValue = catCount,
                TargetValue = 5
            });

            return achievements;
        }

        // --- Вспомогательные методы для прогресса ---

        private static int GetCurrentExpenseStreak(List<Transaction> history)
        {
            int maxStreak = 0;
            int current = 0;
            foreach (var t in history.OrderBy(x => x.Date))
            {
                if (!t.IsIncome) current++;
                else current = 0;
                if (current > maxStreak) maxStreak = current;
            }
            return maxStreak;
        }

        private static int GetCurrentStrike(List<Transaction> history)
        {
            var dates = history.Select(t => t.Date.Date).Distinct().OrderBy(d => d).ToList();
            if (!dates.Any()) return 0;
            int maxStrike = 1;
            int current = 1;
            for (int i = 1; i < dates.Count; i++)
            {
                if ((dates[i] - dates[i - 1]).TotalDays == 1) current++;
                else current = 1;
                if (current > maxStrike) maxStrike = current;
            }
            return maxStrike;
        }
    }
}