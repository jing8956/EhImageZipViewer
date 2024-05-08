using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using AndroidX.Core.Content;
using _Microsoft.Android.Resource.Designer;
using Android.Views;

namespace EhImageZipViewer
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            var statusBarColor = ContextCompat.GetColor(this, ResourceConstant.Color.statusBarColor);
            Window?.SetStatusBarColor(new Android.Graphics.Color(statusBarColor));

            var navigationBarColor = ContextCompat.GetColor(this, ResourceConstant.Color.navigationBarColor);
            Window?.SetNavigationBarColor(new Android.Graphics.Color(navigationBarColor));

            var isNightMode = (newConfig.UiMode & UiMode.NightMask) == UiMode.NightYes;

            if(OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                Window?.InsetsController?.SetSystemBarsAppearance(
                    isNightMode ? 0 : (int)WindowInsetsControllerAppearance.LightStatusBars,
                    (int)WindowInsetsControllerAppearance.LightStatusBars);
                Window?.InsetsController?.SetSystemBarsAppearance(
                    isNightMode ? 0 : (int)WindowInsetsControllerAppearance.LightNavigationBars,
                    (int)WindowInsetsControllerAppearance.LightNavigationBars);
            }
        }
    }
}
