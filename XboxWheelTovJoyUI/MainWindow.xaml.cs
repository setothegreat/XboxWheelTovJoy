using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using vJoyInterfaceWrap;
using static WheelInputTask;
using XboxWheelCompatibility.WheelTransformer;
using Windows.Gaming.Input;

namespace XboxWheelTovJoyUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isRunning = false;
        private WheelInputTask task = new WheelInputTask();
        private WheelInputTask wheelInputTask;
        private Logger logger;
        public MainWindow()
        {
            InitializeComponent();
            logger = new Logger();
            LifecycleManager.Start();
            WheelManager.Initialize();

            this.wheelInputTask = new WheelInputTask(); // Make wheelInputTask an instance variable
            #pragma warning disable CS8622
            this.wheelInputTask.WheelChanged += InputHandler_WheelChanged;
            #pragma warning restore CS8622
            this.wheelInputTask.WheelDisconnected += () => Dispatcher.Invoke(() =>
            {
                WheelStatusTextInfo.Text = "Not detected";
            });
            WheelInputTask.LogMessage += Log;
        }
        public void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.Text += message + Environment.NewLine;

                // Scroll to the end
                LogTextBox.ScrollToEnd();
            });
        }
        public void OnWheelDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                this.WheelStatusTextInfo.Text = "Not detected";
            });
        }
        private async void CheckDevicesStatus()
        {
            await logger.Log("CheckDevicesStatus method started");
            // Check if vJoy is enabled
            if (joystick != null && joystick.vJoyEnabled())
            {
                VJoyStatusTextInfo.Text = "Detected";
            }
            else
            {
                VJoyStatusTextInfo.Text = "Not detected";
            }

            // Check if the wheel is detected
            // Add your wheel detection logic here
            if (WheelManager.MainWheel != null)
            {
                WheelStatusTextInfo.Text = "Detected";
            }
            else
            {
                WheelStatusTextInfo.Text = "Not detected";
            }

        }
        private async void ToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            await logger.Log("ToggleBtn_Click method started");
            isRunning = !isRunning;

            if (isRunning)
            {
                // Start the input translation and update the UI accordingly.
                ToggleBtn.Content = "Stop";
                StatusTextInfo.Text = "Running";
                await task.Start();
                CheckDevicesStatus();
            }
            else
            {
                // Stop the input translation and update the UI accordingly.
                ToggleBtn.Content = "Start";
                StatusTextInfo.Text = "Not running";
                wheelInputTask.Stop();  // Here, you are calling the Stop method
                CheckDevicesStatus();
            }

        }

        private void InputHandler_WheelChanged(object sender, WheelChangedEventArgs e)
        {
            // Here, you can update your UI based on the new readings
            // Be careful to use Dispatcher if you're updating the UI from a non-UI thread
            ThrottleProgress.Value = e.ThrottleValue;
            WheelProgress.Value = e.WheelValue;
            BrakeProgress.Value = e.BrakeValue;

            // If the WheelChangedEventArgs also included button info, you could update it here:
            Button1Info.Text = e.Buttons.HasFlag(RacingWheelButtons.Button1).ToString();
            Button2Info.Text = e.Buttons.HasFlag(RacingWheelButtons.Button2).ToString();
            Button3Info.Text = e.Buttons.HasFlag(RacingWheelButtons.Button3).ToString();
            Button4Info.Text = e.Buttons.HasFlag(RacingWheelButtons.Button4).ToString();
            Button5Info.Text = e.Buttons.HasFlag(RacingWheelButtons.Button5).ToString();
            Button6Info.Text = e.Buttons.HasFlag(RacingWheelButtons.Button6).ToString();
            TriggerLeftInfo.Text = e.Triggers.HasFlag(RacingWheelButtons.NextGear).ToString();
            TriggerRightInfo.Text = e.Triggers.HasFlag(RacingWheelButtons.PreviousGear).ToString();
            // ... repeat for each piece of device info
        }
    }
}