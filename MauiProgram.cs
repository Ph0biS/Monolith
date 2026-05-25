using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using PROJECT.Pages;
using SkiaSharp.Views.Maui.Controls.Hosting;

#if MACCATALYST
using Microsoft.Maui.Handlers;
using UIKit;
#endif

namespace PROJECT;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMicrocharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter.ttf", "Inter");
                fonts.AddFont("JetBrainsMono.ttf", "JetBrainsMono");
                fonts.AddFont("Orbitron.ttf", "Orbitron");
                fonts.AddFont("Montserrat.ttf", "MontserratBold");
                fonts.AddFont("RobotoMono.ttf", "RobotoMono");
                fonts.AddFont("OpenSans.ttf", "OpenSans");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // --- ПАТЧ ДЛЯ УДАЛЕНИЯ АРТЕФАКТОВ НА MAC CATALYST ---
#if MACCATALYST
using UIKit;
using Microsoft.Maui.Handlers;

public static class MacStyleFixer
{
    public static void Apply()
    {
        // 1. Для Entry (Текстовые поля)
        EntryHandler.Mapper.AppendToMapping("FixBorder", (handler, view) =>
        {
            handler.PlatformView.BorderStyle = UITextBorderStyle.None; // Отключаем стиль Apple
            handler.PlatformView.Layer.BorderWidth = 0;
            handler.PlatformView.Layer.BorderColor = UIColor.Clear.CGColor;
            handler.PlatformView.BackgroundColor = UIColor.Clear;
            // Отключаем системный focus ring
            handler.PlatformView.Layer.ShadowOpacity = 0;
        });

        // 2. Для Editor
        EditorHandler.Mapper.AppendToMapping("FixBorder", (handler, view) =>
        {
            handler.PlatformView.Layer.BorderWidth = 0;
            handler.PlatformView.BackgroundColor = UIColor.Clear;
        });

        // 3. Для Picker и Button
        var buttonFix = new Action<IViewHandler, IView>((handler, view) =>
        {
            if (handler.PlatformView is UIButton btn)
            {
                btn.Configuration = null; // Отключаем стиль кнопки
                btn.Layer.BorderWidth = 0;
                btn.Layer.BorderColor = UIColor.Clear.CGColor;
                btn.BackgroundColor = UIColor.Clear;
            }
        });
        PickerHandler.Mapper.AppendToMapping("FixBorder", buttonFix);
        ButtonHandler.Mapper.AppendToMapping("FixBorder", buttonFix);

    }
}
#endif
        // ----------------------------------------------------

        Microsoft.Maui.Handlers.ElementHandler.ElementMapper.AppendToMapping("CustomTabBar", (handler, view) =>
        {
            if (view is TabBar)
            {
#if ANDROID
                // Центрируем иконки и текст для Android
#endif
            }
        });

        builder.Services.AddSingleton<PROJECT.Services.DatabaseService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<StatisticsPage>();
        builder.Services.AddSingleton<ForecastPage>();
        builder.Services.AddSingleton<AchievementsPage>();

#if MACCATALYST
MacStyleFixer.Apply();
#endif
        return builder.Build();
    }
}