using System;
using System.Collections.Generic;
using System.Linq;
using PROJECT.Models; 

namespace PROJECT.Services;

public class FinanceService
{
    // Метод расчета расходов
    public static double CalculateTotalExpenses(List<Transaction> transactions)
    {
        // Считаем сумму только для тех операций, где IsIncome == false (расходы)
        return (double)transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
    }

    public static string GetSurvivalForecast(double balance, double spent)
    {
        int daysPassed = Math.Max(DateTime.Now.Day, 1);
        double dailyAverage = spent / daysPassed;

        if (dailyAverage <= 0) return "Недостаточно данных для прогноза";

        int daysLeft = (int)(balance / dailyAverage);
        return $"Денег хватит на {daysLeft} дн.";
    }
}