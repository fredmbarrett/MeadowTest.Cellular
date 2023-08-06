using Meadow.Hardware;
using Meadow;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Meadow.Gateway.WiFi;
using System.Linq;
using System.Threading;
using System.Net.Http;

namespace MeadowTest.Cellular
{
    internal class NetworkController : MeadowBase
    {
        #region Singleton Declaration

        private static readonly Lazy<NetworkController> instance =
            new Lazy<NetworkController>(() => new NetworkController());

        public static NetworkController Instance => instance.Value;

        private NetworkController()
        {
            Resolver.Device.PlatformOS.NtpClient.TimeChanged += (time) =>
            {
                Log.Info($"Network time changed to {time}");
                IsTimeSet = true;
            };
        }

        #endregion Singleton Declaration

        public IPAddress? IpAddress { get; private set; }
        public IPAddress? SubnetMask { get; private set; }
        public IPAddress? DefaultGateway { get; private set; }

        public string SSID { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsTimeSet { get; private set; }

        IWiFiNetworkAdapter _wifi;

        /// <summary>
        /// Initialize the WiFiController functions
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task InitializeWifiNetwork()
        {
            // TODO: Get from Device Network menu
            SSID = AppSettings.WifiSsid;
            if (string.IsNullOrEmpty(SSID))
                throw new ArgumentNullException(nameof(SSID));

            var passwd = AppSettings.WifiPassword;
            if (string.IsNullOrEmpty(passwd))
                throw new ArgumentNullException(nameof(passwd));

            var sleepTime = TimeSpan.FromSeconds(AppSettings.WifiWakeUpDelaySeconds);
            var timeout = TimeSpan.FromSeconds(AppSettings.WifiTimeoutSeconds);
            var retries = AppSettings.WifiMaxRetryCount;

            Log.Debug($"Initializing NetworkController for network {SSID}...");
            _wifi = MeadowApp.Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
            _wifi.NetworkConnected += OnWifiNetworkConnected;

            await ScanForAccessPoints(_wifi);

            try
            {
                Log.Debug($"...wifi connecting to {SSID}...");
                await _wifi.Connect(SSID, passwd, timeout);

                // Give the wifi network a little more time to come up
                //await Task.Delay(sleepTime);
            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing WiFi: {ex.Message} with {retries} left");
                retries -= 1;

                if (retries == 0)
                    throw new Exception("Cannot connect to network");
            }
        }

        private void OnWifiNetworkConnected(INetworkAdapter sender, NetworkConnectionEventArgs args)
        {
            Log.Debug($"WiFiController.OnNetworkConnected event - args are \n{args.ObjectPropertiesToString()}");
            if (args != null)
            {
                IpAddress = args.IpAddress;
                SubnetMask = args.Subnet;
                DefaultGateway = args.Gateway;
                IsConnected = true;
            }

            Log.Info($"Wifi network {SSID} is up, ip is {DefaultGateway.MapToIPv4()}");
        }

        async Task ScanForAccessPoints(IWiFiNetworkAdapter wifi)
        {
            Resolver.Log.Info("Getting list of access points");
            var networks = await wifi.Scan(TimeSpan.FromSeconds(60));

            if (networks.Count > 0)
            {
                Resolver.Log.Info("|-------------------------------------------------------------|---------|");
                Resolver.Log.Info("|         Network Name             | RSSI |       BSSID       | Channel |");
                Resolver.Log.Info("|-------------------------------------------------------------|---------|");

                foreach (WifiNetwork accessPoint in networks)
                {
                    Resolver.Log.Info($"| {accessPoint.Ssid,-32} | {accessPoint.SignalDbStrength,4} | {accessPoint.Bssid,17} |   {accessPoint.ChannelCenterFrequency,3}   |");
                }
            }
            else
            {
                Resolver.Log.Info($"No access points detected");
            }
        }

        public async Task InitializeCellNetwork()
        {
            var cell = MeadowApp.Device.NetworkAdapters.Primary<ICellNetworkAdapter>();
            var sleepTime = TimeSpan.FromSeconds(AppSettings.CellWakeUpDelaySeconds);

            Log.Info($"Cell network invoked with APN [{AppSettings.CellApnName}]");

            while (!cell.IsConnected)
            {
                await Task.Delay(1000);
            }

            // Give the cell network a little more time to come up
            Thread.Sleep(sleepTime);

            IpAddress = cell.IpAddress;
            SubnetMask = cell.SubnetMask;
            DefaultGateway = cell.Gateway;
            IsConnected = cell.IsConnected;

            Log.Info($"Cellular network is up, ip is {DefaultGateway.MapToIPv4()}");
        }

        public async Task WaitForNtpTimeUpdate()
        {
            Log.Info($"Waiting for Network Time event...");
            if (IsTimeSet == false)
            {
                do
                {
                    await Task.Delay(1000);
                } while (!NetworkController.Instance.IsTimeSet);

                if (IsTimeSet == false)
                    throw new Exception("NTP time update failed");
            }
        }

        public async Task GetWebPageViaHttpClient(string uri)
        {
            Resolver.Log.Info($"Requesting {uri} - {DateTime.Now}");

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 5, 0);

                HttpResponseMessage response = await client.GetAsync(uri);

                try
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Resolver.Log.Info(responseBody);
                }
                catch (TaskCanceledException)
                {
                    Resolver.Log.Info("Request time out.");
                }
                catch (Exception e)
                {
                    Resolver.Log.Info($"Request went sideways: {e.Message}");
                }
            }
        }
    }

    public static class NetworkExtensions
    {
        /// <summary>
        /// Gets an objects properties and their current value
        /// as a string
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static String ObjectPropertiesToString(this object o)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var p in o.GetType().GetProperties().Where(p => p.GetGetMethod().GetParameters().Count() == 0))
            {
                object value = p.GetValue(o, null);
                sb.Append($"{p.Name} = ");

                if (value != null)
                {
                    if (value.GetType().Equals(typeof(byte[])))
                    {
                        sb.AppendLine(((byte[])value).ToByteArrayString());
                    }

                    else
                    {
                        sb.AppendLine(value.ToString());
                    }
                }
                else
                {
                    sb.AppendLine("null");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a byte array into its hexadecimal string equivalent.
        /// </summary>
        /// <param name="value">Byte array to convert</param>
        /// <param name="hyphens">If true, separates each hex byte with a 
        /// hyphen (e.g. "00-00-00"). Defaults to false (no hyphens)</param>
        /// <returns>Formatted hexadecimal string, or String.Empty if value is null.</returns>
        public static string ToByteArrayString(this byte[] value, bool hyphens = false)
        {
            if (value != null)
            {
                var results = BitConverter.ToString(value);
                if (!hyphens)
                    results = results.Replace("-", "");

                return results;
            }

            return String.Empty;
        }
    }
}
