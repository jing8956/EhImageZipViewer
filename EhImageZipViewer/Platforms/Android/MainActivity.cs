using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using AndroidX.Core.Content;
using _Microsoft.Android.Resource.Designer;
using Android.Views;
using Android.OS;
using AndroidX.Lifecycle;
using Microsoft.Maui;

namespace EhImageZipViewer
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity CurrentActivity => (MainActivity)ActivityStateManager.Default.GetCurrentActivity()!;

        public FilePickerLifecycleObserver FilePickerLifecycleObserver { get; private set; } = null!;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            FilePickerLifecycleObserver = new FilePickerLifecycleObserver(ActivityResultRegistry);
            Lifecycle.AddObserver(FilePickerLifecycleObserver);
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            if (OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                // TODO: Android 15
            }
            else
            {
                var statusBarColor = ContextCompat.GetColor(this, ResourceConstant.Color.statusBarColor);
                Window?.SetStatusBarColor(new Android.Graphics.Color(statusBarColor));

                var navigationBarColor = ContextCompat.GetColor(this, ResourceConstant.Color.navigationBarColor);
                Window?.SetNavigationBarColor(new Android.Graphics.Color(navigationBarColor));
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var isNightMode = (newConfig.UiMode & UiMode.NightMask) == UiMode.NightYes;

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
