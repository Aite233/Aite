using System.Windows;

namespace Aite.WPF
{
    public partial class CustomMessageBoxWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public CustomMessageBoxWindow()
        {
            InitializeComponent();
        }

        public void SetContent(string message, string title, MessageBoxButton button)
        {
            TitleText.Text = title;
            MessageText.Text = message;

            if (button == MessageBoxButton.OK)
            {
                CancelButton.Visibility = Visibility.Collapsed;
                ButtonGrid.ColumnDefinitions[1].Width = new GridLength(0);
                OkButton.Content = "确定";
            }
            else if (button == MessageBoxButton.OKCancel)
            {
                CancelButton.Visibility = Visibility.Visible;
                ButtonGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                OkButton.Content = "确定";
                CancelButton.Content = "取消";
            }
            else if (button == MessageBoxButton.YesNo)
            {
                CancelButton.Visibility = Visibility.Visible;
                ButtonGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                OkButton.Content = "是";
                CancelButton.Content = "否";
            }

            OkButton.Click += (sender, e) =>
            {
                Result = MessageBoxResult.OK;
                Close();
            };

            CancelButton.Click += (sender, e) =>
            {
                Result = MessageBoxResult.Cancel;
                Close();
            };
        }
    }
}
