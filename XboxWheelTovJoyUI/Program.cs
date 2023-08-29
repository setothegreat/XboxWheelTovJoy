#nullable enable

using System;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Gaming.Input;
using vJoyInterfaceWrap;
using System.Threading.Tasks;
using XboxWheelCompatibility.WheelTransformer;
using Windows.UI.Core;
using Windows.UI.Text.Core;
using XboxWheelTovJoyUI;

public class WheelInputTask
{
    public static event Action<string>? LogMessage;
    public static vJoy? joystick;
    private static uint id = 1; // vJoy ID
    private static CancellationTokenSource? cancellationTokenSource;
    private bool Started = false;
    public event Action? WheelDisconnected;

    public Task Start()
    {
        try
        {
            if (Started) return Task.CompletedTask;

            cancellationTokenSource = new CancellationTokenSource();
            LogMessage?.Invoke("Button Clicked");
            LifecycleManager.Start();
            WheelManager.Initialize();


            Task.Run(() => RunWheelManager(cancellationTokenSource.Token));
            Task.Run(() => Run(cancellationTokenSource.Token));
            Task.Run(() => HandleInputAsync(cancellationTokenSource.Token));
            Started = true;

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"An error occurred: {ex.Message}");
            return Task.FromException(ex);
        }
    }
    public void Stop()
    {
        Started = false;
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        LifecycleManager.Stop();
        LogMessage?.Invoke("Button Clicked");
    }


    public void Run(CancellationToken cancellationToken)
    {
        try
        {
            // Initialize vJoy
            joystick = new vJoy();

            if (!joystick.vJoyEnabled())
            {
                LogMessage?.Invoke("vJoy is not enabled. Exiting...");
                return;
            }

            LogMessage?.Invoke("vJoy is enabled.");

            if (!joystick.AcquireVJD(id))
            {
                LogMessage?.Invoke($"Failed to acquire vJoy device with ID {id}. Exiting...");
                return;
            }

            LogMessage?.Invoke($"Successfully acquired vJoy device with ID {id}.");

        }
        catch (Exception)
        {
            cancellationTokenSource?.Cancel();
            throw;
        }
    }
    private void CheckWheelConnection()
    {
        RacingWheel? wheel = WheelManager.MainWheel;
        if (wheel == null)
        {
            WheelDisconnected?.Invoke();
        }
        else
            LogMessage?.Invoke("Racing wheel detected.");
    }

    public async Task RunWheelManager(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && Started)
        {
            LifecycleManager.Tick?.Invoke(null, EventArgs.Empty);
            await Task.Delay(1000);  // adjust delay as necessary
        }
    }

    public void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
    {
        cancellationTokenSource?.Cancel();
    }

    public class WheelChangedEventArgs : EventArgs
    {
        public double ThrottleValue { get; set; }
        public double WheelValue { get; set; }
        public double BrakeValue { get; set; }
        public RacingWheelButtons Buttons { get; set; }
        public RacingWheelButtons DPad { get; set; }
        public RacingWheelButtons Triggers { get; set; }
    }

    public event EventHandler<WheelChangedEventArgs> WheelChanged = delegate { };

    private async Task HandleInputAsync(CancellationToken cancellationToken)
    {
        LogMessage?.Invoke("Entering HandleInputAsync");
        while (!cancellationToken.IsCancellationRequested && Started)
        {
            CheckWheelConnection();
            RacingWheel? wheel = WheelManager.MainWheel;

            if (wheel == null)
            {
                LogMessage?.Invoke("No racing wheel detected.");
                WheelDisconnected?.Invoke();
                await Task.Delay(1000, cancellationToken); // Wait before checking again
                continue;
            }

            if (wheel != null)
            {
                RacingWheelReading reading = wheel.GetCurrentReading(); ;

                // Map wheel value to vJoy X-axis
                long vJoyX = (long)((reading.Wheel + 1) * 0x4000); // Normalize to vJoy range
                joystick?.SetAxis((int)vJoyX, id, HID_USAGES.HID_USAGE_X);

                // Map throttle and brake values to vJoy Y and Z axes
                long vJoyY = (long)(reading.Throttle * 0x8000); // Normalize to vJoy range
                long vJoyZ = (long)(reading.Brake * 0x8000); // Normalize to vJoy range
                joystick?.SetAxis((int)vJoyY, id, HID_USAGES.HID_USAGE_Y);
                joystick?.SetAxis((int)vJoyZ, id, HID_USAGES.HID_USAGE_Z);

                // Map buttons
                bool isPressed = reading.Buttons.HasFlag(RacingWheelButtons.Button1);
                LogMessage?.Invoke($"{isPressed}");
                joystick?.SetBtn(isPressed, id, 1);
                isPressed = reading.Buttons.HasFlag(RacingWheelButtons.Button2);
                joystick?.SetBtn(isPressed, id, 2);
                isPressed = reading.Buttons.HasFlag(RacingWheelButtons.Button3);
                joystick?.SetBtn(isPressed, id, 3);
                isPressed = reading.Buttons.HasFlag(RacingWheelButtons.Button4);
                joystick?.SetBtn(isPressed, id, 4);
                isPressed = reading.Buttons.HasFlag(RacingWheelButtons.Button5);
                joystick?.SetBtn(isPressed, id, 5);
                isPressed = reading.Buttons.HasFlag(RacingWheelButtons.Button6);
                joystick?.SetBtn(isPressed, id, 6);

                // Map D-Pad as POV
                int povDirection;

                if (reading.Buttons.HasFlag(RacingWheelButtons.DPadUp))
                {
                    povDirection = 0; // up
                }
                else if (reading.Buttons.HasFlag(RacingWheelButtons.DPadRight))
                {
                    povDirection = 9000; // right
                }
                else if (reading.Buttons.HasFlag(RacingWheelButtons.DPadDown))
                {
                    povDirection = 18000; // down
                }
                else if (reading.Buttons.HasFlag(RacingWheelButtons.DPadLeft))
                {
                    povDirection = 27000; // left
                }
                else
                {
                    povDirection = -1; // no direction
                }

                joystick?.SetContPov(povDirection, id, 1); // assuming 1 for POV number


                // Map triggers as buttons
                isPressed = reading.Buttons.HasFlag(RacingWheelButtons.NextGear);
                joystick?.SetBtn(isPressed, id, 7);

                isPressed = reading.Buttons.HasFlag(RacingWheelButtons.PreviousGear);
                joystick?.SetBtn(isPressed, id, 8);


                // Fire the event with the new readings
                WheelChanged?.Invoke(this, new WheelChangedEventArgs
                {
                    ThrottleValue = reading.Throttle,
                    WheelValue = (reading.Wheel + 1) / 2, // Normalize to 0-1 range
                    BrakeValue = reading.Brake,
                    Buttons = reading.Buttons,
                    DPad = reading.Buttons,
                    Triggers = reading.Buttons
                });

                await Task.Delay(7, cancellationToken); // About 144 FPS
            }
            else
            {
                // Fire the event with default values indicating no wheel is connected
                LogMessage?.Invoke("No racing wheel detected.");
                WheelDisconnected?.Invoke();
                await Task.Delay(1000, cancellationToken); // Wait before checking again
                continue;
            }
        }
        LogMessage?.Invoke("Exiting HandleInputAsync");
    }
}
