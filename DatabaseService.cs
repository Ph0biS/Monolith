using SQLite;
using PROJECT.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PROJECT.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        private async Task Init()
        {
            if (_database is not null) return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyData.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            await _database.CreateTableAsync<Achievement>();
            await _database.CreateTableAsync<Transaction>();
            await _database.CreateTableAsync<SavingsGoal>();
            await _database.CreateTableAsync<Subscription>();
        }

        // --- ТРАНЗАКЦИИ ---
        public async Task<List<Transaction>> GetTransactionsAsync()
        {
            await Init();
            return await _database.Table<Transaction>().ToListAsync();
        }

        public async Task<int> SaveTransactionAsync(Transaction transaction)
        {
            await Init();
            return transaction.Id != 0
                ? await _database.UpdateAsync(transaction)
                : await _database.InsertAsync(transaction);
        }

        public async Task<int> DeleteTransactionAsync(Transaction t)
        {
            await Init();
            return await _database.DeleteAsync(t);
        }

        // --- КОПИЛКИ (SavingsGoal) ---

        // Получение списка всех целей
        public async Task<List<SavingsGoal>> GetGoalsAsync()
        {
            await Init();
            // Добавим сортировку, чтобы новые или важные цели были сверху
            return await _database.Table<SavingsGoal>().OrderByDescending(x => x.Id).ToListAsync();
        }

        // Универсальный метод: если Id есть — обновляет, если нет — создает
        public async Task<int> SaveGoalAsync(SavingsGoal goal)
        {
            await Init();
            if (goal.Id != 0)
            {
                return await _database.UpdateAsync(goal);
            }
            else
            {
                return await _database.InsertAsync(goal);
            }
        }

        // Удаление цели
        public async Task<int> DeleteGoalAsync(SavingsGoal goal)
        {
            await Init();
            if (goal == null) return 0;
            return await _database.DeleteAsync(goal);
        }

        // --- ПОДПИСКИ (Subscription) ---
        public async Task<List<Subscription>> GetSubscriptionsAsync()
        {
            await Init();
            return await _database.Table<Subscription>().ToListAsync();
        }

        // Переименовал в SaveSubscriptionAsync, чтобы метод совпадал с вызовом в коде страницы
        public async Task<int> SaveSubscriptionAsync(Subscription sub)
        {
            await Init();
            if (sub.Id != 0)
            {
                return await _database.UpdateAsync(sub); // Добавь await здесь
            }
            else
            {
                return await _database.InsertAsync(sub); // И здесь
            }
        }

        // Переименовал в DeleteSubscriptionAsync для соответствия вызовам
        public async Task<int> DeleteSubscriptionAsync(Subscription sub)
        {
            await Init();
            return await _database.DeleteAsync(sub);
        }

        // --- СБРОС ---
        public async Task ClearAllTransactionsAsync()
        {
            await Init();
            await _database.DeleteAllAsync<Transaction>();
        }

        public async Task ClearEverythingAsync()
        {
            await Init();
            await _database.DeleteAllAsync<Transaction>();
            await _database.DeleteAllAsync<SavingsGoal>();
            await _database.DeleteAllAsync<Subscription>();
        }
        public async Task<bool> IsAchievementUnlocked(string name)
        {
            await Init();
            var ach = await _database.Table<Achievement>().FirstOrDefaultAsync(x => x.Name == name);
            return ach?.IsUnlocked ?? false;
        }

        public async Task UnlockAchievement(string name)
        {
            await Init();
            await _database.InsertOrReplaceAsync(new Achievement { Name = name, IsUnlocked = true });
        }
    }

}