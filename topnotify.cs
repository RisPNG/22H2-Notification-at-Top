using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;

public class Program
{
    #region Native Windows API Declarations
    // Import the FindWindow function from user32.dll to find a window by class name or window title.
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    // Import the FindWindowEx function from user32.dll to find a child window within a parent window.
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

    // Import the ShowWindow function from user32.dll to control the visibility of a specified window.
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Import the SetWindowPos function from user32.dll to modify the position, size, and Z order of a window.
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    // Import the GetWindowRect function from user32.dll to get the size and position of a specified window's rectangle.
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, ref Rectangle rectangle);

    // Flags for modifying window behavior with SetWindowPos
    const short SWP_NOMOVE = 0X2;      // Do not change the window's position.
    const short SWP_NOSIZE = 1;        // Do not change the window's size.
    const short SWP_NOZORDER = 0X4;    // Retain the current Z order (stacking order) of the window.
    const int SWP_SHOWWINDOW = 0x0040; // Show the window if it's hidden.
    #endregion

    // Define constants for UI positioning and behavior to avoid hardcoding values throughout the code.
    const int OFFSET_TOP = 15;                  // The top offset for positioning the notification windows.
    const int OFFSET_RIGHT = 15;                // The right offset for positioning the notification windows.
    const int COOLDOWN_THRESHOLD = 30;          // Cooldown threshold to dismiss the notification after stutters.
    const int WINDOW_OFFSCREEN_X = -9999;       // X-coordinate to move the window off-screen.
    const int WINDOW_OFFSCREEN_Y = -9999;       // Y-coordinate to move the window off-screen.
    const int WINDOW_WIDTH = 100;               // Default window width (for positioning logic).

    // A cooldown mechanism to prevent dismissing notifications too quickly due to stutters.
    public static int Cooldown = COOLDOWN_THRESHOLD;

    /// <summary>
    /// The main entry point of the application. This method contains the main loop for handling both
    /// Microsoft Teams and Windows system notifications, adjusting their screen position accordingly.
    /// </summary>
    public static void Main(string[] args)
    {
        // Continuous loop to check for notifications and reposition them if necessary.
        while (true)
        {
            HandleTeamsNotification();    // Handle Microsoft Teams notifications.
            HandleWindowsNotification();  // Handle Windows system notifications.
            Thread.Sleep(50);             // Adjust sleep duration to control CPU usage.
        }
    }

    /// <summary>
    /// Handles the detection and positioning of Microsoft Teams notifications on the screen.
    /// This method checks if a Teams notification is visible and repositions it based on the executable name.
    /// </summary>
    private static void HandleTeamsNotification()
    {
        try
        {
            // Find the Microsoft Teams notification window using its Chrome widget class name.
            var teamsHwnd = FindWindow("Chrome_WidgetWin_1", "Microsoft Teams Notification");

            // Find the Chrome render widget host for the Teams notification (MS Teams likely uses a Chrome WebView).
            var chromeHwnd = FindWindowEx(teamsHwnd, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", "Chrome Legacy Window");

            // If the notification window is found (i.e., notification is showing up)
            if (chromeHwnd != IntPtr.Zero)
            {
                Cooldown = 0;  // Reset cooldown since a notification is active.

                // Position the notification based on the executable name (topleft.exe or topright.exe).
                HandleNotificationPosition(teamsHwnd);
            }
            else
            {
                // If no notification is showing, start a cooldown before dismissing off-screen.
                if (Cooldown >= COOLDOWN_THRESHOLD)
                {
                    // Move the notification window off-screen to dismiss it.
                    SetNotificationPosition(teamsHwnd, WINDOW_OFFSCREEN_X, WINDOW_OFFSCREEN_Y);
                    Cooldown = 0;  // Reset cooldown.
                }
                Cooldown += 1; // Increment cooldown counter on each loop iteration.
            }
        }
        catch (Exception ex)
        {
            // Log any errors encountered while handling Teams notifications.
            Console.WriteLine("Error occurred while handling Teams notification: " + ex.Message);
        }
    }

    /// <summary>
    /// Handles the detection and positioning of Windows system notifications on the screen.
    /// This method adjusts the notification window based on the current executable's position setting.
    /// </summary>
    private static void HandleWindowsNotification()
    {
        try
        {
            // Find the Windows system notification window using its class name.
            var hwnd = FindWindow("Windows.UI.Core.CoreWindow", "New notification");

            // Position the notification based on the executable name (topleft.exe or topright.exe).
            HandleNotificationPosition(hwnd);
        }
        catch (Exception ex)
        {
            // Log any errors encountered while handling Windows notifications.
            Console.WriteLine("Error occurred while handling Windows notification: " + ex.Message);
        }
    }

    /// <summary>
    /// Enum to manage the positioning of notifications (either top-left, top-right, bottom-left, middle positions, etc.)
    /// </summary>
    public enum NotificationPosition { TopLeft, TopRight, BottomLeft, BottomRight, MiddleLeft, MiddleRight, TopMiddle, BottomMiddle }

    /// <summary>
    /// Determines the position for the notification based on the executable name.
    /// This is used to toggle between different screen corners (left or right).
    /// </summary>
    /// <returns>Returns the notification position (TopLeft, TopRight, BottomLeft, MiddleRight, etc.).</returns>
    static NotificationPosition GetNotificationPosition()
    {
        // Use the executable name to determine the position.
        switch (System.AppDomain.CurrentDomain.FriendlyName)
        {
            case "topleft.exe": return NotificationPosition.TopLeft;
            case "topright.exe": return NotificationPosition.TopRight;
            case "bottomleft.exe": return NotificationPosition.BottomLeft;
            case "bottomright.exe": return NotificationPosition.BottomRight;
            case "middleleft.exe": return NotificationPosition.MiddleLeft;
            case "middleright.exe": return NotificationPosition.MiddleRight;
            case "topmiddle.exe": return NotificationPosition.TopMiddle;
            case "bottommiddle.exe": return NotificationPosition.BottomMiddle;
            default: return NotificationPosition.TopLeft; // Default to TopLeft
        }
    }

    /// <summary>
    /// Sets the position of the notification window using SetWindowPos API.
    /// The position is determined based on the screen coordinates and the NotificationPosition enum.
    /// </summary>
    /// <param name="hwnd">Handle of the notification window.</param>
    private static void HandleNotificationPosition(IntPtr hwnd)
    {
        // Get current window rectangle to calculate its size
        Rectangle notifyRect = new Rectangle();
        GetWindowRect(hwnd, ref notifyRect);

        int width = notifyRect.Width - notifyRect.X;
        int height = notifyRect.Height - notifyRect.Y;

        // Determine position based on NotificationPosition enum
        switch (GetNotificationPosition())
        {
            case NotificationPosition.TopLeft:
                SetNotificationPosition(hwnd, OFFSET_TOP, OFFSET_TOP);
                break;

            case NotificationPosition.TopRight:
                SetNotificationPosition(hwnd, Screen.PrimaryScreen.Bounds.Width - width - OFFSET_RIGHT, OFFSET_TOP);
                break;

            case NotificationPosition.BottomLeft:
                SetNotificationPosition(hwnd, OFFSET_TOP, Screen.PrimaryScreen.Bounds.Height - height - OFFSET_TOP);
                break;

            case NotificationPosition.BottomRight:
                SetNotificationPosition(hwnd, Screen.PrimaryScreen.Bounds.Width - width - OFFSET_RIGHT, Screen.PrimaryScreen.Bounds.Height - height - OFFSET_TOP);
                break;

            case NotificationPosition.MiddleLeft:
                SetNotificationPosition(hwnd, OFFSET_TOP, (Screen.PrimaryScreen.Bounds.Height / 2) - (height / 2));
                break;

            case NotificationPosition.MiddleRight:
                SetNotificationPosition(hwnd, Screen.PrimaryScreen.Bounds.Width - width - OFFSET_RIGHT, (Screen.PrimaryScreen.Bounds.Height / 2) - (height / 2));
                break;

            case NotificationPosition.TopMiddle:
                SetNotificationPosition(hwnd, (Screen.PrimaryScreen.Bounds.Width / 2) - (width / 2), OFFSET_TOP);
                break;

            case NotificationPosition.BottomMiddle:
                SetNotificationPosition(hwnd, (Screen.PrimaryScreen.Bounds.Width / 2) - (width / 2), Screen.PrimaryScreen.Bounds.Height - height - OFFSET_TOP);
                break;
        }
    }

    /// <summary>
    /// Sets the position of the notification window using SetWindowPos API.
    /// </summary>
    /// <param name="hwnd">Handle of the notification window.</param>
    /// <param name="x">X-coordinate of the window position.</param>
    /// <param name="y">Y-coordinate of the window position.</param>
    private static void SetNotificationPosition(IntPtr hwnd, int x, int y)
    {
        // Call the SetWindowPos API to update the notification window's position.
        SetWindowPos(hwnd, 0, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
    }
}
