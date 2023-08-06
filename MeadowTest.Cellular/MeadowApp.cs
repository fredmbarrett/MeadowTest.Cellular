using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Leds;
using Meadow.Logging;
using System;
using System.Threading.Tasks;
using static Meadow.IPlatformOS;

namespace MeadowTest.Cellular
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards
    public class MeadowApp : App<F7FeatherV2>
    {
        protected static Logger Log { get => Resolver.Log; }

        const string TEST_URL = "https://postman-echo.com/get?foo1=bar1&foo2=bar2";

        TestAppSettings _appSettings;
        RgbPwmLed onboardLed;

        public override async Task Initialize()
        {
            Log.Info("MeadowTest.Cellular initializing...");

            try
            {
                // get apps settings and put into app resolver storage
                _appSettings = new TestAppSettings(Resolver.App.Settings);
                Resolver.Services.Add(_appSettings, typeof(TestAppSettings));

                // initialize the LED controller and set to network state
                LedController.Instance.SetState(LedController.LedState.Boot);

                // wait for cell to initialize
                if (Device.PlatformOS.SelectedNetwork == NetworkConnectionType.Cell)
                {
                    Log.Info($"Boot network is cellular, waiting for cell connection...");
                    LedController.Instance.SetState(LedController.LedState.Cell);
                    await NetworkController.Instance.InitializeCellNetwork();
                }
                else
                {
                    Log.Info($"Boot network is wifi, waiting for network connection...");
                    LedController.Instance.SetState(LedController.LedState.Network);
                    await NetworkController.Instance.InitializeWifiNetwork();
                }

                LedController.Instance.SetState(LedController.LedState.Time);
                await NetworkController.Instance.WaitForNtpTimeUpdate();

            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing MeadowTest.Cellular: {ex.Message}, aborting...");

                LedController.Instance?.SetState(LedController.LedState.Error);

                throw;
            }

            Log.Info("MeadowTest.Cellular initialization complete.");

        }

        public override Task Run()
        {
            Resolver.Log.Info("MeadowTest.Cellular running...");

            return CycleColors(TimeSpan.FromMilliseconds(1000));
        }

        async Task CycleColors(TimeSpan duration)
        {
            Resolver.Log.Info("Cycle colors...");

            while (true)
            {
                await NetworkController.Instance.GetWebPageViaHttpClient(TEST_URL);

                await ShowColorPulse(Color.Blue, duration);
                await ShowColorPulse(Color.Cyan, duration);
                await ShowColorPulse(Color.Green, duration);
                await ShowColorPulse(Color.GreenYellow, duration);
                await ShowColorPulse(Color.Yellow, duration);
                await ShowColorPulse(Color.Orange, duration);
                await ShowColorPulse(Color.OrangeRed, duration);
                await ShowColorPulse(Color.Red, duration);
                await ShowColorPulse(Color.MediumVioletRed, duration);
                await ShowColorPulse(Color.Purple, duration);
                await ShowColorPulse(Color.Magenta, duration);
                await ShowColorPulse(Color.Pink, duration);
            }
        }

        async Task ShowColorPulse(Color color, TimeSpan duration)
        {
            await onboardLed.StartPulse(color, duration / 2);
            await Task.Delay(duration);
        }
    }
}