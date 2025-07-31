using System;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace BatchMonitor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialize Material Design theme
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(Theme.Light);
            theme.SetPrimaryColor(System.Windows.Media.Colors.Blue);
            theme.SetSecondaryColor(System.Windows.Media.Colors.Orange);
            paletteHelper.SetTheme(theme);

            base.OnStartup(e);
        }
    }
}
