using System;
using Avalonia;
using ReactiveUI.Avalonia;
using Patroclus.Avalonia.ViewModels;
using Patroclus.Avalonia.Views;

namespace Patroclus.Avalonia
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch(Exception e)
            {
                Errorlog.logException(e, "Main");
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                //DH1KLM: Avalonia 12 ReactiveUI uses the ReactiveUIBuilder overload.
                .UseReactiveUI(_ => { });
    }
}