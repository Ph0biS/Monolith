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

            // БЛОК 1: ПЕРВЫЕ ШАГИ
            
            // 1. First Blood — первая запись
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

            // 2. Double Kill — доход и расход в один день
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

            // 3. Maniac — 5 операций за день
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

            // 4. Коллекционер — 5 разных категорий
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

            // 5. Всезнайка — все 8 категорий расходов использованы
            achievements.Add(new Achievement
            {
                Name = "Allrounder",
                Title = "Всезнайка",
                Description = "Использованы все 8 категорий расходов",
                Icon = "🧠",
                IsUnlocked = catCount >= 8,
                CurrentValue = catCount,
                TargetValue = 8
            });

            // БЛОК 2: БАЛАНС И ДЕНЬГИ

            // 6. Сберегатель — баланс выше 50 000 ₽
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

            // 7. Godlike — баланс выше 1 000 000 ₽
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

            // 8. Savage — разовый доход более 100 000 ₽
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

            // 9. День зарплаты — ровная сумма дохода
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

            // 10. Миллионер на час — суммарный доход за всё время > 1 000 000 ₽
            var totalIncome = (double)income.Sum(t => t.Amount);
            achievements.Add(new Achievement
            {
                Name = "Millionaire",
                Title = "Миллионер на час",
                Description = "Суммарный доход превысил 1 000 000 ₽",
                Icon = "💎",
                IsUnlocked = totalIncome >= 1000000,
                CurrentValue = totalIncome,
                TargetValue = 1000000
            });

            // 11. Первый миллион трат — суммарные расходы > 1 000 000 ₽
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

            // 12. Скупой рыцарь — баланс выше 200 000 ₽
            achievements.Add(new Achievement
            {
                Name = "Miser",
                Title = "Скупой рыцарь",
                Description = "Баланс выше 200 000 ₽",
                Icon = "🏰",
                IsUnlocked = balance >= 200000,
                CurrentValue = balance > 0 ? balance : 0,
                TargetValue = 200000
            });

            // БЛОК 3: КАТЕГОРИИ ТРАТ

            // 13. Гурман — 10 записей в категории Еда
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

            // 14. Шеф-повар — 25 записей в категории Еда
            achievements.Add(new Achievement
            {
                Name = "Chef",
                Title = "Шеф-повар",
                Description = "25 записей в категории 'Еда'",
                Icon = "👨‍🍳",
                IsUnlocked = foodCount >= 25,
                CurrentValue = foodCount,
                TargetValue = 25
            });

            // 15. Путешественник — трата на транспорт
            int travelDone = expenses.Any(t => t.Category.Contains("Транспорт")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Traveler",
                Title = "Путешественник",
                Description = "Первая трата на транспорт",
                Icon = "🚕",
                IsUnlocked = travelDone == 1,
                CurrentValue = travelDone,
                TargetValue = 1
            });

            // 16. Legendary Gamer — траты на развлечения > 10 000 ₽
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

            // 17. Инвестор — категория Инвестиции
            int investorDone = history.Any(t => t.Category.Contains("Инвестиции")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Investor",
                Title = "Инвестор",
                Description = "Первая запись 'Инвестиции'",
                Icon = "📈",
                IsUnlocked = investorDone == 1,
                CurrentValue = investorDone,
                TargetValue = 1
            });

            // 18. Здоровье прежде всего — трата на здоровье
            int healthDone = expenses.Any(t => t.Category.Contains("Здоровье")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "HealthFirst",
                Title = "Здоровье прежде всего",
                Description = "Первая трата на здоровье",
                Icon = "💊",
                IsUnlocked = healthDone == 1,
                CurrentValue = healthDone,
                TargetValue = 1
            });

            // 19. Домосед — траты на дом > 5 000 ₽
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

            // 20. Модник — трата на одежду
            int fashionDone = expenses.Any(t => t.Category.Contains("Одежда")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Fashionista",
                Title = "Модник",
                Description = "Первая трата на одежду",
                Icon = "👗",
                IsUnlocked = fashionDone == 1,
                CurrentValue = fashionDone,
                TargetValue = 1
            });

            // 21. Всегда на связи — трата на связь
            int commDone = expenses.Any(t => t.Category.Contains("Связь")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Connected",
                Title = "Всегда на связи",
                Description = "Первая трата на связь",
                Icon = "📱",
                IsUnlocked = commDone == 1,
                CurrentValue = commDone,
                TargetValue = 1
            });

            // 22. Шопоголик — более 20 покупок за всё время
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

            // 23. Транжира — более 50 покупок за всё время
            achievements.Add(new Achievement
            {
                Name = "Spender",
                Title = "Транжира",
                Description = "Более 50 покупок за всё время",
                Icon = "💸",
                IsUnlocked = expenses.Count >= 50,
                CurrentValue = expenses.Count,
                TargetValue = 50
            });

            // 24. Продуктовый маньяк — траты на продукты > 20 000 ₽
            var grocerySpend = (double)expenses.Where(t => t.Category.Contains("Продукты")).Sum(t => t.Amount);
            achievements.Add(new Achievement
            {
                Name = "GroceryMaster",
                Title = "Продуктовый маньяк",
                Description = "Траты на продукты > 20 000 ₽",
                Icon = "🛒",
                IsUnlocked = grocerySpend >= 20000,
                CurrentValue = grocerySpend,
                TargetValue = 20000
            });

            // 25. Меценат — трата на подарок
            int giftDone = expenses.Any(t => t.Category.Contains("Подарок")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Patron",
                Title = "Меценат",
                Description = "Первая трата на подарок",
                Icon = "🎁",
                IsUnlocked = giftDone == 1,
                CurrentValue = giftDone,
                TargetValue = 1
            });

            // БЛОК 4: АКТИВНОСТЬ И СТРИКИ

            // 26. Unstoppable — 7 дней подряд активности
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

            // 27. Легенда — 30 дней подряд активности
            achievements.Add(new Achievement
            {
                Name = "Legend",
                Title = "Легенда",
                Description = "30 дней подряд активности",
                Icon = "🌟",
                IsUnlocked = currentStrike >= 30,
                CurrentValue = currentStrike,
                TargetValue = 30
            });

            // 28. Monster Kill — 10 транзакций подряд без доходов
            var streak = GetCurrentExpenseStreak(history);
            achievements.Add(new Achievement
            {
                Name = "MonsterKill",
                Title = "Monster Kill",
                Description = "10 трат подряд без доходов",
                Icon = "👹",
                IsUnlocked = streak >= 10,
                CurrentValue = streak,
                TargetValue = 10
            });

            // 29. Сотня — 100 операций за всё время
            achievements.Add(new Achievement
            {
                Name = "Century",
                Title = "Сотня",
                Description = "100 операций за всё время",
                Icon = "💯",
                IsUnlocked = history.Count >= 100,
                CurrentValue = history.Count,
                TargetValue = 100
            });

            // 30. Рекордсмен — 10 операций за один день
            var maxPerDay = history.GroupBy(t => t.Date.Date).Select(g => g.Count()).DefaultIfEmpty(0).Max();
            achievements.Add(new Achievement
            {
                Name = "RecordBreaker",
                Title = "Рекордсмен",
                Description = "10 операций за один день",
                Icon = "🏆",
                IsUnlocked = maxPerDay >= 10,
                CurrentValue = maxPerDay,
                TargetValue = 10
            });

            // 31. Марафонец — 50 операций за всё время
            achievements.Add(new Achievement
            {
                Name = "Marathon",
                Title = "Марафонец",
                Description = "50 операций за всё время",
                Icon = "🏅",
                IsUnlocked = history.Count >= 50,
                CurrentValue = history.Count,
                TargetValue = 50
            });

            // БЛОК 5: ВРЕМЯ СУТОК

            // 32. Ночная сова — запись ночью
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

            // 33. Ранняя пташка — запись ранним утром
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

            // 34. Полночный финансист — запись ровно в полночь (23-00)
            int midnightDone = history.Any(t => t.Date.Hour == 23 || t.Date.Hour == 0) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "MidnightFinancier",
                Title = "Полночный финансист",
                Description = "Запись сделана в 23:00-00:59",
                Icon = "🌙",
                IsUnlocked = midnightDone == 1,
                CurrentValue = midnightDone,
                TargetValue = 1
            });

            // 35. Обеденный перерыв — запись в обеденное время
            int lunchDone = history.Any(t => t.Date.Hour >= 12 && t.Date.Hour < 14) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "LunchBreak",
                Title = "Обеденный перерыв",
                Description = "Запись сделана в обед (12-14)",
                Icon = "🍱",
                IsUnlocked = lunchDone == 1,
                CurrentValue = lunchDone,
                TargetValue = 1
            });

            // БЛОК 6: ОСОБЫЕ СУММЫ

            // 36. Копейка рубль бережёт — трата ровно 1 ₽
            int pennySaved = history.Any(t => t.Amount == 1) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "PennySaved",
                Title = "Копейка рубль бережёт",
                Description = "Запись ровно на 1 ₽",
                Icon = "🪙",
                IsUnlocked = pennySaved == 1,
                CurrentValue = pennySaved,
                TargetValue = 1
            });

            // 37. Круглая сумма — трата кратная 10 000 ₽
            int roundSum = expenses.Any(t => t.Amount % 10000 == 0 && t.Amount >= 10000) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "RoundNumber",
                Title = "Круглая сумма",
                Description = "Трата кратная 10 000 ₽",
                Icon = "🎯",
                IsUnlocked = roundSum == 1,
                CurrentValue = roundSum,
                TargetValue = 1
            });

            // 38. Большой куш — разовая трата более 50 000 ₽
            var maxExpense = (double)(expenses.Any() ? expenses.Max(t => t.Amount) : 0);
            achievements.Add(new Achievement
            {
                Name = "BigSpend",
                Title = "Большой куш",
                Description = "Разовая трата более 50 000 ₽",
                Icon = "💣",
                IsUnlocked = maxExpense >= 50000,
                CurrentValue = maxExpense,
                TargetValue = 50000
            });

            // 39. Мелочь пузатая — 10 трат меньше 100 ₽
            var smallExpenses = expenses.Count(t => t.Amount < 100);
            achievements.Add(new Achievement
            {
                Name = "SmallChange",
                Title = "Мелочь пузатая",
                Description = "10 трат меньше 100 ₽",
                Icon = "🐷",
                IsUnlocked = smallExpenses >= 10,
                CurrentValue = smallExpenses,
                TargetValue = 10
            });

            // БЛОК 7: ДОЛГИ И ЦЕЛИ

            // 40. Честный человек — запись "Выплата долга"
            int debtPaid = history.Any(t => t.Category.Contains("Выплата долга")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "HonestMan",
                Title = "Честный человек",
                Description = "Выплатил долг",
                Icon = "🤝",
                IsUnlocked = debtPaid == 1,
                CurrentValue = debtPaid,
                TargetValue = 1
            });

            // 41. Кредитор — запись "Возврат долга"
            int debtReturned = history.Any(t => t.Category.Contains("Возврат долга")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Creditor",
                Title = "Кредитор",
                Description = "Получил возврат долга",
                Icon = "💼",
                IsUnlocked = debtReturned == 1,
                CurrentValue = debtReturned,
                TargetValue = 1
            });

            // 42. Целеустремлённый — первое пополнение копилки
            int goalDeposit = history.Any(t => t.Category.Contains("Цели")) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Goalseeker",
                Title = "Целеустремлённый",
                Description = "Первое пополнение копилки",
                Icon = "🎯",
                IsUnlocked = goalDeposit == 1,
                CurrentValue = goalDeposit,
                TargetValue = 1
            });

            // 43. Накопитель — 5 пополнений копилки
            var goalDeposits = history.Count(t => t.Category.Contains("Цели"));
            achievements.Add(new Achievement
            {
                Name = "Accumulator",
                Title = "Накопитель",
                Description = "5 пополнений копилки",
                Icon = "🏦",
                IsUnlocked = goalDeposits >= 5,
                CurrentValue = goalDeposits,
                TargetValue = 5
            });

            // БЛОК 8: БАЛАНС И ЭКОНОМИЯ

            // 44. Экономист — доходы превышают расходы в 2 раза
            var incomeTotal2 = (double)income.Sum(t => t.Amount);
            var expenseTotal2 = (double)expenses.Sum(t => t.Amount);
            int economist = (expenseTotal2 > 0 && incomeTotal2 >= expenseTotal2 * 2) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Economist",
                Title = "Экономист",
                Description = "Доходы в 2 раза больше расходов",
                Icon = "📊",
                IsUnlocked = economist == 1,
                CurrentValue = economist,
                TargetValue = 1
            });

            // 45. Без трат — день без единой траты (но с доходом)
            var daysWithIncomeNoExpense = income
                .Select(t => t.Date.Date)
                .Distinct()
                .Count(d => !expenses.Any(e => e.Date.Date == d));
            achievements.Add(new Achievement
            {
                Name = "ZeroDay",
                Title = "День без трат",
                Description = "День с доходом и без расходов",
                Icon = "😇",
                IsUnlocked = daysWithIncomeNoExpense >= 1,
                CurrentValue = daysWithIncomeNoExpense,
                TargetValue = 1
            });

            // 46. Стабильный доход — 3 записи дохода за один месяц
            var incomePerMonth = income
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Max();
            achievements.Add(new Achievement
            {
                Name = "StableIncome",
                Title = "Стабильный доход",
                Description = "3 дохода за один месяц",
                Icon = "📅",
                IsUnlocked = incomePerMonth >= 3,
                CurrentValue = incomePerMonth,
                TargetValue = 3
            });

            // БЛОК 9: СКРЫТЫЕ И СЕКРЕТНЫЕ

            // 47. Скрытый — запись в воскресенье
            int sundayDone = history.Any(t => t.Date.DayOfWeek == DayOfWeek.Sunday) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "SundayFinancier",
                Title = "Воскресный финансист",
                Description = "Запись сделана в воскресенье",
                Icon = "📖",
                IsUnlocked = sundayDone == 1,
                CurrentValue = sundayDone,
                TargetValue = 1
            });

            // 48. Пятница — запись в пятницу
            int fridayDone = history.Any(t => t.Date.DayOfWeek == DayOfWeek.Friday) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "FridayMood",
                Title = "Пятница, пятница!",
                Description = "Запись сделана в пятницу",
                Icon = "🎉",
                IsUnlocked = fridayDone == 1,
                CurrentValue = fridayDone,
                TargetValue = 1
            });

            // 49. Новогодний — запись 31 декабря или 1 января
            int newYearDone = history.Any(t =>
                (t.Date.Month == 12 && t.Date.Day == 31) ||
                (t.Date.Month == 1 && t.Date.Day == 1)) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "NewYear",
                Title = "С Новым годом!",
                Description = "Запись 31 декабря или 1 января",
                Icon = "🎄",
                IsUnlocked = newYearDone == 1,
                CurrentValue = newYearDone,
                TargetValue = 1
            });

            // 50. Перфекционист — ровно 0 ₽ баланса (доходы = расходы)
            int perfectBalance = (history.Any() && balance == 0) ? 1 : 0;
            achievements.Add(new Achievement
            {
                Name = "Perfectionist",
                Title = "Перфекционист",
                Description = "Баланс ровно 0 ₽",
                Icon = "⚖️",
                IsUnlocked = perfectBalance == 1,
                CurrentValue = perfectBalance,
                TargetValue = 1
            });

            return achievements;
        }

        // --- Вспомогательные методы ---

        // Считает максимальную серию трат подряд без доходов
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

        // Считает максимальный стрик дней подряд с активностью
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