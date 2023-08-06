using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Leds;
using Meadow.Peripherals.Leds;
using System;
using System.Threading.Tasks;

namespace MeadowTest.Cellular
{
    internal class LedController : MeadowBase
    {
        #region Singleton Declaration

        private static readonly Lazy<LedController> instance =
            new Lazy<LedController>(() => new LedController());

        public static LedController Instance => instance.Value;

        private LedController()
        {
            Log.Info("LedController initializing...");

            _device = MeadowApp.Device;

            _led = new RgbPwmLed(
                redPwmPin: _device.Pins.OnboardLedRed,
                greenPwmPin: _device.Pins.OnboardLedGreen,
                bluePwmPin: _device.Pins.OnboardLedBlue,
                CommonType.CommonAnode);

            Log.Info("LedController up.");
        }

        #endregion Singleton Declaration

        readonly RgbPwmLed _led;
        readonly F7FeatherV2 _device;
        readonly TimeSpan _slowBlinkSpeed = TimeSpan.FromMilliseconds(500);
        readonly TimeSpan _fastBlinkSpeed = TimeSpan.FromMilliseconds(100);

        public void SetState(LedState state)
        {
            switch (state)
            {
                case LedState.Boot:
                    SetColor(Color.Yellow);
                    break;

                case LedState.Cell:
                    Blink(Color.Blue, _slowBlinkSpeed);
                    break;

                case LedState.Network:
                    Blink(Color.Blue, _slowBlinkSpeed);
                    break;

                case LedState.Time:
                    Blink(Color.Blue, _fastBlinkSpeed);
                    break;

                case LedState.Idle:
                    SetColor(Color.DarkGreen);
                    break;

                case LedState.Error:
                    SetColor(Color.Red);
                    break;

                default:
                    SetColor(Color.White);
                    break;
            }
        }

        public async Task ShowColorPulse(Color color, TimeSpan duration)
        {
            await _led.StartPulse(color, duration / 2);
            await Task.Delay(duration);
        }

        private void SetColor(Color color)
        {
            _led.StopAnimation();
            _led.SetColor(color);
        }

        private void Blink(Color color, TimeSpan speed)
        {
            _led.StopAnimation();
            _led.StartBlink(color, speed, speed);
        }

        public enum LedState
        {
            Boot,
            Cell,
            Network,
            Time,
            Idle,
            Error
        }
    }
}
