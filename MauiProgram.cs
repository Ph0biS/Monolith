using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using PROJECT.Pages;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace PROJECT;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp() // Регистрируем Microcharts
            .UseMicrocharts()
            // Регистрируем CommunityToolkit (обязательно для анимаций и Popup)

            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter.ttf", "Inter");
                fonts.AddFont("JetBrainsMono.ttf", "JetBrainsMono");
                fonts.AddFont("Orbitron.ttf", "Orbitron");
                fonts.AddFont("Montserrat.ttf", "MontserratBold");
                fonts.AddFont("RobotoMono.ttf", "RobotoMono");
                fonts.AddFont("OpenSans.ttf", "OpenSans"); // Используй файл, который называется просто OpenSans.ttf
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        Microsoft.Maui.Handlers.ElementHandler.ElementMapper.AppendToMapping("CustomTabBar", (handler, view) =>
        {
            if (view is TabBar)
            {
#if ANDROID
                // Центрируем иконки и текст для Android
                // (Здесь можно добавить специфичные настройки для платформы)
#endif
            }
        });
        builder.Services.AddSingleton<PROJECT.Services.DatabaseService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<StatisticsPage>();
        builder.Services.AddSingleton<ForecastPage>();
        builder.Services.AddSingleton<AchievementsPage>();
        return builder.Build();
    }
}