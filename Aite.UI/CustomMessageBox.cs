using System.Windows;

namespace Aite.WPF
{
    public class CustomMessageBox
    {
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            var window = new CustomMessageBoxWindow();
            window.SetContent(message, title, button);

            window.TitleText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            window.MessageText.Measure(new Size(440, double.PositiveInfinity));

            double contentWidth = Math.Max(window.MessageText.DesiredSize.Width, window.TitleText.DesiredSize.Width) + 40;
            double contentHeight = window.TitleText.DesiredSize.Height + window.MessageText.DesiredSize.Height + 100;

            double maxWidth = 480;
            double minWidth = 320;
            double minHeight = 140;
            double maxHeight = 400;

            window.Width = Math.Clamp(contentWidth, minWidth, maxWidth);
            window.Height = Math.Clamp(contentHeight, minHeight, maxHeight);

            window.ShowDialog();

            return window.Result;
        }

        public static void Show(string message, string title)
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static void Show(string message)
        {
            Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.None);
        }
    }
}
