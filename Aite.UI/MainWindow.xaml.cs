﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aite.Core;
using Aite.Config.Entities.Login;
using Aite.Core.Message;
using Aite.Core.Utils;
using Aite.IRC;
using Aite.Config.Utils;
using Serilog;
using System.Threading;

namespace Aite.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
    {
        // 互斥锁，用于防止程序多开
        private static Mutex mutex = new Mutex(true, "AiteApplicationMutex");
        
        private Grid? HomePage;
        private Grid? AccountsPage;
        private Grid? ServersPage;
        private Grid? LogsPage;
        private TextBlock? LogOutput;
        private ListView? _gamesList;
        private ListView? _proxiesList;
        private ListView? _accountsList;
        
        // 保存代理信息的类
        private class ProxyInfo
        {
            public string? ServerId { get; set; }
            public string? RoleName { get; set; }
            public string? ServerName { get; set; }
            public string? Port { get; set; }
        }
        
        // 保存当前运行的代理信息
        private List<ProxyInfo> _runningProxies = new List<ProxyInfo>();
        
        // 记录是否已经登录过（用于控制只在第一次登录时刷新服务器列表）
        private bool hasLoggedIn = false;
    
    // 公告数据类
    private class AnnouncementData
    {
        public long date { get; set; }
        public string? msg { get; set; }
    }
    
    // 生成随机名称的方法
    private string GenerateRandomName()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var randomChars = new char[7];
        for (int i = 0; i < 7; i++)
        {
            randomChars[i] = chars[random.Next(chars.Length)];
        }
        return "Aite_" + new string(randomChars);
    }
    
    // 移除HTML标签的方法
    private string RemoveHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;
        
        // 直接移除所有HTML标签
        return System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]*>", string.Empty);
    }
    
    // 日志输出方法
        private void AddLog(string message, bool isSystemEvent = true)
        {
            // 输出到控制台
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // 只记录系统事件，忽略导航等次要事件
            if (isSystemEvent)
            {
                Dispatcher.Invoke(() => {
                    if (LogOutput != null) {
                        LogOutput.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
                        // 滚动到底部
                        if (LogOutput.Parent is ScrollViewer scrollViewer)
                        {
                            scrollViewer.ScrollToBottom();
                        }
                    }
                });
            }
        }

        // 登录方法
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建登录对话框
                var dialog = new Window
                {
                    Title = "登录",
                    Width = 500,
                    Height = 550,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"))
                };

                // 创建对话框内容
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                // 登录方式选择
                var loginTypeLabel = new Label { Content = "登录方式:", Margin = new Thickness(20, 20, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold };
                Grid.SetRow(loginTypeLabel, 0);

                var loginTypeGrid = new Grid { Margin = new Thickness(20, 5, 20, 10) };
                loginTypeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                loginTypeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                loginTypeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetRow(loginTypeGrid, 1);

                var sauthRadio = new RadioButton { Content = "Sauth", GroupName = "LoginType", IsChecked = true, Margin = new Thickness(5) };
                Grid.SetColumn(sauthRadio, 0);

                var com4399Radio = new RadioButton { Content = "4399Com", GroupName = "LoginType", Margin = new Thickness(5) };
                Grid.SetColumn(com4399Radio, 1);

                var email163Radio = new RadioButton { Content = "163Email", GroupName = "LoginType", Margin = new Thickness(5) };
                Grid.SetColumn(email163Radio, 2);

                loginTypeGrid.Children.Add(sauthRadio);
                loginTypeGrid.Children.Add(com4399Radio);
                loginTypeGrid.Children.Add(email163Radio);

                // 账号名称
                var nameLabel = new Label { Content = "名称:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold };
                Grid.SetRow(nameLabel, 2);
                var nameTextBox = new TextBox { Margin = new Thickness(20, 5, 20, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
                Grid.SetRow(nameTextBox, 3);

                // Sauth登录 (单参数)
            var sauthLabel = new Label { Content = "Sauth:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold };
                Grid.SetRow(sauthLabel, 4);
                var sauthTextBox = new TextBox { Margin = new Thickness(20, 5, 20, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), Height = 100, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                Grid.SetRow(sauthTextBox, 5);

                // 账号密码登录 (双参数)
                var accountLabel = new Label { Content = "账号:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold, Visibility = Visibility.Collapsed };
                Grid.SetRow(accountLabel, 4);
                var accountTextBox = new TextBox { Margin = new Thickness(20, 5, 20, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), Visibility = Visibility.Collapsed };
                Grid.SetRow(accountTextBox, 5);

                var passwordLabel = new Label { Content = "密码:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold, Visibility = Visibility.Collapsed };
                Grid.SetRow(passwordLabel, 6);
                var passwordTextBox = new PasswordBox { Margin = new Thickness(20, 5, 20, 10), Background = Brushes.Transparent, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), Visibility = Visibility.Collapsed };
                Grid.SetRow(passwordTextBox, 7);

                // 验证码 (4399和4399com登录需要)
                var captchaLabel = new Label { Content = "验证码:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold, Visibility = Visibility.Collapsed };
                Grid.SetRow(captchaLabel, 8);
                var captchaGrid = new Grid { Margin = new Thickness(20, 5, 20, 10), Visibility = Visibility.Collapsed };
                captchaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                captchaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                Grid.SetRow(captchaGrid, 9);

                var captchaTextBox = new TextBox { Margin = new Thickness(0, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
                Grid.SetColumn(captchaTextBox, 0);

                var captchaImage = new Image { Width = 100, Height = 40, Margin = new Thickness(10, 0, 0, 0), Stretch = Stretch.UniformToFill };
                Grid.SetColumn(captchaImage, 1);

                captchaGrid.Children.Add(captchaTextBox);
                captchaGrid.Children.Add(captchaImage);

                // 按钮
                var buttonGrid = new Grid { Margin = new Thickness(20, 20, 20, 20) };
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetRow(buttonGrid, 10);

                var cancelButton = new Button { Content = "取消", Margin = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
                cancelButton.Click += (s, args) => dialog.Close();
                Grid.SetColumn(cancelButton, 0);

                var loginButton = new Button { Content = "登录", Margin = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };

                // 登录方式切换事件
                sauthRadio.Checked += (s, args) => {
                    sauthLabel.Visibility = Visibility.Visible;
                    sauthTextBox.Visibility = Visibility.Visible;
                    accountLabel.Visibility = Visibility.Collapsed;
                    accountTextBox.Visibility = Visibility.Collapsed;
                    passwordLabel.Visibility = Visibility.Collapsed;
                    passwordTextBox.Visibility = Visibility.Collapsed;
                    nameTextBox.IsEnabled = true;
                };

                com4399Radio.Checked += (s, args) => {
                    sauthLabel.Visibility = Visibility.Collapsed;
                    sauthTextBox.Visibility = Visibility.Collapsed;
                    accountLabel.Visibility = Visibility.Visible;
                    accountTextBox.Visibility = Visibility.Visible;
                    passwordLabel.Visibility = Visibility.Visible;
                    passwordTextBox.Visibility = Visibility.Visible;
                    captchaLabel.Visibility = Visibility.Visible;
                    captchaGrid.Visibility = Visibility.Visible;
                    nameTextBox.IsEnabled = false;
                    nameTextBox.Text = "此项是多余的";
                    if (!string.IsNullOrEmpty(accountTextBox.Text)) {
                        nameTextBox.Text = accountTextBox.Text;
                    }
                    // 更新并显示验证码
                    UpdateCaptchaImage(captchaImage);
                };

                email163Radio.Checked += (s, args) => {
                    sauthLabel.Visibility = Visibility.Collapsed;
                    sauthTextBox.Visibility = Visibility.Collapsed;
                    accountLabel.Visibility = Visibility.Visible;
                    accountTextBox.Visibility = Visibility.Visible;
                    passwordLabel.Visibility = Visibility.Visible;
                    passwordTextBox.Visibility = Visibility.Visible;
                    captchaLabel.Visibility = Visibility.Collapsed;
                    captchaGrid.Visibility = Visibility.Collapsed;
                    nameTextBox.IsEnabled = false;
                    nameTextBox.Text = "此项是多余的";
                    if (!string.IsNullOrEmpty(accountTextBox.Text)) {
                        nameTextBox.Text = accountTextBox.Text;
                    }
                };

                sauthRadio.Checked += (s, args) => {
                    sauthLabel.Visibility = Visibility.Visible;
                    sauthTextBox.Visibility = Visibility.Visible;
                    accountLabel.Visibility = Visibility.Collapsed;
                    accountTextBox.Visibility = Visibility.Collapsed;
                    passwordLabel.Visibility = Visibility.Collapsed;
                    passwordTextBox.Visibility = Visibility.Collapsed;
                    captchaLabel.Visibility = Visibility.Collapsed;
                    captchaGrid.Visibility = Visibility.Collapsed;
                    nameTextBox.IsEnabled = true;
                };

                // 账号输入变化时，自动更新名称
                accountTextBox.TextChanged += (s, args) => {
                    if ((com4399Radio.IsChecked == true || email163Radio.IsChecked == true) && !string.IsNullOrEmpty(accountTextBox.Text)) {
                        nameTextBox.Text = accountTextBox.Text;
                    }
                };

                // 验证码图片点击事件，用于刷新验证码
                captchaImage.MouseLeftButtonDown += (s, args) => {
                    UpdateCaptchaImage(captchaImage);
                };

                // 更新验证码图片的方法
                void UpdateCaptchaImage(Image image) {
                    try {
                        // 更新验证码
                        Aite.Core.Message.AccountMessage.UpdateCaptcha();
                        if (Aite.Core.Message.AccountMessage.Captcha4399Bytes != null) {
                            // 将字节数组转换为BitmapImage
                            var bitmap = new BitmapImage();
                            using (var stream = new MemoryStream(Aite.Core.Message.AccountMessage.Captcha4399Bytes)) {
                                bitmap.BeginInit();
                                bitmap.StreamSource = stream;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                            }
                            image.Source = bitmap;
                        }
                    } catch (Exception ex) {
                        AddLog($"更新验证码失败: {ex.Message}");
                    }
                }

                loginButton.Click += async (s, args) => {
                    try
                    {
                        // 显示登录中状态
                        loginButton.Content = "登录中...";
                        loginButton.IsEnabled = false;

                        // 创建账号实体
                        Aite.Config.Entities.Login.EntityAccount account;

                        // 根据选择的登录方式创建不同的账号实体
                        if (sauthRadio.IsChecked == true)
                        {
                            account = new Aite.Config.Entities.Login.EntityAccount
                            {
                                Type = "cookie",
                                Account = nameTextBox.Text,
                                Password = sauthTextBox.Text
                            };
                        }
                        else if (com4399Radio.IsChecked == true)
                        {
                            account = new Aite.Config.Entities.Login.EntityAccount
                            {
                                Type = "4399com",
                                Account = accountTextBox.Text,
                                Password = passwordTextBox.Password
                            };
                        }
                        else if (email163Radio.IsChecked == true)
                        {
                            account = new Aite.Config.Entities.Login.EntityAccount
                            {
                                Type = "163Email",
                                Account = accountTextBox.Text,
                                Password = passwordTextBox.Password
                            };
                        }
                        else
                        {
                            // 默认使用Sauth登录
                            account = new Aite.Config.Entities.Login.EntityAccount
                            {
                                Type = "cookie",
                                Account = nameTextBox.Text,
                                Password = sauthTextBox.Text
                            };
                        }

                        // 尝试登录
                        var result = await Task.Run(() => {
                            try
                            {
                                // 调用登录API
                                AddLog($"尝试登录账号: {account.Account}");
                                
                                // 对于4399和4399com登录，需要设置用户输入的验证码
                                if (account.Type == "4399" || account.Type == "4399com")
                                {
                                    // 使用用户输入的验证码
                                    Aite.Core.Message.AccountMessage.Captcha4399 = captchaTextBox.Text;
                                    if (string.IsNullOrEmpty(Aite.Core.Message.AccountMessage.Captcha4399))
                                    {
                                        throw new Exception("验证码不能为空");
                                    }
                                    AddLog($"使用验证码: {Aite.Core.Message.AccountMessage.Captcha4399}");
                                }
                                
                                Aite.Core.Message.AccountMessage.Login(account);
                                AddLog($"登录账号 {account.Account} 成功");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                AddLog($"登录失败: {ex.Message}");
                                // 登录失败时刷新验证码
                                if (account.Type == "4399" || account.Type == "4399com")
                                {
                                    Dispatcher.Invoke(() => {
                                        UpdateCaptchaImage(captchaImage);
                                        captchaTextBox.Text = "";
                                    });
                                }
                                return false;
                            }
                        });

                        if (result)
                        {
                            // 登录成功
                            AddLog($"登录成功: {account.Account}");
                            
                            // 第一次登录后自动刷新服务器列表
                            if (!hasLoggedIn)
                            {
                                AddLog("首次登录，自动刷新服务器列表...");
                                _ = Task.Run(async () => {
                                    await RefreshServersList();
                                    hasLoggedIn = true;
                                });
                            }
                            
                            dialog.Close();
                        }
                        else
                        {
                            // 登录失败
                            CustomMessageBox.Show("登录失败，请检查账号和密码", "错误");
                            loginButton.Content = "登录";
                            loginButton.IsEnabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"登录错误: {ex.Message}", "错误");
                        loginButton.Content = "登录";
                        loginButton.IsEnabled = true;
                    }
                };
                Grid.SetColumn(loginButton, 1);

                buttonGrid.Children.Add(cancelButton);
                buttonGrid.Children.Add(loginButton);

                grid.Children.Add(loginTypeLabel);
                grid.Children.Add(loginTypeGrid);
                grid.Children.Add(nameLabel);
                grid.Children.Add(nameTextBox);
                grid.Children.Add(sauthLabel);
                grid.Children.Add(sauthTextBox);
                grid.Children.Add(accountLabel);
                grid.Children.Add(accountTextBox);
                grid.Children.Add(passwordLabel);
                grid.Children.Add(passwordTextBox);
                grid.Children.Add(captchaLabel);
                grid.Children.Add(captchaGrid);
                grid.Children.Add(buttonGrid);

                dialog.Content = grid;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"打开登录对话框失败: {ex.Message}", "错误");
                AddLog($"打开登录对话框失败: {ex.Message}");
            }
        }

    public MainWindow()
    {
        try
        {
            // 检查是否已经有实例在运行
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                CustomMessageBox.Show("程序已经在运行中", "提示");
                Application.Current.Shutdown();
                return;
            }
            

            
            InitializeComponent();
            NavigationList.SelectionChanged += NavigationList_SelectionChanged;
            CloseButton.Click += CloseButton_Click;
            
            // 登录点击事件已移除，因为UserAccount控件已被替换为主题切换按钮
            
            // 初始化页面
            InitializePages();
            
            // 默认选择首页
            NavigationList.SelectedIndex = 0;
            
            // 窗口加载后进行初始化
            Loaded += MainWindow_Loaded;
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"初始化错误: {ex.Message}\n\n{ex.StackTrace}", "错误");
        }
    }
    
    // 顶部栏拖动事件
    private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
    
    // 更新文本块颜色
    private void UpdateTextBlocks(Panel panel, string textColor, string cardTextColor)
    {
        foreach (var child in panel.Children)
        {
            if (child is TextBlock textBlock)
            {
                // 根据文本内容判断使用哪种颜色
                if (textBlock.FontWeight == FontWeights.Bold || textBlock.FontWeight == FontWeights.SemiBold)
                {
                    textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textColor));
                }
                else
                {
                    textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cardTextColor));
                }
            }
            else if (child is Panel childPanel)
            {
                UpdateTextBlocks(childPanel, textColor, cardTextColor);
            }
        }
    }
    
    // 窗口加载后进行初始化
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 注册编码提供程序以支持中文编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // 初始化日志
            InitProgram.LogoInit();
            
            // 检查是否已经激活
            if (CheckActivationStatus())
            {
/*                 // 激活成功，获取 CRC salt
                try
                {
                    // 检查X19.CrcSalt是否已经设置
                    if (!string.IsNullOrEmpty(WPFLauncherApi.Protocol.X19.CrcSalt))
                    {
                        AddLog("CRC salt已设置");
                    }
                    else
                    {
                        CustomMessageBox.Show($"CRC salt未设置", "错误");
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"检查 CRC salt 失败: {ex.Message}");
                    AddLog($"错误详情: {ex.StackTrace}");
                    CustomMessageBox.Show($"检查 CRC salt 失败: {ex.Message}\n\n{ex.StackTrace}", "错误");
                    Environment.Exit(0);
                } */
                
                // 初始化程序
                try
                {
                    await Task.Run(() => InitProgram.NelInit(new string[] {}, () => InitProgram.LogoInit()));
                    await Task.Run(() => InitProgram.NelInit1());
                    IrcEventHandler.Register();
                    AddLog("Aite.Core初始化完成");
                }
                catch (Exception ex)
                {
                    AddLog($"初始化网络请求失败: {ex.Message}");
                    AddLog("继续启动应用程序...");
                }
                
                // 禁用默认自动登录
                //AccountMessage.DisableDefaultLogin();
                //AddLog("已禁用默认自动登录");
            }
            else
            {
                AddLog("未激活，退出程序");
                CustomMessageBox.Show($"未激活，退出程序", "错误");
                Environment.Exit(0);
            }
            
            // 自动刷新账号列表
            await RefreshAccountsListAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Aite.Core初始化错误: {ex.Message}");
            AddLog($"Aite.Core错误详情: {ex.StackTrace}");
            CustomMessageBox.Show($"Aite.Core初始化错误: {ex.Message}\n\n{ex.StackTrace}", "错误");
            Environment.Exit(0);
        }
    }
    
    // 检查激活状态
    private bool CheckActivationStatus()
    {
        try
        {
            // 检查X19.CrcSalt是否已经设置
            return !string.IsNullOrEmpty(WPFLauncherApi.Protocol.X19.CrcSalt);
        }
        catch (Exception)
        {
            // 忽略异常，返回未激活状态
        }
        return false;
    }

    private void InitializePages()
    {
        try
        {
            // 主页
            HomePage = new Grid();
            
            // 添加行定义
            HomePage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            HomePage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 公告功能
            var announcementBorder = new Border { Margin = new Thickness(20), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E6")) };
            // 设置边框样式
            announcementBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            announcementBorder.BorderThickness = new Thickness(1);
            announcementBorder.CornerRadius = new CornerRadius(12);
            announcementBorder.Padding = new Thickness(20);
            
            // 创建公告内容控件
            var announcementTitle = new StackPanel { Orientation = Orientation.Horizontal, Children = {
                new TextBlock { Text = "📢", FontSize = 18, Margin = new Thickness(0, 0, 8, 0) },
                new TextBlock { Text = "公告", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, -4, 0, 0), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) }
            }};
            
            var announcementContent = new TextBlock { 
                Text = "加载中...", 
                FontSize = 14, 
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), 
                Margin = new Thickness(0, 8, 0, 0), 
                TextWrapping = TextWrapping.Wrap 
            };
            
            var announcementDate = new TextBlock { 
                Text = "发布时间: 加载中...", 
                FontSize = 12, 
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), 
                Margin = new Thickness(0, 8, 0, 0) 
            };
            
            var announcementStack = new StackPanel {
                Children = {
                    announcementTitle,
                    announcementContent,
                    announcementDate
                }
            };
            
            announcementBorder.Child = announcementStack;
            Grid.SetRow(announcementBorder, 0);
            HomePage.Children.Add(announcementBorder);
            
            // 直接调用异步方法，不使用 Task.Run 包装
            _ = LoadAnnouncementAsync(announcementContent, announcementDate);

            // 主要功能区域
            var featuresBorder = new Border { Margin = new Thickness(20) };
            // 设置边框样式
            featuresBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            featuresBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            featuresBorder.BorderThickness = new Thickness(1);
            featuresBorder.CornerRadius = new CornerRadius(12);
            featuresBorder.Padding = new Thickness(20);
            var featuresStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "主要功能", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) }
                }
            };
            var featuresGrid = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 功能卡片 1
            var featureCard1 = new Border { Margin = new Thickness(8) };
            // 设置卡片样式
            featureCard1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            featureCard1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            featureCard1.BorderThickness = new Thickness(1);
            featureCard1.CornerRadius = new CornerRadius(12);
            featureCard1.Padding = new Thickness(20);
            featureCard1.Child = new StackPanel {
                Children = {
                    new TextBlock { Text = "👤", FontSize = 32, Margin = new Thickness(0, 0, 0, 12) },
                    new TextBlock { Text = "游戏账号管理", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) },
                    new TextBlock { Text = "轻松管理您的游戏账号，支持添加、编辑、删除账号信息。", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), TextWrapping = TextWrapping.Wrap }
                }
            };
            Grid.SetColumn(featureCard1, 0);
            featuresGrid.Children.Add(featureCard1);

            // 功能卡片 2
            var featureCard2 = new Border { Margin = new Thickness(8) };
            // 设置卡片样式
            featureCard2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            featureCard2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            featureCard2.BorderThickness = new Thickness(1);
            featureCard2.CornerRadius = new CornerRadius(12);
            featureCard2.Padding = new Thickness(20);
            featureCard2.Child = new StackPanel {
                Children = {
                    new TextBlock { Text = "🖥️", FontSize = 32, Margin = new Thickness(0, 0, 0, 12) },
                    new TextBlock { Text = "服务器管理", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) },
                    new TextBlock { Text = "管理您的游戏服务器，查看服务器信息，一键启动服务器。", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), TextWrapping = TextWrapping.Wrap }
                }
            };
            Grid.SetColumn(featureCard2, 1);
            featuresGrid.Children.Add(featureCard2);

            // 功能卡片 3
            var featureCard3 = new Border { Margin = new Thickness(8) };
            // 设置卡片样式
            featureCard3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            featureCard3.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            featureCard3.BorderThickness = new Thickness(1);
            featureCard3.CornerRadius = new CornerRadius(12);
            featureCard3.Padding = new Thickness(20);
            featureCard3.Child = new StackPanel {
                Children = {
                    new TextBlock { Text = "🧩", FontSize = 32, Margin = new Thickness(0, 0, 0, 12) },
                    new TextBlock { Text = "插件管理", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) },
                    new TextBlock { Text = "管理服务器插件，查看插件状态，支持启动和停止插件。", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), TextWrapping = TextWrapping.Wrap }
                }
            };
            Grid.SetColumn(featureCard3, 2);
            featuresGrid.Children.Add(featureCard3);

            // 功能卡片 4
            var featureCard4 = new Border { Margin = new Thickness(8) };
            // 设置卡片样式
            featureCard4.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            featureCard4.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            featureCard4.BorderThickness = new Thickness(1);
            featureCard4.CornerRadius = new CornerRadius(12);
            featureCard4.Padding = new Thickness(20);
            featureCard4.Child = new StackPanel {
                Children = {
                    new TextBlock { Text = "🛒", FontSize = 32, Margin = new Thickness(0, 0, 0, 12) },
                    new TextBlock { Text = "插件商城", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) },
                    new TextBlock { Text = "浏览和下载各种插件，丰富您的服务器功能。", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), TextWrapping = TextWrapping.Wrap }
                }
            };
            Grid.SetColumn(featureCard4, 3);
            featuresGrid.Children.Add(featureCard4);

            featuresStack.Children.Add(featuresGrid);
            featuresBorder.Child = featuresStack;
            Grid.SetRow(featuresBorder, 1);
            HomePage.Children.Add(featuresBorder);

            // 游戏账号管理页面
            AccountsPage = new Grid();
            AccountsPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // 账号管理区域
            var accountsBorder = new Border { Margin = new Thickness(20) };
            // 设置边框样式
            accountsBorder.Background = Brushes.Transparent;
            accountsBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            accountsBorder.BorderThickness = new Thickness(1);
            accountsBorder.CornerRadius = new CornerRadius(12);
            accountsBorder.Padding = new Thickness(20);
            var accountsStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "游戏账号管理", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 16), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) }
                }
            };
            
            var accountsHeader = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            accountsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            accountsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            accountsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            var accountsListText = new TextBlock { Text = "账号列表", FontSize = 16, FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(accountsListText, 0);
            accountsHeader.Children.Add(accountsListText);

            var addAccountButton = new Button { Content = "添加账号", Margin = new Thickness(0, 0, 0, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), Foreground = Brushes.White, BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Right };
            addAccountButton.Click += AddAccountButton_Click;
            Grid.SetColumn(addAccountButton, 1);
            accountsHeader.Children.Add(addAccountButton);

            var refreshAccountsButton = new Button { Content = "刷新", Margin = new Thickness(8, 0, 0, 0), Background = Brushes.LightGray, Foreground = Brushes.Black, BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Left };
            refreshAccountsButton.Click += async (sender, e) => {
                try
                {
                    refreshAccountsButton.Content = "刷新中...";
                    refreshAccountsButton.IsEnabled = false;
                    
                    // 延迟刷新，模拟网络请求
                    await Task.Delay(500);
                    
                    RefreshAccountsList();
                    AddLog("账号列表已刷新");
                }
                catch (Exception ex)
                {
                    AddLog($"刷新账号列表失败: {ex.Message}");
                }
                finally
                {
                    refreshAccountsButton.Content = "刷新";
                    refreshAccountsButton.IsEnabled = true;
                }
            };
            Grid.SetColumn(refreshAccountsButton, 2);
            accountsHeader.Children.Add(refreshAccountsButton);

            accountsStack.Children.Add(accountsHeader);

            // 添加列头
            var headerBorder = new Border { Margin = new Thickness(0, 16, 0, 0), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) };
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 使用星号宽度，确保操作列占据剩余空间
            
            // 列头文本
            var accountHeader = new TextBlock { Text = "账号", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
            Grid.SetColumn(accountHeader, 0);
            headerGrid.Children.Add(accountHeader);
            
            var userIdHeader = new TextBlock { Text = "User ID", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
            Grid.SetColumn(userIdHeader, 1);
            headerGrid.Children.Add(userIdHeader);
            
            var typeHeader = new TextBlock { Text = "类型", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
            Grid.SetColumn(typeHeader, 2);
            headerGrid.Children.Add(typeHeader);
            
            var statusHeader = new TextBlock { Text = "状态", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
            Grid.SetColumn(statusHeader, 3);
            headerGrid.Children.Add(statusHeader);
            
            var actionHeader = new TextBlock { Text = "操作", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
            Grid.SetColumn(actionHeader, 4);
            headerGrid.Children.Add(actionHeader);
            
            headerBorder.Child = headerGrid;
            accountsStack.Children.Add(headerBorder);

            // 创建账号列表ScrollViewer
            var accountsScrollViewer = new ScrollViewer { Margin = new Thickness(0, 0, 0, 0), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            _accountsList = new ListView { BorderThickness = new Thickness(0) };
            accountsScrollViewer.Content = _accountsList;
            
            // 创建选中项样式
            var itemContainerStyle = new Style(typeof(ListViewItem));
            var trigger = new Trigger { Property = ListViewItem.IsSelectedProperty, Value = true };
            trigger.Setters.Add(new Setter(ListViewItem.BackgroundProperty, Brushes.Transparent));
            trigger.Setters.Add(new Setter(ListViewItem.BorderBrushProperty, Brushes.Blue));
            trigger.Setters.Add(new Setter(ListViewItem.BorderThicknessProperty, new Thickness(2)));
            itemContainerStyle.Triggers.Add(trigger);
            _accountsList.ItemContainerStyle = itemContainerStyle;
            
            // 使用默认视图
            accountsStack.Children.Add(accountsScrollViewer);

            accountsBorder.Child = accountsStack;
            Grid.SetRow(accountsBorder, 0);
            AccountsPage.Children.Add(accountsBorder);

            // 网络器管理页面
            ServersPage = new Grid();
            ServersPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // 服务器管理区域
            var serversBorder = new Border { Margin = new Thickness(20) };
            // 设置边框样式
            serversBorder.Background = Brushes.Transparent;
            serversBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            serversBorder.BorderThickness = new Thickness(1);
            serversBorder.CornerRadius = new CornerRadius(12);
            serversBorder.Padding = new Thickness(20);
            var serversStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "网络器管理", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 16), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) }
                }
            };
            
            var serversHeader = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            serversHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            serversHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            serversHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            var serversListText = new TextBlock { Text = "服务器列表", FontSize = 16, FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(serversListText, 0);
            serversHeader.Children.Add(serversListText);

            var refreshServersButton = new Button { Content = "刷新", Background = Brushes.LightGray, Foreground = Brushes.Black, BorderThickness = new Thickness(0) };
            refreshServersButton.Click += async (sender, e) => {
                try
                {
                    refreshServersButton.Content = "刷新中...";
                    refreshServersButton.IsEnabled = false;
                    
                    await RefreshServersList();
                }
                catch (Exception ex)
                {
                    AddLog($"刷新服务器列表失败: {ex.Message}");
                }
                finally
                {
                    refreshServersButton.Content = "刷新";
                    refreshServersButton.IsEnabled = true;
                }
            };
            Grid.SetColumn(refreshServersButton, 1);
            serversHeader.Children.Add(refreshServersButton);

            // 添加搜索框
            var searchBox = new TextBox { Width = 180, Height = 36, Margin = new Thickness(0, 0, 0, 0), Background = Brushes.Transparent, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), BorderThickness = new Thickness(1), FontSize = 12, Text = "搜索服务器...", VerticalContentAlignment = VerticalAlignment.Center };
            searchBox.GotFocus += (sender, e) => {
                if (searchBox.Text == "搜索服务器...")
                {
                    searchBox.Text = "";
                }
            };
            searchBox.LostFocus += (sender, e) => {
                if (string.IsNullOrEmpty(searchBox.Text))
                {
                    searchBox.Text = "搜索服务器...";
                }
            };
            searchBox.TextChanged += async (sender, e) => {
                if (searchBox.Text != "搜索服务器...")
                {
                    // 实现搜索功能
                    await SearchServers(searchBox.Text);
                }
            };
            Grid.SetColumn(searchBox, 2);
            serversHeader.Children.Add(searchBox);

            serversStack.Children.Add(serversHeader);

            // 添加ScrollViewer以实现滚动功能
            var serversScrollViewer = new ScrollViewer { Margin = new Thickness(0, 16, 0, 0), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Height = 500 };
            // 使用WrapPanel来实现卡片式布局
            var serversWrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            serversScrollViewer.Content = serversWrapPanel;
            serversStack.Children.Add(serversScrollViewer);

            serversBorder.Child = serversStack;
            Grid.SetRow(serversBorder, 0);
            ServersPage.Children.Add(serversBorder);

            // 插件管理页面 - 已在XAML中实现



            // 日志页面
            LogsPage = new Grid();
            LogsPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var logsBorder = new Border { Margin = new Thickness(20) };
            // 设置边框样式
            logsBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            logsBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
            logsBorder.BorderThickness = new Thickness(1);
            logsBorder.CornerRadius = new CornerRadius(12);
            logsBorder.Padding = new Thickness(20);
            
            var logsGrid = new Grid();
            logsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            logsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            var titleText = new TextBlock { Text = "系统日志", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 16), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) };
            Grid.SetRow(titleText, 0);
            logsGrid.Children.Add(titleText);
            
            var logsScrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            LogOutput = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), TextWrapping = TextWrapping.Wrap };
            logsScrollViewer.Content = LogOutput;
            Grid.SetRow(logsScrollViewer, 1);
            logsGrid.Children.Add(logsScrollViewer);
            
            logsBorder.Child = logsGrid;
            Grid.SetRow(logsBorder, 0);
            LogsPage.Children.Add(logsBorder);

            // 默认显示主页
            ContentArea.Content = HomePage;

        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"初始化页面错误: {ex.Message}", "错误");
            AddLog($"初始化页面错误: {ex.Message}");
        }
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (NavigationList.SelectedItem is ListBoxItem selectedItem)
            {
                var tag = selectedItem.Tag;
                if (tag != null)
                {
                    string tagStr = tag.ToString() ?? "home";
                    ShowPage(tagStr);
                }
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"导航错误: {ex.Message}\n\n{ex.StackTrace}", "错误");
        }
    }

    private async void ShowPage(string pageTag)
        {
            try
            {
                if (string.IsNullOrEmpty(pageTag))
                {
                    pageTag = "home";
                }
                
                // 根据标签显示对应页面
                isServersPageActive = false;
                switch (pageTag)
                {
                    case "home":
                        // 显示主页
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        ContentArea.Content = HomePage;
                        AddLog("导航到主页", false);
                        break;
                    case "accounts":
                        // 显示账号管理页面
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        ContentArea.Content = AccountsPage;
                        AddLog("导航到游戏账号管理", false);
                        // 导航到账号管理界面时自动刷新
                        RefreshAccountsList();
                        break;
                    case "servers":
                        // 显示服务器管理页面
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        ContentArea.Content = ServersPage;
                        isServersPageActive = true;
                        AddLog("导航到网络器管理", false);
                        break;
                    case "rental":
                        // 显示租赁服页面
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        // 租赁服页面
                        var RentalPage = new Grid();
                        RentalPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        var rentalBorder = new Border { Margin = new Thickness(20) };
                        // 设置边框样式
                        rentalBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                        rentalBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                        rentalBorder.BorderThickness = new Thickness(1);
                        rentalBorder.CornerRadius = new CornerRadius(12);
                        rentalBorder.Padding = new Thickness(20);
                        var rentalStack = new StackPanel {
                            Children = {
                                new TextBlock { Text = "租赁服管理", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 16), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                                new TextBlock { Text = "租赁服管理功能正在开发中...", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), Margin = new Thickness(0, 16, 0, 0) }
                            }
                        };
                        rentalBorder.Child = rentalStack;
                        Grid.SetRow(rentalBorder, 0);
                        RentalPage.Children.Add(rentalBorder);
                        ContentArea.Content = RentalPage;
                        AddLog("导航到租赁服管理", false);
                        break;
                    case "skins":
                        // 显示我的皮肤页面
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        // 我的皮肤页面
                        var SkinsPage = new Grid();
                        SkinsPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        var skinsBorder = new Border { Margin = new Thickness(20) };
                        // 设置边框样式
                        skinsBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                        skinsBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                        skinsBorder.BorderThickness = new Thickness(1);
                        skinsBorder.CornerRadius = new CornerRadius(12);
                        skinsBorder.Padding = new Thickness(20);
                        var skinsStack = new StackPanel {
                            Children = {
                                new TextBlock { Text = "我的皮肤", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 16), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                                new TextBlock { Text = "皮肤管理功能正在开发中...", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), Margin = new Thickness(0, 16, 0, 0) }
                            }
                        };
                        skinsBorder.Child = skinsStack;
                        Grid.SetRow(skinsBorder, 0);
                        SkinsPage.Children.Add(skinsBorder);
                        ContentArea.Content = SkinsPage;
                        AddLog("导航到我的皮肤", false);
                        break;
                    case "plugins":
                        // 显示插件管理页面
                        ContentArea.Visibility = Visibility.Collapsed;
                        PluginsPage.Visibility = Visibility.Visible;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        AddLog("导航到插件管理", false);
                        // 导航到插件管理页面时自动刷新，确保UI更新后再刷新列表
                        await Dispatcher.InvokeAsync(async () => {
                            await RefreshPluginsList();
                        }, System.Windows.Threading.DispatcherPriority.Render);
                        break;
                    case "plugin-store":
                        // 显示插件商城页面
                        ContentArea.Visibility = Visibility.Collapsed;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Visible;
                        AddLog("导航到插件商城", false);
                        // 自动刷新插件商城列表，确保UI更新后再刷新列表
                        await Dispatcher.InvokeAsync(async () => {
                            await RefreshPluginStoreList();
                        }, System.Windows.Threading.DispatcherPriority.Render);
                        break;
                    case "proxy":
                        // 代理管理页面
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        var ProxyPage = new Grid();
                        ProxyPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        var proxyBorder = new Border { Margin = new Thickness(20) };
                        // 设置边框样式
                        proxyBorder.Background = Brushes.Transparent;
                        proxyBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                        proxyBorder.BorderThickness = new Thickness(1);
                        proxyBorder.CornerRadius = new CornerRadius(12);
                        proxyBorder.Padding = new Thickness(20);
                        var proxyStack = new StackPanel {
                            Children = {
                                new TextBlock { Text = "代理管理", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                                new TextBlock { Text = "管理您的代理，包括启动和停止代理进程。", FontSize = 14, Margin = new Thickness(0, 0, 0, 20), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) }
                            }
                        };
                        
                        var proxyHeader = new Grid { Margin = new Thickness(0, 0, 0, 16) };
                        proxyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        proxyHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                        var proxyListText = new TextBlock { Text = "代理列表", FontSize = 16, FontWeight = FontWeights.SemiBold };
                        Grid.SetColumn(proxyListText, 0);
                        proxyHeader.Children.Add(proxyListText);

                        var refreshProxiesButton = new Button { Content = "刷新", Background = Brushes.LightGray, Foreground = Brushes.Black, BorderThickness = new Thickness(0) };
                        refreshProxiesButton.Click += async (sender, e) => {
                            try
                            {
                                refreshProxiesButton.Content = "刷新中...";
                                refreshProxiesButton.IsEnabled = false;
                                
                                await RefreshProxiesList();
                                AddLog("代理列表已刷新");
                            }
                            catch (Exception ex)
                            {
                                AddLog($"刷新代理列表失败: {ex.Message}");
                            }
                            finally
                            {
                                refreshProxiesButton.Content = "刷新";
                                refreshProxiesButton.IsEnabled = true;
                            }
                        };
                        Grid.SetColumn(refreshProxiesButton, 1);
                        proxyHeader.Children.Add(refreshProxiesButton);

                        proxyStack.Children.Add(proxyHeader);

                        // 创建代理列表ScrollViewer
                        var proxiesScrollViewer = new ScrollViewer { Margin = new Thickness(0, 0, 0, 16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
                        var proxiesList = new ListView { BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
                        proxiesScrollViewer.Content = proxiesList;
                        var proxiesGridView = new GridView();
                        proxiesGridView.Columns.Add(new GridViewColumn { Header = "代理名称", Width = 200 });
                        proxiesGridView.Columns.Add(new GridViewColumn { Header = "状态", Width = 100 });
                        proxiesGridView.Columns.Add(new GridViewColumn { Header = "本地端口", Width = 100 });
                        proxiesGridView.Columns.Add(new GridViewColumn { Header = "操作", Width = 150 });
                        proxiesList.View = proxiesGridView;
                        proxyStack.Children.Add(proxiesScrollViewer);

                        // 保存代理列表引用
                        _proxiesList = proxiesList;

                        proxyBorder.Child = proxyStack;
                        Grid.SetRow(proxyBorder, 0);
                        ProxyPage.Children.Add(proxyBorder);
                        ContentArea.Content = ProxyPage;
                        AddLog("导航到代理管理", false);
                        // 自动刷新代理列表
                        _ = RefreshProxiesList();
                        break;
                    case "game-manager":
                        // 游戏管理页面
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        var GameManagerPage = new Grid();
                        GameManagerPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        var gameManagerBorder = new Border { Margin = new Thickness(20) };
                        // 设置边框样式
                        gameManagerBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                        gameManagerBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                        gameManagerBorder.BorderThickness = new Thickness(1);
                        gameManagerBorder.CornerRadius = new CornerRadius(12);
                        gameManagerBorder.Padding = new Thickness(20);
                        var gameManagerStack = new StackPanel {
                            Children = {
                                new TextBlock { Text = "游戏管理", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                                new TextBlock { Text = "管理您的游戏，包括启动和停止游戏进程。", FontSize = 14, Margin = new Thickness(0, 0, 0, 20), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) }
                            }
                        };
                        
                        var gameManagerHeader = new Grid { Margin = new Thickness(0, 0, 0, 16) };
                        gameManagerHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        gameManagerHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                        var gameManagerListText = new TextBlock { Text = "游戏列表", FontSize = 16, FontWeight = FontWeights.SemiBold };
                        Grid.SetColumn(gameManagerListText, 0);
                        gameManagerHeader.Children.Add(gameManagerListText);

                        var refreshGamesButton = new Button { Content = "刷新", Background = Brushes.LightGray, Foreground = Brushes.Black, BorderThickness = new Thickness(0) };
                        refreshGamesButton.Click += async (sender, e) => {
                            try
                            {
                                refreshGamesButton.Content = "刷新中...";
                                refreshGamesButton.IsEnabled = false;
                                
                                await RefreshGamesList();
                                AddLog("游戏列表已刷新");
                            }
                            catch (Exception ex)
                            {
                                AddLog($"刷新游戏列表失败: {ex.Message}");
                            }
                            finally
                            {
                                refreshGamesButton.Content = "刷新";
                                refreshGamesButton.IsEnabled = true;
                            }
                        };
                        Grid.SetColumn(refreshGamesButton, 1);
                        gameManagerHeader.Children.Add(refreshGamesButton);

                        gameManagerStack.Children.Add(gameManagerHeader);

                        // 创建游戏列表ScrollViewer
                        var gamesScrollViewer = new ScrollViewer { Margin = new Thickness(0, 0, 0, 16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
                        var gamesList = new ListView { BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
                        gamesScrollViewer.Content = gamesList;
                        var gamesGridView = new GridView();
                        gamesGridView.Columns.Add(new GridViewColumn { Header = "游戏名称", Width = 200 });
                        gamesGridView.Columns.Add(new GridViewColumn { Header = "状态", Width = 100 });
                        gamesGridView.Columns.Add(new GridViewColumn { Header = "进程ID", Width = 100 });
                        gamesGridView.Columns.Add(new GridViewColumn { Header = "操作", Width = 150 });
                        gamesList.View = gamesGridView;
                        gameManagerStack.Children.Add(gamesScrollViewer);

                        // 保存游戏列表引用
                        _gamesList = gamesList;

                        gameManagerBorder.Child = gameManagerStack;
                        Grid.SetRow(gameManagerBorder, 0);
                        GameManagerPage.Children.Add(gameManagerBorder);
                        ContentArea.Content = GameManagerPage;
                        AddLog("导航到游戏管理", false);
                        // 自动刷新游戏列表
                        _ = RefreshGamesList();
                        break;
                    case "logs":
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        ContentArea.Content = LogsPage;
                        AddLog("导航到系统日志", false);
                        break;
                    case "settings":
                        // 系统设置页面
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        var SettingsPage = new Grid();
                        SettingsPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        var settingsBorder = new Border { Margin = new Thickness(20) };
                        // 设置边框样式
                        settingsBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                        settingsBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                        settingsBorder.BorderThickness = new Thickness(1);
                        settingsBorder.CornerRadius = new CornerRadius(12);
                        settingsBorder.Padding = new Thickness(20);
                        var settingsStack = new StackPanel {
                            Children = {
                                new TextBlock { Text = "系统设置", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 16), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                                new TextBlock { Text = "系统设置功能正在开发中...", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), Margin = new Thickness(0, 16, 0, 0) }
                            }
                        };
                        settingsBorder.Child = settingsStack;
                        Grid.SetRow(settingsBorder, 0);
                        SettingsPage.Children.Add(settingsBorder);
                        ContentArea.Content = SettingsPage;
                        AddLog("导航到系统设置", false);
                        break;
                    default:
                        ContentArea.Visibility = Visibility.Visible;
                        PluginsPage.Visibility = Visibility.Collapsed;
                        PluginStorePage.Visibility = Visibility.Collapsed;
                        ContentArea.Content = HomePage;
                        AddLog("导航到主页", false);
                        break;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"页面显示错误: {ex.Message}\n\n{ex.StackTrace}", "错误");
            }
        }



    private void AddAccountButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 创建添加账号的对话框
            var dialog = new Window
            {
                Title = "添加账号",
                Width = 500,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"))
            };

            // 创建对话框内容
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // 登录方式选择
            var loginTypeLabel = new Label { Content = "登录方式:", Margin = new Thickness(20, 20, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold };
            Grid.SetRow(loginTypeLabel, 0);

            var loginTypeGrid = new Grid { Margin = new Thickness(20, 5, 20, 10) };
            loginTypeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            loginTypeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            loginTypeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(loginTypeGrid, 1);

            var sauthRadio = new RadioButton { Content = "Sauth", GroupName = "LoginType", IsChecked = true, Margin = new Thickness(5) };
            Grid.SetColumn(sauthRadio, 0);

            var com4399Radio = new RadioButton { Content = "4399Com", GroupName = "LoginType", Margin = new Thickness(5) };
            Grid.SetColumn(com4399Radio, 1);

            var email163Radio = new RadioButton { Content = "163Email", GroupName = "LoginType", Margin = new Thickness(5) };
            Grid.SetColumn(email163Radio, 2);

            loginTypeGrid.Children.Add(sauthRadio);
            loginTypeGrid.Children.Add(com4399Radio);
            loginTypeGrid.Children.Add(email163Radio);

            // 账号名称
            var nameLabel = new Label { Content = "名称:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold };
            Grid.SetRow(nameLabel, 2);
            var nameTextBox = new TextBox { Margin = new Thickness(20, 5, 20, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
            Grid.SetRow(nameTextBox, 3);

            // Sauth登录 (单参数)
            var sauthLabel = new Label { Content = "Sauth:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold };
            Grid.SetRow(sauthLabel, 4);
            var sauthTextBox = new TextBox { Margin = new Thickness(20, 5, 20, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), Height = 100, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(sauthTextBox, 5);

            // 账号密码登录 (双参数)
            var accountLabel = new Label { Content = "账号:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold, Visibility = Visibility.Collapsed };
            Grid.SetRow(accountLabel, 4);
            var accountTextBox = new TextBox { Margin = new Thickness(20, 5, 20, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), Visibility = Visibility.Collapsed };
            Grid.SetRow(accountTextBox, 5);

            var passwordLabel = new Label { Content = "密码:", Margin = new Thickness(20, 10, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold, Visibility = Visibility.Collapsed };
            Grid.SetRow(passwordLabel, 6);
            var passwordTextBox = new TextBox { Margin = new Thickness(20, 5, 20, 10), Background = Brushes.Transparent, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), Visibility = Visibility.Collapsed };
            Grid.SetRow(passwordTextBox, 7);

            // 登录方式切换事件
            sauthRadio.Checked += (s, args) => {
                sauthLabel.Visibility = Visibility.Visible;
                sauthTextBox.Visibility = Visibility.Visible;
                accountLabel.Visibility = Visibility.Collapsed;
                accountTextBox.Visibility = Visibility.Collapsed;
                passwordLabel.Visibility = Visibility.Collapsed;
                passwordTextBox.Visibility = Visibility.Collapsed;
                nameTextBox.IsEnabled = true;
            };

            com4399Radio.Checked += (s, args) => {
                sauthLabel.Visibility = Visibility.Collapsed;
                sauthTextBox.Visibility = Visibility.Collapsed;
                accountLabel.Visibility = Visibility.Visible;
                accountTextBox.Visibility = Visibility.Visible;
                passwordLabel.Visibility = Visibility.Visible;
                passwordTextBox.Visibility = Visibility.Visible;
                nameTextBox.IsEnabled = false;
                nameTextBox.Text = "此项是多余的";
                if (!string.IsNullOrEmpty(accountTextBox.Text)) {
                    nameTextBox.Text = accountTextBox.Text;
                }
            };

            email163Radio.Checked += (s, args) => {
                sauthLabel.Visibility = Visibility.Collapsed;
                sauthTextBox.Visibility = Visibility.Collapsed;
                accountLabel.Visibility = Visibility.Visible;
                accountTextBox.Visibility = Visibility.Visible;
                passwordLabel.Visibility = Visibility.Visible;
                passwordTextBox.Visibility = Visibility.Visible;
                nameTextBox.IsEnabled = false;
                nameTextBox.Text = "此项是多余的";
                if (!string.IsNullOrEmpty(accountTextBox.Text)) {
                    nameTextBox.Text = accountTextBox.Text;
                }
            };

            // 账号输入变化时，自动更新名称
            accountTextBox.TextChanged += (s, args) => {
                if ((com4399Radio.IsChecked == true || email163Radio.IsChecked == true) && !string.IsNullOrEmpty(accountTextBox.Text)) {
                    nameTextBox.Text = accountTextBox.Text;
                }
            };

            // 按钮
            var buttonGrid = new Grid { Margin = new Thickness(20, 20, 20, 20) };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(buttonGrid, 8);

            var cancelButton = new Button { Content = "取消", Margin = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
            cancelButton.Click += (s, args) => dialog.Close();
            Grid.SetColumn(cancelButton, 0);

            var saveButton = new Button { Content = "保存", Margin = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            saveButton.Click += async (s, args) => {
                try
                {
                    // 显示保存中状态
                    saveButton.Content = "保存中...";
                    saveButton.IsEnabled = false;

                    // 创建账号实体
                    Aite.Config.Entities.Login.EntityAccount account;

                    // 根据选择的登录方式创建不同的账号实体
                    if (sauthRadio.IsChecked == true)
                    {
                        account = new Aite.Config.Entities.Login.EntityAccount
                        {
                            Type = "cookie",
                            Account = nameTextBox.Text,
                            Password = sauthTextBox.Text
                        };
                    }
                    else if (com4399Radio.IsChecked == true)
                    {
                        account = new Aite.Config.Entities.Login.EntityAccount
                        {
                            Type = "4399com",
                            Account = accountTextBox.Text,
                            Password = passwordTextBox.Text
                        };
                    }
                    else if (email163Radio.IsChecked == true)
                    {
                        account = new Aite.Config.Entities.Login.EntityAccount
                        {
                            Type = "163Email",
                            Account = accountTextBox.Text,
                            Password = passwordTextBox.Text
                        };
                    }
                    else
                    {
                        // 默认使用Sauth登录
                        account = new Aite.Config.Entities.Login.EntityAccount
                        {
                            Type = "cookie",
                            Account = nameTextBox.Text,
                            Password = sauthTextBox.Text
                        };
                    }

                    // 检查是否已存在相同名称的账号
                    bool isNameDuplicate = false;
                    bool isUserIdDuplicate = false;
                    bool loginSuccess = false;
                    await Task.Run(() => {
                        var existingAccounts = Aite.Core.Message.AccountMessage.GetAccountList();
                        
                        // 检查账号名称是否重复
                        foreach (var existingAccount in existingAccounts)
                        {
                            if (existingAccount.Account == account.Account && existingAccount.Id != account.Id)
                            {
                                isNameDuplicate = true;
                                return;
                            }
                        }
                        
                        // 尝试登录以获取 User ID
                        try
                        {
                            // 对于4399和4399com登录，需要先获取验证码
                            if (account.Type == "4399" || account.Type == "4399com")
                            {
                                // 更新验证码
                                Aite.Core.Message.AccountMessage.UpdateCaptcha();
                                // 自动获取验证码内容
                                var captchaContent = Aite.Core.Message.AccountMessage.GetCaptcha4399Content().Result;
                                Aite.Core.Message.AccountMessage.Captcha4399 = captchaContent;
                            }
                            
                            Aite.Core.Message.AccountMessage.Login(account);
                            loginSuccess = true;
                            // 登录成功后检查是否有相同 User ID 的账号
                            foreach (var existingAccount in existingAccounts)
                            {
                                if (existingAccount.UserId == account.UserId && existingAccount.Id != account.Id)
                                {
                                    isUserIdDuplicate = true;
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // 登录失败，继续保存
                            AddLog($"登录失败: {ex.Message}");
                        }
                    });

                    if (isNameDuplicate)
                    {
                        CustomMessageBox.Show("账号名称已存在", "提示");
                        AddLog("账号名称已存在，无需重复添加");
                        return;
                    }

                    if (isUserIdDuplicate)
                    {
                        CustomMessageBox.Show("账号已登录", "提示");
                        AddLog("账号已登录，无需重复添加");
                        dialog.Close();
                        return;
                    }

                    // 在后台线程中保存账号
                    await Task.Run(() => {
                        Aite.Core.Message.AccountMessage.SaveAccount(account);
                    });

                    AddLog("账号添加成功！");
                    dialog.Close();

                    // 刷新账号列表
                    RefreshAccountsList();
                    
                    // 如果登录成功且是首次登录，刷新服务器列表
                    if (loginSuccess && !hasLoggedIn)
                    {
                        AddLog("首次登录，自动刷新服务器列表...");
                        _ = Task.Run(async () => {
                            await RefreshServersList();
                            hasLoggedIn = true;
                        });
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"添加账号失败: {ex.Message}", "错误");
                    AddLog($"添加账号失败: {ex.Message}");
                }
                finally
                {
                    // 恢复按钮状态
                    saveButton.Content = "保存";
                    saveButton.IsEnabled = true;
                }
            };
            Grid.SetColumn(saveButton, 1);

            buttonGrid.Children.Add(cancelButton);
            buttonGrid.Children.Add(saveButton);

            grid.Children.Add(loginTypeLabel);
            grid.Children.Add(loginTypeGrid);
            grid.Children.Add(nameLabel);
            grid.Children.Add(nameTextBox);
            grid.Children.Add(sauthLabel);
            grid.Children.Add(sauthTextBox);
            grid.Children.Add(accountLabel);
            grid.Children.Add(accountTextBox);
            grid.Children.Add(passwordLabel);
            grid.Children.Add(passwordTextBox);
            grid.Children.Add(buttonGrid);

            dialog.Content = grid;
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"打开添加账号对话框失败: {ex.Message}", "错误");
            AddLog($"打开添加账号对话框失败: {ex.Message}");
        }
    }

    private bool isRefreshingAccounts = false;
    private string lastAccountsHash = "";
    
    private async Task RefreshAccountsListAsync()
    {
        // 防止重复调用
        if (isRefreshingAccounts)
        {
            return;
        }
        
        try
        {
            isRefreshingAccounts = true;
            
            // 检查账号列表ListView是否存在
            if (_accountsList != null)
            {
                // 在后台线程中获取所有账号
                var accounts = await Task.Run(() => {
                    return Aite.Core.Message.AccountMessage.GetAccountList();
                });
                
                // 计算账号列表的哈希值，用于检测变化
                string currentHash = CalculateAccountsHash(accounts);
                
                // 只有当账号列表发生变化时才刷新
                if (currentHash == lastAccountsHash)
                {
                    return;
                }
                
                lastAccountsHash = currentHash;
                
                // 在UI线程中更新列表
                await Dispatcher.InvokeAsync(() => {
                    // 清空现有列表
                    _accountsList.Items.Clear();
                    
                    // 添加到列表
                    foreach (var account in accounts)
                    {
                        // 创建一个包含所有信息的Grid
                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 使用星号宽度，确保操作列占据剩余空间
                        
                        // 账号
                        var accountText = new TextBlock { Text = account.Account, VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetColumn(accountText, 0);
                        grid.Children.Add(accountText);
                        
                        // User ID
                        var userIdText = new TextBlock { Text = account.UserId ?? "未登录", VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetColumn(userIdText, 1);
                        grid.Children.Add(userIdText);
                        
                        // 类型
                        var typeText = new TextBlock { Text = account.Type, VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetColumn(typeText, 2);
                        grid.Children.Add(typeText);
                        
                        // 状态
                        var statusText = new TextBlock { Text = account.UserId != null ? "已登录" : "已保存", VerticalAlignment = VerticalAlignment.Center, Foreground = account.UserId != null ? Brushes.Blue : Brushes.Green };
                        Grid.SetColumn(statusText, 3);
                        grid.Children.Add(statusText);
                        
                        // 操作按钮
                        var actionCell = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                        var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 5, 0), Width = double.NaN };
                        
                        var loginButton = new Button { Content = "登录", Width = 70, Margin = new Thickness(0, 0, 5, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                        loginButton.Click += async (sender, e) => {
                            // 登录该账号
                            try
                            {
                                loginButton.Content = "登录中...";
                                loginButton.IsEnabled = false;
                                
                                AddLog($"尝试登录账号: {account.Account}");
                                
                                // 对于4399和4399com账号，需要验证码
                                if (account.Type == "4399" || account.Type == "4399com")
                                {
                                    // 创建验证码输入对话框
                                    var captchaDialog = new Window
                                    {
                                        Title = "验证码",
                                        Width = 400,
                                        Height = 200,
                                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                                        ResizeMode = ResizeMode.NoResize,
                                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"))
                                    };

                                    var captchaGrid = new Grid();
                                    captchaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                                    captchaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                                    captchaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                                    captchaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                                    var captchaLabel = new Label { Content = "请输入验证码:", Margin = new Thickness(20, 20, 20, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), FontWeight = FontWeights.Bold };
                                    Grid.SetRow(captchaLabel, 0);

                                    var captchaImageGrid = new Grid { Margin = new Thickness(20, 5, 20, 10) };
                                    captchaImageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                    captchaImageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                                    Grid.SetRow(captchaImageGrid, 1);

                                    var captchaTextBox = new TextBox { Margin = new Thickness(0, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
                                    Grid.SetColumn(captchaTextBox, 0);

                                    var captchaImage = new Image { Width = 100, Height = 40, Margin = new Thickness(10, 0, 0, 0), Stretch = Stretch.UniformToFill };
                                    Grid.SetColumn(captchaImage, 1);

                                    captchaImageGrid.Children.Add(captchaTextBox);
                                    captchaImageGrid.Children.Add(captchaImage);

                                    var buttonGrid = new Grid { Margin = new Thickness(20, 20, 20, 20) };
                                    buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                    buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                    Grid.SetRow(buttonGrid, 3);

                                    var cancelButton = new Button { Content = "取消", Margin = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")) };
                                    cancelButton.Click += (s, args) => captchaDialog.Close();
                                    Grid.SetColumn(cancelButton, 0);

                                    var confirmButton = new Button { Content = "确认", Margin = new Thickness(10, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
                                    bool isConfirmed = false;
                                    confirmButton.Click += (s, args) => {
                                        isConfirmed = true;
                                        captchaDialog.Close();
                                    };
                                    Grid.SetColumn(confirmButton, 1);

                                    buttonGrid.Children.Add(cancelButton);
                                    buttonGrid.Children.Add(confirmButton);

                                    captchaGrid.Children.Add(captchaLabel);
                                    captchaGrid.Children.Add(captchaImageGrid);
                                    captchaGrid.Children.Add(buttonGrid);

                                    // 更新验证码图片
                                    Aite.Core.Message.AccountMessage.UpdateCaptcha();
                                    if (Aite.Core.Message.AccountMessage.Captcha4399Bytes != null)
                                    {
                                        var bitmap = new BitmapImage();
                                        using (var stream = new MemoryStream(Aite.Core.Message.AccountMessage.Captcha4399Bytes))
                                        {
                                            bitmap.BeginInit();
                                            bitmap.StreamSource = stream;
                                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                            bitmap.EndInit();
                                        }
                                        captchaImage.Source = bitmap;
                                    }

                                    // 验证码图片点击事件，用于刷新验证码
                                    captchaImage.MouseLeftButtonDown += (s, args) => {
                                        Aite.Core.Message.AccountMessage.UpdateCaptcha();
                                        if (Aite.Core.Message.AccountMessage.Captcha4399Bytes != null)
                                        {
                                            var bitmap = new BitmapImage();
                                            using (var stream = new MemoryStream(Aite.Core.Message.AccountMessage.Captcha4399Bytes))
                                            {
                                                bitmap.BeginInit();
                                                bitmap.StreamSource = stream;
                                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                                bitmap.EndInit();
                                            }
                                            captchaImage.Source = bitmap;
                                        }
                                    };

                                    captchaDialog.Content = captchaGrid;
                                    captchaDialog.ShowDialog();

                                    if (!isConfirmed)
                                    {
                                        throw new Exception("用户取消登录");
                                    }

                                    // 设置验证码
                                    Aite.Core.Message.AccountMessage.Captcha4399 = captchaTextBox.Text;
                                    if (string.IsNullOrEmpty(Aite.Core.Message.AccountMessage.Captcha4399))
                                    {
                                        throw new Exception("验证码不能为空");
                                    }
                                    AddLog($"使用验证码: {Aite.Core.Message.AccountMessage.Captcha4399}");
                                }
                                
                                // 在后台线程中登录
                                await Task.Run(() => {
                                    Aite.Core.Message.AccountMessage.Login(account);
                                });
                                
                                AddLog($"登录成功: {account.Account}");
                                
                                // 刷新账号列表
                                await RefreshAccountsListAsync();
                                
                                // 只有第一次登录时刷新服务器列表
                                if (!hasLoggedIn)
                                {
                                    // 刷新服务器列表
                                    await RefreshServersList();
                                    hasLoggedIn = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                CustomMessageBox.Show($"登录失败: {ex.Message}", "错误");
                                AddLog($"登录失败: {ex.Message}");
                            }
                            finally
                            {
                                loginButton.Content = "登录";
                                loginButton.IsEnabled = true;
                            }
                        };
                        buttonStack.Children.Add(loginButton);
                        
                        var deleteButton = new Button { Content = "删除", Width = 70, Margin = new Thickness(5, 0, 0, 0), Background = Brushes.LightCoral, Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                        deleteButton.Click += async (sender, e) => {
                            // 删除该账号
                            try
                            {
                                if (account.Id.HasValue)
                                {
                                    await Task.Run(() => {
                                        Aite.Core.Message.AccountMessage.DeleteAccount(account.Id.Value);
                                    });
                                    AddLog($"账号已删除: {account.Account}");
                                    await RefreshAccountsListAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                CustomMessageBox.Show($"删除账号失败: {ex.Message}", "错误");
                                AddLog($"删除账号失败: {ex.Message}");
                            }
                        };
                        buttonStack.Children.Add(deleteButton);
                        
                        actionCell.Children.Add(buttonStack);
                        Grid.SetColumn(actionCell, 4);
                        grid.Children.Add(actionCell);
                        
                        // 直接添加Grid到ListView，不使用ListViewItem
                        _accountsList.Items.Add(grid);
                    }
                    
                    AddLog($"账号列表已刷新，共 {accounts.Length} 个账号");
                });
            }
            else
            {
                AddLog("未找到账号列表ListView");
            }
        }
        catch (Exception ex)
        {
            AddLog($"刷新账号列表失败: {ex.Message}");
        }
        finally
        {
            isRefreshingAccounts = false;
        }
    }
    
    // 计算账号列表的哈希值，用于检测变化
    private string CalculateAccountsHash(Aite.Config.Entities.Login.EntityAccount[] accounts)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var builder = new System.Text.StringBuilder();
            foreach (var account in accounts)
            {
                builder.Append($"{account.Id}:{account.Account}:{account.Type}:{account.UserId}:{account.Token}");
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
    
    // 异步加载公告数据
    private async Task LoadAnnouncementAsync(TextBlock contentBlock, TextBlock dateBlock)
    {
        try
        {
            AddLog("开始加载公告数据...");
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10); // 设置超时时间
                var response = await client.GetAsync("http://c4b51997.hk-vh-c.x-c.top/aite.html");
                
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                
                
                // 解析 JSON
                var announcementData = JsonSerializer.Deserialize<AnnouncementData>(jsonString);
                
                // 在UI线程中更新公告内容
                await Dispatcher.InvokeAsync(() => {
                    if (announcementData != null)
                    {
                        AddLog($"解析成功，日期: {announcementData.date}");
                        // 更新公告内容
                        contentBlock.Text = announcementData.msg;
                        
                        // 格式化日期
                        string dateStr = announcementData.date.ToString();
                        if (dateStr.Length == 8)
                        {
                            string year = dateStr.Substring(0, 4);
                            string month = dateStr.Substring(4, 2);
                            string day = dateStr.Substring(6, 2);
                            dateBlock.Text = $"发布时间: {year}-{month}-{day}";
                        }
                        else
                        {
                            dateBlock.Text = $"发布时间: {announcementData.date}";
                        }
                    }
                    else
                    {
                        AddLog("解析 JSON 失败，结果为 null");
                        contentBlock.Text = "公告数据加载失败";
                        dateBlock.Text = "发布时间: 未知";
                    }
                });
            }
        }
        catch (Exception ex)
        {
            AddLog($"加载公告失败: {ex.Message}");
            AddLog($"错误堆栈: {ex.StackTrace}");
            // 在UI线程中更新错误信息
            await Dispatcher.InvokeAsync(() => {
                contentBlock.Text = "公告加载失败";
                dateBlock.Text = "发布时间: 未知";
            });
        }
    }
    
    private void RefreshAccountsList()
    {
        _ = RefreshAccountsListAsync();
    }
    
    private bool isRefreshingServers = false;
    private bool isServersPageActive = false;
    private int serversOffset = 0;
    
    private bool isRefreshingPlugins = false;
    private bool isRefreshingPluginStore = false;
    private bool isRefreshingGames = false;
    private bool isRefreshingProxies = false;
    
    private async Task RefreshGamesList()
    {
        // 防止重复调用
        if (isRefreshingGames)
        {
            AddLog("游戏列表正在刷新中...");
            return;
        }
        
        try
        {
            isRefreshingGames = true;
            AddLog("开始刷新游戏列表...");
            
            // 找到游戏列表ListView
            if (_gamesList != null)
            {
                // 使用FanNEL的ActiveGameAndProxies获取游戏列表
                var games = await Task.Run(() => {
                    try
                    {
                        AddLog("开始获取游戏列表");
                        // 这里应该调用FanNEL的API获取游戏列表
                        // 暂时返回空列表
                        return new List<object>();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"获取游戏列表失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                        return new List<object>();
                    }
                });
                
                // 在UI线程中更新列表
                await Dispatcher.InvokeAsync(() => {
                    try
                    {
                        // 清空现有列表
                        _gamesList.Items.Clear();
                        AddLog("已清空现有游戏列表");
                        
                        // 添加到列表
                        foreach (var game in games)
                        {
                            // 创建一个包含所有信息的Grid
                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                            
                            // 游戏名称
                            var nameText = new TextBlock { Text = "Minecraft", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(nameText, 0);
                            grid.Children.Add(nameText);
                            
                            // 状态
                            var statusText = new TextBlock { Text = "运行中", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0), Foreground = Brushes.Green };
                            Grid.SetColumn(statusText, 1);
                            grid.Children.Add(statusText);
                            
                            // 进程ID
                            var pidText = new TextBlock { Text = "1234", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(pidText, 2);
                            grid.Children.Add(pidText);
                            
                            // 操作按钮
                            var actionCell = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 5, 0), Width = double.NaN };
                            
                            // 停止按钮
                            var stopButton = new Button { Content = "停止", Width = 60, Margin = new Thickness(0, 0, 5, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                            stopButton.Click += async (sender, e) => {
                                try
                                {
                                    stopButton.Content = "停止中...";
                                    stopButton.IsEnabled = false;
                                    
                                    AddLog("开始停止游戏");
                                    
                                    // 这里应该调用FanNEL的API停止游戏
                                    await Task.Delay(1000);
                                    
                                    AddLog("游戏停止成功");
                                    // 刷新游戏列表
                                    await RefreshGamesList();
                                }
                                catch (Exception ex)
                                {
                                    AddLog($"停止游戏失败: {ex.Message}");
                                }
                                finally
                                {
                                    stopButton.Content = "停止";
                                    stopButton.IsEnabled = true;
                                }
                            };
                            buttonStack.Children.Add(stopButton);
                            
                            actionCell.Children.Add(buttonStack);
                            Grid.SetColumn(actionCell, 3);
                            grid.Children.Add(actionCell);
                            
                            // 直接添加Grid到ListView
                            _gamesList.Items.Add(grid);
                            AddLog("已添加游戏到列表");
                        }
                        
                        AddLog($"游戏列表已刷新，共 {games.Count} 个游戏");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"更新游戏列表UI失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                    }
                });
            }
            else
            {
                AddLog("未找到游戏列表ListView");
            }
        }
        catch (Exception ex)
        {
            AddLog($"刷新游戏列表失败: {ex.Message}");
            AddLog($"错误堆栈: {ex.StackTrace}");
        }
        finally
        {
            isRefreshingGames = false;
        }
    }
    
    private async Task RefreshProxiesList()
    {
        // 防止重复调用
        if (isRefreshingProxies)
        {
            AddLog("代理列表正在刷新中...");
            return;
        }
        
        try
        {
            isRefreshingProxies = true;
            AddLog("开始刷新代理列表...");
            
            // 找到代理列表ListView
            if (_proxiesList != null)
            {
                // 获取代理列表
                var proxies = await Task.Run(() => {
                    try
                    {
                        AddLog("开始获取代理列表");
                        // 使用_runningProxies列表中的数据
                        var result = new List<object>();
                        if (_runningProxies.Count > 0)
                        {
                            AddLog($"使用运行中的代理数据，共 {_runningProxies.Count} 个代理");
                            // 将_runningProxies中的每个代理信息添加到结果中
                            foreach (var proxyInfo in _runningProxies)
                            {
                                result.Add(proxyInfo);
                            }
                        }
                        else
                        {
                            AddLog("没有运行中的代理");
                        }
                        return result;
                    }
                    catch (Exception ex)
                    {
                        AddLog($"获取代理列表失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                        return new List<object>();
                    }
                });
                
                // 在UI线程中更新列表
                await Dispatcher.InvokeAsync(() => {
                    try
                    {
                        // 清空现有列表
                        _proxiesList.Items.Clear();
                        AddLog("已清空现有代理列表");
                        
                        // 添加到列表
                        foreach (var proxy in proxies)
                        {
                            // 创建一个包含所有信息的Grid
                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                            
                            // 代理名称
                            string serverName = "布吉岛·新玩法上线";
                            string roleName = "Ran1";
                            string proxyName = $"{roleName}({serverName})";
                            string port = "25565";
                            
                            // 检查代理类型并提取信息
                            try
                            {
                                // 如果代理是ProxyInfo类型，使用实际数据
                                if (proxy is ProxyInfo proxyInfo)
                                {
                                    serverName = proxyInfo.ServerName;
                                    roleName = proxyInfo.RoleName;
                                    proxyName = $"{roleName}({serverName})";
                                    port = proxyInfo.Port;
                                    AddLog($"使用代理信息: {proxyInfo.ServerName} - {proxyInfo.RoleName}");
                                }
                                else
                                {
                                    AddLog($"代理类型: {proxy.GetType().Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLog($"解析代理信息失败: {ex.Message}");
                            }
                            
                            var nameText = new TextBlock { Text = proxyName, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(nameText, 0);
                            grid.Children.Add(nameText);
                            
                            // 状态
                            var statusText = new TextBlock { Text = "运行中", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0), Foreground = Brushes.Green };
                            Grid.SetColumn(statusText, 1);
                            grid.Children.Add(statusText);
                            
                            // 本地端口
                            var portText = new TextBlock { Text = port, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(portText, 2);
                            grid.Children.Add(portText);
                            
                            // 操作按钮
                            var actionCell = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 5, 0), Width = double.NaN };
                            
                            // 停止按钮
                            var stopButton = new Button { Content = "停止", Width = 60, Margin = new Thickness(0, 0, 5, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                            // 保存当前代理信息
                            var currentProxyInfo = proxy as ProxyInfo;
                            stopButton.Click += async (sender, e) => {
                                try
                                {
                                    stopButton.Content = "停止中...";
                                    stopButton.IsEnabled = false;
                                    
                                    AddLog("开始停止代理");
                                    
                                    if (currentProxyInfo != null)
                                    {
                                        // 使用Aite.Core的API停止代理
                                        await Task.Run(() => {
                                            try
                                            {
                                                // 尝试使用ProxiesMessage停止代理
                                                // 这里使用实际的停止API，使用保存的服务器ID和角色名
                                                Aite.Core.Message.ProxiesMessage.StopProxy(currentProxyInfo.ServerId, currentProxyInfo.RoleName);
                                                AddLog("代理停止成功");
                                                
                                                // 从运行中的代理列表中移除
                                                lock (_runningProxies)
                                                {
                                                    _runningProxies.Remove(currentProxyInfo);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AddLog($"停止代理失败: {ex.Message}");
                                                throw new Exception($"停止代理失败: {ex.Message}");
                                            }
                                        });
                                        
                                        AddLog("代理停止成功");
                                        // 刷新代理列表
                                        await RefreshProxiesList();
                                    }
                                    else
                                    {
                                        AddLog("无法停止代理: 代理信息不存在");
                                        CustomMessageBox.Show("无法停止代理: 代理信息不存在", "错误");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AddLog($"停止代理失败: {ex.Message}");
                                    CustomMessageBox.Show($"停止代理失败: {ex.Message}", "错误");
                                }
                                finally
                                {
                                    stopButton.Content = "停止";
                                    stopButton.IsEnabled = true;
                                }
                            };
                            buttonStack.Children.Add(stopButton);
                            
                            // 复制IP按钮
                            var copyIpButton = new Button { Content = "复制", Width = 60, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                            copyIpButton.Click += (sender, e) => {
                                try
                                {
                                    // 代理IP
                                    string proxyIp = "127.0.0.1";
                                    string proxyAddress = $"{proxyIp}:{port}";
                                    
                                    // 复制到剪贴板
                                    System.Windows.Clipboard.SetText(proxyAddress);
                                    AddLog($"已复制代理IP: {proxyAddress}");
                                    CustomMessageBox.Show($"已复制代理IP: {proxyAddress}", "成功");
                                }
                                catch (Exception ex)
                                {
                                    AddLog($"复制代理IP失败: {ex.Message}");
                                    CustomMessageBox.Show($"复制代理IP失败: {ex.Message}", "错误");
                                }
                            };
                            buttonStack.Children.Add(copyIpButton);
                            
                            actionCell.Children.Add(buttonStack);
                            Grid.SetColumn(actionCell, 3);
                            grid.Children.Add(actionCell);
                            
                            // 直接添加Grid到ListView
                            _proxiesList.Items.Add(grid);
                            AddLog($"已添加代理到列表: {proxyName}");
                        }
                        
                        AddLog($"代理列表已刷新，共 {proxies.Count} 个代理");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"更新代理列表UI失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                    }
                });
            }
            else
            {
                AddLog("未找到代理列表ListView");
            }
        }
        catch (Exception ex)
        {
            AddLog($"刷新代理列表失败: {ex.Message}");
            AddLog($"错误堆栈: {ex.StackTrace}");
        }
        finally
        {
            isRefreshingProxies = false;
        }
    }
    
    private async Task RefreshPluginStoreList()
    {
        // 防止重复调用
        if (isRefreshingPluginStore)
        {
            AddLog("插件商城列表正在刷新中...");
            return;
        }
        
        try
        {
            isRefreshingPluginStore = true;
            AddLog("开始刷新插件商城列表...");
            
            // 找到插件商城列表ListView
            AddLog("开始查找插件商城列表ListView");
            var pluginStoreList = FindVisualChild<ListView>(PluginStorePage);
            AddLog($"查找结果: pluginStoreList = {pluginStoreList != null}");
            if (pluginStoreList != null)
            {
                // 使用FanNEL的PlugInstoreMessage获取插件列表
            var plugins = await Task.Run(async () => {
                try
                {
                    AddLog("开始调用PlugInstoreMessage.GetPluginList()");
                    var result = await PlugInstoreMessage.GetPluginList();
                    AddLog($"获取插件商城列表成功，共 {result.Length} 个插件");
                    foreach (var plugin in result)
                    {
                        AddLog($"插件: {plugin.Name} (ID: {plugin.Id}, 发布者: {plugin.Publisher})");
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    AddLog($"获取插件商城列表失败: {ex.Message}");
                    AddLog($"错误堆栈: {ex.StackTrace}");
                    return new Aite.Core.Entities.Plugin.EntityComponents[0];
                }
            });
                
                // 获取本地已安装的插件列表
                var localPlugins = await Task.Run(() => {
                    try
                    {
                        return Aite.Core.Message.PluginMessage.GetPluginListSafe();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"获取本地插件列表失败: {ex.Message}");
                        return new List<Aite.Core.Entities.NEL.EntityPluginState>();
                    }
                });
                
                // 为每个插件获取详细信息，包括版本号
                var pluginsWithDetails = await Task.Run(async () => {
                    var result = new List<(Aite.Core.Entities.Plugin.EntityComponents, string)>();
                    foreach (var plugin in plugins)
                    {
                        try
                        {
                            // 调用GetPluginDetail获取详细信息，包括版本号
                            var pluginDetail = await Aite.Core.Message.PlugInstoreMessage.GetPluginDetail(plugin.Id);
                            string version = pluginDetail?.Data?.Version ?? "";
                            result.Add((plugin, version));
                        }
                        catch (Exception ex)
                        {
                            AddLog($"获取插件 {plugin.Name} 详情失败: {ex.Message}");
                            result.Add((plugin, ""));
                        }
                    }
                    return result;
                });
                
                // 在UI线程中更新列表
                await Dispatcher.InvokeAsync(() => {
                    try
                    {
                        // 清空现有列表
                        pluginStoreList.Items.Clear();
                        AddLog("已清空现有插件商城列表");
                        
                        // 添加到列表
                        foreach (var (plugin, storeVersion) in pluginsWithDetails)
                        {
                            // 创建一个包含所有信息的Grid
                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            
                            // 名称
                            var nameText = new TextBlock { Text = plugin.Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(nameText, 0);
                            grid.Children.Add(nameText);
                            
                            // 版本/描述
                            var versionText = new TextBlock { Text = plugin.ShortDescription, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0), TextWrapping = TextWrapping.Wrap };
                            Grid.SetColumn(versionText, 1);
                            grid.Children.Add(versionText);
                            
                            // 发布者
                            var authorText = new TextBlock { Text = plugin.Publisher, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(authorText, 2);
                            grid.Children.Add(authorText);
                            
                            // 下载次数
                            var descriptionText = new TextBlock { Text = plugin.DownloadCount.ToString(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(descriptionText, 3);
                            grid.Children.Add(descriptionText);
                            
                            // 操作按钮
                            var actionCell = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 5, 0), Width = double.NaN };
                            
                            // 检查插件是否已安装
                            var localPlugin = localPlugins.FirstOrDefault(p => p.Id.Equals(plugin.Id, StringComparison.OrdinalIgnoreCase));
                            
                            if (localPlugin != null)
                            {
                                // 插件已安装，检查是否可更新
                                bool canUpdate = false;
                                
                                try
                                {
                                    // 获取本地插件版本
                                    string localVersion = localPlugin.Version;
                                    AddLog($"本地插件版本: {localVersion}");
                                    
                                    AddLog($"商城插件版本: {storeVersion}");
                                    
                                    // 比较版本号
                                    if (!string.IsNullOrEmpty(localVersion) && !string.IsNullOrEmpty(storeVersion))
                                    {
                                        canUpdate = CompareVersions(storeVersion, localVersion) > 0;
                                        AddLog($"版本比较结果: {canUpdate}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AddLog($"版本比较失败: {ex.Message}");
                                    // 出错时默认显示更新按钮
                                    canUpdate = true;
                                }
                                
                                if (canUpdate)
                                {
                                    var updateButton = new Button { Content = "更新", Width = 80, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                                    updateButton.Click += async (sender, e) => {
                                        try
                                        {
                                            updateButton.Content = "更新中...";
                                            updateButton.IsEnabled = false;
                                            
                                            AddLog($"开始更新插件: {plugin.Name}");
                                            
                                            // 使用FanNEL的PlugInstoreMessage更新插件
                                            await Task.Run(async () => {
                                                try
                                                {
                                                    await PlugInstoreMessage.Install(plugin.Id);
                                                }
                                                catch (Exception ex)
                                                {
                                                    throw new Exception($"更新插件失败: {ex.Message}");
                                                }
                                            });
                                            
                                            AddLog($"插件 {plugin.Name} 更新成功");
                                            // 刷新本地插件列表
                                            await RefreshPluginsList();
                                            // 刷新插件商城列表，更新插件状态为已更新
                                            await RefreshPluginStoreList();
                                        }
                                        catch (Exception ex)
                                        {
                                            AddLog($"更新插件失败: {ex.Message}");
                                        }
                                        finally
                                        {
                                            updateButton.Content = "更新";
                                            updateButton.IsEnabled = true;
                                        }
                                    };
                                    buttonStack.Children.Add(updateButton);
                                }
                                else
                                {
                                    // 插件已拥有且无需更新，显示"已拥有"的灰色按钮
                                    var ownedButton = new Button { Content = "已拥有", Width = 80, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")), BorderThickness = new Thickness(0), FontSize = 12, IsEnabled = false };
                                    buttonStack.Children.Add(ownedButton);
                                }
                            }
                            else
                            {
                                // 插件未安装，显示下载按钮
                                var downloadButton = new Button { Content = "下载", Width = 80, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                                downloadButton.Click += async (sender, e) => {
                                    try
                                    {
                                        downloadButton.Content = "下载中...";
                                        downloadButton.IsEnabled = false;
                                        
                                        AddLog($"开始下载插件: {plugin.Name}");
                                        
                                        // 使用FanNEL的PlugInstoreMessage下载插件
                                            await Task.Run(async () => {
                                                try
                                                {
                                                    await PlugInstoreMessage.Install(plugin.Id);
                                                }
                                                catch (Exception ex)
                                                {
                                                    throw new Exception($"下载插件失败: {ex.Message}");
                                                }
                                            });
                                        
                                        AddLog($"插件 {plugin.Name} 下载成功");
                                        // 刷新本地插件列表
                                        await RefreshPluginsList();
                                        // 刷新插件商城列表，更新插件状态为已下载
                                        await RefreshPluginStoreList();
                                    }
                                    catch (Exception ex)
                                    {
                                        AddLog($"下载插件失败: {ex.Message}");
                                    }
                                    finally
                                    {
                                        downloadButton.Content = "下载";
                                        downloadButton.IsEnabled = true;
                                    }
                                };
                                buttonStack.Children.Add(downloadButton);
                            }
                            
                            actionCell.Children.Add(buttonStack);
                            Grid.SetColumn(actionCell, 4);
                            grid.Children.Add(actionCell);
                            
                            // 直接添加Grid到ListView
                            pluginStoreList.Items.Add(grid);
                            AddLog($"已添加插件到商城列表: {plugin.Name}");
                        }
                        
                        AddLog($"插件商城列表已刷新，共 {plugins.Length} 个插件");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"更新插件商城列表UI失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                    }
                });
            }
            else
            {
                AddLog("未找到插件商城列表ListView");
            }
        }
        catch (Exception ex)
        {
            AddLog($"刷新插件商城列表失败: {ex.Message}");
            AddLog($"错误堆栈: {ex.StackTrace}");
        }
        finally
        {
            isRefreshingPluginStore = false;
        }
    }
    
    private async Task RefreshPluginsList()
    {
        // 防止重复调用
        if (isRefreshingPlugins)
        {
            AddLog("插件列表正在刷新中...");
            return;
        }
        
        try
        {
            isRefreshingPlugins = true;
            AddLog("开始刷新插件列表...");
            
            // 显示当前工作目录
            AddLog($"当前工作目录: {Environment.CurrentDirectory}");
            AddLog($"应用程序基目录: {AppDomain.CurrentDomain.BaseDirectory}");
            
            // 确保插件目录存在
            string pluginsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            AddLog($"插件目录: {pluginsPath}");
            if (!System.IO.Directory.Exists(pluginsPath))
            {
                System.IO.Directory.CreateDirectory(pluginsPath);
                AddLog("插件目录不存在，已创建");
            }
            else
            {
                AddLog("插件目录已存在");
            }
            
            // 列出插件目录中的文件
            var pluginFiles = System.IO.Directory.GetFiles(pluginsPath);
            AddLog($"插件目录中的文件数量: {pluginFiles.Length}");
            foreach (var file in pluginFiles)
            {
                AddLog($"插件文件: {System.IO.Path.GetFileName(file)}");
            }
            
            // 找到插件列表ListView
            AddLog("开始查找插件列表ListView");
            var pluginsList = FindVisualChild<ListView>(PluginsPage);
            AddLog($"查找结果: pluginsList = {pluginsList != null}");
            if (pluginsList != null)
            {
                // 使用FanNEL的PluginMessage获取插件列表
                var plugins = await Task.Run(() => {
                    try
                    {
                        AddLog("开始调用PluginMessage.GetPluginListSafe()");
                        var result = Aite.Core.Message.PluginMessage.GetPluginListSafe();
                        AddLog($"获取插件列表成功，共 {result.Count} 个插件");
                        foreach (var plugin in result)
                        {
                            AddLog($"插件: {plugin.Name} (ID: {plugin.Id}, 版本: {plugin.Version}, 作者: {plugin.Author}, 状态: {plugin.Status})");
                        }
                        return result;
                    }
                    catch (Exception ex)
                    {
                        AddLog($"获取插件列表失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                        return new List<Aite.Core.Entities.NEL.EntityPluginState>();
                    }
                });
                
                // 在UI线程中更新列表
                await Dispatcher.InvokeAsync(() => {
                    try
                    {
                        // 清空现有列表
                        pluginsList.Items.Clear();
                        AddLog("已清空现有插件列表");
                        
                        // 添加到列表
                        foreach (var plugin in plugins)
                        {
                            // 创建一个包含所有信息的Grid
                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                            
                            // 名称
                            var nameText = new TextBlock { Text = plugin.Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(nameText, 0);
                            grid.Children.Add(nameText);
                            
                            // 版本
                            var versionText = new TextBlock { Text = plugin.Version, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(versionText, 1);
                            grid.Children.Add(versionText);
                            
                            // 作者
                            var authorText = new TextBlock { Text = plugin.Author, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            Grid.SetColumn(authorText, 2);
                            grid.Children.Add(authorText);
                            
                            // 状态
                            bool isEnabled = plugin.Status == "1";
                            var statusText = new TextBlock { Text = isEnabled ? "启动" : "未启动", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                            statusText.Foreground = isEnabled ? Brushes.Green : Brushes.Red;
                            Grid.SetColumn(statusText, 3);
                            grid.Children.Add(statusText);
                            
                            // 操作按钮
                            var actionCell = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 5, 0), Width = double.NaN };
                            
                            // 启动/停止按钮
                            var toggleButton = new Button { Content = isEnabled ? "停止" : "启动", Width = 60, Margin = new Thickness(0, 0, 5, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                            toggleButton.Click += async (sender, e) => {
                                try
                                {
                                    toggleButton.Content = "操作中...";
                                    toggleButton.IsEnabled = false;
                                    
                                    AddLog($"尝试{(isEnabled ? "停止" : "启动")}插件: {plugin.Name}");
                                    
                                    // 使用FanNEL的PluginMessage切换插件状态
                                    await Task.Run(() => {
                                        try
                                        {
                                            Aite.Core.Message.PluginMessage.TogglePlugin(plugin.Id);
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception($"切换插件状态失败: {ex.Message}");
                                        }
                                    });
                                    
                                    AddLog($"插件 {plugin.Name} {(isEnabled ? "停止" : "启动")}成功");
                                    // 刷新插件列表
                                    await RefreshPluginsList();
                                }
                                catch (Exception ex)
                                {
                                    AddLog($"{(isEnabled ? "停止" : "启动")}插件失败: {ex.Message}");
                                }
                                finally
                                {
                                    toggleButton.Content = isEnabled ? "停止" : "启动";
                                    toggleButton.IsEnabled = true;
                                }
                            };
                            buttonStack.Children.Add(toggleButton);
                            
                            // 删除按钮
                            var deleteButton = new Button { Content = "删除", Width = 60, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 12 };
                            deleteButton.Click += async (sender, e) => {
                                try
                                {
                                    // 显示确认对话框
                                    var result = CustomMessageBox.Show($"确定要删除这个插件吗？此操作不可恢复。", "删除确认", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                                    if (result == MessageBoxResult.OK)
                                    {
                                        deleteButton.Content = "删除中...";
                                        deleteButton.IsEnabled = false;
                                        
                                        AddLog($"开始删除插件: {plugin.Name}");
                                        
                                        // 使用FanNEL的PluginMessage删除插件
                                        await Task.Run(() => {
                                            try
                                            {
                                                Aite.Core.Message.PluginMessage.DeletePlugin(plugin.Id);
                                            }
                                            catch (Exception ex)
                                            {
                                                throw new Exception($"删除插件失败: {ex.Message}");
                                            }
                                        });
                                        
                                        AddLog($"插件 {plugin.Name} 删除成功");
                                        // 刷新插件列表
                                        await RefreshPluginsList();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AddLog($"删除插件失败: {ex.Message}");
                                }
                                finally
                                {
                                    deleteButton.Content = "删除";
                                    deleteButton.IsEnabled = true;
                                }
                            };
                            buttonStack.Children.Add(deleteButton);
                            
                            actionCell.Children.Add(buttonStack);
                            Grid.SetColumn(actionCell, 4);
                            grid.Children.Add(actionCell);
                            
                            // 直接添加Grid到ListView
                            pluginsList.Items.Add(grid);
                            AddLog($"已添加插件到列表: {plugin.Name}");
                        }
                        
                        AddLog($"插件列表已刷新，共 {plugins.Count} 个插件");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"更新插件列表UI失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                    }
                });
            }
            else
            {
                AddLog("未找到插件列表ListView");
            }
        }
        catch (Exception ex)
        {
            AddLog($"刷新插件列表失败: {ex.Message}");
            AddLog($"错误堆栈: {ex.StackTrace}");
        }
        finally
        {
            isRefreshingPlugins = false;
        }
    }
    
    /// <summary>
    /// 比较两个版本号的大小
    /// </summary>
    /// <param name="version1">版本号1</param>
    /// <param name="version2">版本号2</param>
    /// <returns>如果version1大于version2返回1，等于返回0，小于返回-1</returns>
    private int CompareVersions(string version1, string version2)
    {
        try
        {
            string[] v1 = version1.Split('.');
            string[] v2 = version2.Split('.');
            
            int maxLength = Math.Max(v1.Length, v2.Length);
            
            for (int i = 0; i < maxLength; i++)
            {
                int num1 = i < v1.Length ? int.TryParse(v1[i], out int n1) ? n1 : 0 : 0;
                int num2 = i < v2.Length ? int.TryParse(v2[i], out int n2) ? n2 : 0 : 0;
                
                if (num1 > num2)
                    return 1;
                else if (num1 < num2)
                    return -1;
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AddLog($"版本比较错误: {ex.Message}");
            // 出错时默认认为需要更新
            return 1;
        }
    }
    
    /// <summary>
    /// 搜索服务器
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    private async Task SearchServers(string keyword)
    {
        try
        {
            AddLog($"开始搜索服务器: {keyword}");
            
            // 检查是否已登录
            var loggedInAccounts = Aite.Core.Message.AccountMessage.GetLoginAccountList();
            if (loggedInAccounts.Length == 0)
            {
                AddLog("未登录任何账号，请先登录");
                return;
            }
            
            // 获取服务器列表
            var servers = await Task.Run(async () => {
                try
                {
                    var result = await Aite.Core.Message.ServersGameMessage.GetServerList(0, 100);
                    AddLog($"获取服务器列表成功，共 {result.Length} 个服务器");
                    return result;
                }
                catch (Exception ex)
                {
                    AddLog($"获取服务器列表失败: {ex.Message}");
                    return new WPFLauncherApi.Entities.EntitiesWPFLauncher.NetGame.EntityNetGameItem[0];
                }
            });
            
            // 过滤服务器
            var filteredServers = servers.Where(server => {
                // 检查服务器名称是否包含关键词
                if (server.Name != null && server.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                // 检查服务器描述是否包含关键词
                if (server.BriefSummary != null && server.BriefSummary.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            }).ToArray();
            
            AddLog($"搜索结果: {filteredServers.Length} 个服务器");
            
            // 找到服务器列表WrapPanel
            var serversWrapPanel = FindVisualChild<WrapPanel>(ServersPage);
            if (serversWrapPanel == null)
            {
                var border = ServersPage.Children[0] as Border;
                if (border != null)
                {
                    var stackPanel = border.Child as StackPanel;
                    if (stackPanel != null)
                    {
                        // 先查找ScrollViewer
                        var scrollViewer = stackPanel.Children.OfType<ScrollViewer>().FirstOrDefault();
                        if (scrollViewer != null)
                        {
                            // 从ScrollViewer中查找WrapPanel
                            serversWrapPanel = scrollViewer.Content as WrapPanel;
                        }
                        // 如果没有找到ScrollViewer，直接查找WrapPanel（兼容旧布局）
                        if (serversWrapPanel == null)
                        {
                            serversWrapPanel = stackPanel.Children.OfType<WrapPanel>().FirstOrDefault();
                        }
                    }
                }
            }
            
            if (serversWrapPanel != null)
            {
                // 清空现有项
                serversWrapPanel.Children.Clear();
                
                // 计算卡片宽度，一行显示4个
                serversWrapPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double availableWidth = serversWrapPanel.DesiredSize.Width;
                // 确保availableWidth不为负数
                availableWidth = Math.Max(availableWidth, 800); // 设置最小宽度
                double cardWidth = (availableWidth - 96) / 4; // 96 = 12*2*4 (margin * 2 * 4 gaps)
                // 确保cardWidth不为负数
                cardWidth = Math.Max(cardWidth, 150); // 设置最小卡片宽度
                double cardHeight = cardWidth * 0.9; // 增加高度，确保内容显示完全
                
                // 添加过滤后的服务器
                foreach (var server in filteredServers)
                {
                    // 创建服务器卡片
                    var serverCard = new Border {
                        Margin = new Thickness(12),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                        Width = cardWidth,
                        Height = cardHeight, // 使用计算的卡片高度
                        Cursor = Cursors.Hand // 设置鼠标指针为手形
                    };
                    
                    // 创建卡片内容
                    var cardStack = new StackPanel {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    // 添加服务器图标
                    var image = new Image {
                        Width = cardWidth - 24,
                        Height = (cardWidth - 24) * 0.7,
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    
                    // 尝试加载服务器图标
                    try
                    {
                        if (!string.IsNullOrEmpty(server.TitleImageUrl) && server.TitleImageUrl.Contains("http"))
                        {
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.UriSource = new Uri(server.TitleImageUrl);
                            bitmapImage.EndInit();
                            image.Source = bitmapImage;
                        }
                        else
                        {
                            // 使用默认图标
                            image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_icon.png"));
                        }
                    }
                    catch
                    {
                        // 使用默认图标
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_icon.png"));
                    }
                    cardStack.Children.Add(image);
                    
                    // 添加服务器名称
                    var nameText = new TextBlock {
                        Text = server.Name,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(8, 0, 8, 4),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"))
                    };
                    cardStack.Children.Add(nameText);
                    
                    // 添加在线人数
                    var onlineText = new TextBlock {
                        Text = $"在线: {server.OnlineCount ?? "0"}",
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(8, 0, 8, 8),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
                    };
                    cardStack.Children.Add(onlineText);
                    
                    serverCard.Child = cardStack;
                    
                    // 添加点击事件，显示服务器详情
                    serverCard.MouseLeftButtonDown += (sender, e) => {
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            ShowServerDetailPage(server);
                        }
                    };
                    
                    serversWrapPanel.Children.Add(serverCard);
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"搜索服务器失败: {ex.Message}");
        }
    }
    
    // 存储喜爱的服务器
    private List<string> favoriteServers = new List<string>();
    private string favoriteServersFilePath = System.IO.Path.Combine(PathUtil.CachePath, "favorite_servers.json");
    
    // 加载喜爱的服务器列表
    private void LoadFavoriteServers()
    {
        try
        {
            if (File.Exists(favoriteServersFilePath))
            {
                string json = File.ReadAllText(favoriteServersFilePath);
                favoriteServers = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            AddLog($"加载喜爱服务器列表失败: {ex.Message}");
            favoriteServers = new List<string>();
        }
    }
    
    // 保存喜爱的服务器列表
    private void SaveFavoriteServers()
    {
        try
        {
            string json = JsonSerializer.Serialize(favoriteServers);
            File.WriteAllText(favoriteServersFilePath, json);
        }
        catch (Exception ex)
        {
            AddLog($"保存喜爱服务器列表失败: {ex.Message}");
        }
    }
    
    // 检查服务器是否被喜爱
    private bool IsServerFavorite(string serverId)
    {
        return favoriteServers.Contains(serverId);
    }
    
    // 切换服务器喜爱状态
    private void ToggleServerFavorite(string serverId)
    {
        if (favoriteServers.Contains(serverId))
        {
            favoriteServers.Remove(serverId);
        }
        else
        {
            favoriteServers.Add(serverId);
        }
        SaveFavoriteServers();
    }
    
    private async void ShowServerDetailPage(WPFLauncherApi.Entities.EntitiesWPFLauncher.NetGame.EntityNetGameItem server)
    {
        try
        {
            // 加载喜爱的服务器列表
            LoadFavoriteServers();
            
            // 创建服务器详情页面
            var ServerDetailPage = new Grid();
            ServerDetailPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            ServerDetailPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            ServerDetailPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            ServerDetailPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // 服务器标题区域
            var headerBorder = new Border { Margin = new Thickness(20, 20, 20, 10) };
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            
            // 返回按钮
            var backButton = new Button { Content = "<", Width = 36, Height = 36, Background = Brushes.Transparent, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), FontSize = 20, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
            backButton.Click += (sender, e) => {
                // 返回到服务器列表页面
                ContentArea.Content = ServersPage;
                AddLog("导航到网络器管理", false);
            };
            Grid.SetColumn(backButton, 0);
            headerGrid.Children.Add(backButton);
            
            // 服务器标题和ID
            var titleStack = new StackPanel {
                HorizontalAlignment = HorizontalAlignment.Left, // 确保标题靠左显示
                Children = {
                    new TextBlock { 
                        Text = server.Name, 
                        FontSize = 24, 
                        FontWeight = FontWeights.Bold, 
                        Margin = new Thickness(10, 0, 0, 5), 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")),
                        TextWrapping = TextWrapping.NoWrap,
                        TextTrimming = TextTrimming.CharacterEllipsis, // 文本过长时显示省略号
                        MaxWidth = 400 // 设置最大宽度，防止服务器名称过长
                    },
                    new TextBlock { 
                        Text = $"服务器ID: {server.EntityId}", 
                        FontSize = 14, 
                        Margin = new Thickness(10, 0, 0, 0), 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
                    }
                }
            };
            Grid.SetColumn(titleStack, 1);
            headerGrid.Children.Add(titleStack);
            
            // 按钮区域容器
            var buttonsGrid = new Grid();
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            buttonsGrid.HorizontalAlignment = HorizontalAlignment.Right;
            
            // 资源按钮
            var resourceButton = new Button { Content = "资源", Width = 90, Height = 40, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            resourceButton.Click += (sender, e) => {
                try
                {
                    // 构建服务器缓存目录路径
                    string runDir = AppDomain.CurrentDomain.BaseDirectory;
                    string serverId = server.EntityId;
                    string resourceDir = System.IO.Path.Combine(runDir, ".game_cache", "Game", serverId, ".minecraft");
                    
                    AddLog($"资源目录路径: {resourceDir}");
                    
                    // 检查目录是否存在
                    if (Directory.Exists(resourceDir))
                    {
                        // 打开目录
                        System.Diagnostics.Process.Start("explorer.exe", resourceDir);
                        AddLog("资源目录已打开");
                    }
                    else
                    {
                        // 目录不存在，提示用户
                        CustomMessageBox.Show("还未下载资源", "提示");
                        AddLog("资源目录不存在");
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"打开资源目录失败: {ex.Message}", "错误");
                    AddLog($"打开资源目录失败: {ex.Message}");
                }
            };
            Grid.SetColumn(resourceButton, 0);
            buttonsGrid.Children.Add(resourceButton);
            
            // 喜爱按钮
            bool isFavorite = IsServerFavorite(server.EntityId);
            var favoriteButton = new Button {
                Content = isFavorite ? "❤️" : "🤍",
                Width = 90,
                Height = 40,
                Background = isFavorite ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4081")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")),
                Foreground = isFavorite ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                BorderThickness = new Thickness(0),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            favoriteButton.Click += (sender, e) => {
                // 切换喜爱状态
                ToggleServerFavorite(server.EntityId);
                bool newFavoriteState = IsServerFavorite(server.EntityId);
                favoriteButton.Content = newFavoriteState ? "❤️" : "🤍";
                favoriteButton.Background = newFavoriteState ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4081")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                favoriteButton.Foreground = newFavoriteState ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                AddLog($"服务器 {server.Name} 喜爱状态已更新: {newFavoriteState}");
            };
            Grid.SetColumn(favoriteButton, 1);
            buttonsGrid.Children.Add(favoriteButton);
            
            // 启动按钮
            var launchButton = new Button { Content = "启动", Width = 90, Height = 40, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 0) };
            launchButton.Click += (sender, e) => {
                ShowLaunchModal(server);
            };
            Grid.SetColumn(launchButton, 2);
            buttonsGrid.Children.Add(launchButton);
            
            // 将按钮容器添加到headerGrid
            Grid.SetColumn(buttonsGrid, 2);
            Grid.SetColumnSpan(buttonsGrid, 3);
            headerGrid.Children.Add(buttonsGrid);
            
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            ServerDetailPage.Children.Add(headerBorder);
            
            // 服务器图片区域
            var imagesBorder = new Border { Margin = new Thickness(20, 10, 20, 20) };
            var imagesStack = new StackPanel();
            
            // 主图片
            var mainImageBorder = new Border { Height = 300, Margin = new Thickness(0, 0, 0, 10), CornerRadius = new CornerRadius(5), OverridesDefaultStyle = true };
            var mainImage = new Image { Stretch = Stretch.UniformToFill };
            try
            {
                if (!string.IsNullOrEmpty(server.TitleImageUrl) && server.TitleImageUrl.Contains("http"))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(server.TitleImageUrl);
                    bitmapImage.EndInit();
                    mainImage.Source = bitmapImage;
                }
                else
                {
                    // 使用默认横幅
                    mainImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_banner.png"));
                }
            }
            catch
            {
                // 使用默认横幅
                mainImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_banner.png"));
            }
            mainImageBorder.Child = mainImage;
            imagesStack.Children.Add(mainImageBorder);
            
            // 小图片（如果有的话）
            var smallImagesWrap = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0), ItemHeight = 48, ItemWidth = 80, HorizontalAlignment = HorizontalAlignment.Left };
            // 这里可以添加小图片，暂时使用默认图片
            for (int i = 0; i < 3; i++)
            {
                var smallImageBorder = new Border { Width = 76, Height = 44, Margin = new Thickness(2), CornerRadius = new CornerRadius(3), BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(2), Cursor = Cursors.Hand };
                var smallImage = new Image { Stretch = Stretch.UniformToFill };
                try
                {
                    if (!string.IsNullOrEmpty(server.TitleImageUrl) && server.TitleImageUrl.Contains("http"))
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(server.TitleImageUrl);
                        bitmapImage.EndInit();
                        smallImage.Source = bitmapImage;
                    }
                    else
                    {
                        smallImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_icon.png"));
                    }
                }
                catch
                {
                    smallImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_icon.png"));
                }
                smallImageBorder.Child = smallImage;
                smallImageBorder.MouseLeftButtonDown += (sender, e) => {
                    // 切换主图片
                    var clickedBorder = sender as Border;
                    if (clickedBorder != null && clickedBorder.Child is Image clickedImage)
                    {
                        mainImage.Source = clickedImage.Source;
                        // 重置所有小图片边框
                        foreach (var child in smallImagesWrap.Children)
                        {
                            if (child is Border border)
                            {
                                border.BorderBrush = Brushes.Transparent;
                            }
                        }
                        // 设置当前选中的小图片边框
                        clickedBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    }
                };
                smallImagesWrap.Children.Add(smallImageBorder);
            }
            imagesStack.Children.Add(smallImagesWrap);
            
            imagesBorder.Child = imagesStack;
            Grid.SetRow(imagesBorder, 1);
            ServerDetailPage.Children.Add(imagesBorder);
            
            // 服务器元数据区域
            var metaBorder = new Border { Margin = new Thickness(20, 10, 20, 20), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA")), CornerRadius = new CornerRadius(8), Padding = new Thickness(15) };
            var metaGrid = new Grid();
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // 服务器作者
            var authorStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "服务器作者:", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                    new TextBlock { Text = "加载中...", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) }
                }
            };
            Grid.SetColumn(authorStack, 0);
            metaGrid.Children.Add(authorStack);
            
            // 在线人数
            var onlineStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "在线人数:", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                    new TextBlock { Text = server.OnlineCount ?? "0/0", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) }
                }
            };
            Grid.SetColumn(onlineStack, 1);
            metaGrid.Children.Add(onlineStack);
            
            // 游戏版本
            var versionStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "游戏版本:", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                    new TextBlock { Text = "加载中...", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) }
                }
            };
            Grid.SetColumn(versionStack, 2);
            metaGrid.Children.Add(versionStack);
            
            // 服务器地址
            var addressStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "服务器地址:", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                    new TextBlock { Text = "加载中...", FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) }
                }
            };
            Grid.SetColumn(addressStack, 3);
            metaGrid.Children.Add(addressStack);
            
            metaBorder.Child = metaGrid;
            Grid.SetRow(metaBorder, 2);
            ServerDetailPage.Children.Add(metaBorder);
            
            // 服务器介绍区域
            var descriptionBorder = new Border { Margin = new Thickness(20, 10, 20, 20) };
            var descriptionStack = new StackPanel {
                Children = {
                    new TextBlock { Text = "服务器介绍", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) },
                    new TextBlock { Name = "DescriptionTextBlock", Height = 300, Margin = new Thickness(0, 0, 0, 0), FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), TextWrapping = TextWrapping.Wrap }
                }
            };
            descriptionBorder.Child = descriptionStack;
            Grid.SetRow(descriptionBorder, 3);
            ServerDetailPage.Children.Add(descriptionBorder);
            
            // 初始化TextBlock内容
            if (descriptionStack.Children[1] is TextBlock descriptionText)
            {
                string htmlContent = !string.IsNullOrEmpty(server.BriefSummary) ? server.BriefSummary : "暂无介绍";
                AddLog($"初始化服务器介绍，内容长度: {htmlContent.Length}");
                // 处理HTML内容，将<>替换为空格
                string plainText = RemoveHtmlTags(htmlContent);
                descriptionText.Text = plainText;
                AddLog("TextBlock内容已更新");
            }
            
            // 显示服务器详情页面
            ContentArea.Content = ServerDetailPage;
            AddLog($"导航到服务器详情页面: {server.Name}");
            
            // 检查服务器插件依赖
            AddLog($"检查服务器 {server.Name} 的插件依赖...");
            // 在后台线程中执行，避免阻塞UI线程
            _ = Task.Run(async () => {
                try
                {
                    AddLog($"开始检查服务器 {server.Name} 的插件依赖");
                    
                    // 确保插件目录存在
                    string pluginsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
                    AddLog($"插件目录: {pluginsPath}");
                    if (!System.IO.Directory.Exists(pluginsPath))
                    {
                        System.IO.Directory.CreateDirectory(pluginsPath);
                        AddLog("插件目录不存在，已创建");
                    }
                    
                    // 提前加载插件商店列表，确保插件信息已缓存
                    AddLog("正在加载插件商店列表...");
                    try
                    {
                        await Aite.Core.Message.PlugInstoreMessage.GetPluginList(0, 50);
                        AddLog("插件商店列表加载完成");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"加载插件商店列表失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                    }
                    
                    // 获取服务器的插件依赖列表
                    AddLog($"正在获取服务器 {server.Name} 的插件依赖列表...");
                    try
                    {
                        var dependenceList = await Aite.Core.Message.PluginMessage.GetDependenceList(server.EntityId, null);
                        AddLog($"获取到 {dependenceList.Count} 个依赖项");
                        
                        // 提取实际的插件依赖列表
                        var dependencies = new List<Aite.Core.Entities.Aite.EntityDependence2>();
                        foreach (var dependence in dependenceList)
                        {
                            if (dependence.Data != null)
                            {
                                dependencies.AddRange(dependence.Data);
                                AddLog($"依赖项包含 {dependence.Data.Length} 个插件");
                            }
                        }
                        AddLog($"服务器 {server.Name} 依赖 {dependencies.Count} 个插件");
                        
                        // 检查哪些插件未安装
                        var missingPlugins = new List<Aite.Core.Entities.Aite.EntityDependence2>();
                        AddLog("当前已安装的插件列表:");
                        var installedPlugins = Aite.Core.Message.PluginMessage.GetPluginList();
                        foreach (var plugin in installedPlugins)
                        {
                            AddLog($"- {plugin.Name} (ID: {plugin.Id})");
                        }
                        
                        AddLog("检查服务器依赖的插件:");
                        foreach (var dep in dependencies)
                        {
                            AddLog($"- 检查插件: {dep.Name} (ID: {dep.Id})");
                            if (!Aite.Core.Message.PluginMessage.IsPluginExist(dep.Id))
                            {
                                AddLog($"  - 插件未安装: {dep.Name}");
                                missingPlugins.Add(dep);
                            }
                            else
                            {
                                AddLog($"  - 插件已安装: {dep.Name}");
                            }
                        }
                        
                        if (missingPlugins.Count > 0)
                        {
                            AddLog($"发现 {missingPlugins.Count} 个未安装的依赖插件");
                            
                            // 在UI线程中显示消息框
                            Dispatcher.Invoke(() => {
                                try
                                {
                                    // 提示用户安装
                                    string message = $"服务器 {server.Name} 需要以下插件:\n";
                                    foreach (var plugin in missingPlugins)
                                    {
                                        message += $"- {plugin.Name}\n";
                                    }
                                    message += "\n是否安装这些插件?";
                                    
                                    AddLog("显示插件依赖提示消息框");
                                    if (CustomMessageBox.Show(message, "插件依赖", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.OK)
                                    {
                                        AddLog("用户选择安装插件");
                                        // 安装缺失的插件
                                        _ = Task.Run(async () => {
                                            AddLog("开始安装缺失的插件...");
                                            foreach (var plugin in missingPlugins)
                                            {
                                                try
                                                {
                                                    AddLog($"开始安装插件: {plugin.Name} (ID: {plugin.Id})");
                                                    Aite.Core.Message.PlugInstoreMessage.Install(plugin.Id);
                                                    AddLog($"插件安装成功: {plugin.Name}");
                                                }
                                                catch (Exception ex)
                                                {
                                                    AddLog($"安装插件 {plugin.Name} 失败: {ex.Message}");
                                                    AddLog($"错误堆栈: {ex.StackTrace}");
                                                }
                                            }
                                            AddLog("插件安装过程完成");
                                        });
                                    }
                                    else
                                    {
                                        AddLog("用户选择不安装插件");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AddLog($"显示消息框失败: {ex.Message}");
                                    AddLog($"错误堆栈: {ex.StackTrace}");
                                }
                            });
                        }
                        else
                        {
                            AddLog("所有依赖插件都已安装");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"获取服务器依赖列表失败: {ex.Message}");
                        AddLog($"错误堆栈: {ex.StackTrace}");
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"检查插件依赖失败: {ex.Message}");
                    AddLog($"错误堆栈: {ex.StackTrace}");
                }
            });
            
            // 获取服务器详细信息
            await Task.Run(async () => {
                try
                {
                    // 获取服务器详细信息
                    var serverDetail = await WPFLauncherApi.Protocol.WPFLauncher.GetNetGameDetailByIdAsync(server.EntityId);
                    
                    // 获取服务器地址信息
                    var serverAddress = await WPFLauncherApi.Protocol.WPFLauncher.GetNetGameServerAddressAsync(server.EntityId);
                    
                    // 在UI线程中更新信息
                    await Dispatcher.InvokeAsync(() => {
                        // 更新服务器作者
                        if (authorStack.Children[1] is TextBlock authorText)
                        {
                            authorText.Text = !string.IsNullOrEmpty(serverDetail.DeveloperName) ? serverDetail.DeveloperName : "未知";
                        }
                        
                        // 更新游戏版本
                        if (versionStack.Children[1] is TextBlock versionText)
                        {
                            if (serverDetail.McVersionList != null && serverDetail.McVersionList.Length > 0)
                            {
                                versionText.Text = string.Join(", ", serverDetail.McVersionList.Select(v => v.Name));
                            }
                            else
                            {
                                versionText.Text = "未知";
                            }
                        }
                        
                        // 更新服务器地址
                        if (addressStack.Children[1] is TextBlock addressText)
                        {
                            string address = !string.IsNullOrEmpty(serverAddress.Host) ? serverAddress.Host : serverDetail.ServerAddress;
                            int port = serverAddress.Port > 0 ? serverAddress.Port : serverDetail.ServerPort;
                            
                            if (!string.IsNullOrEmpty(address))
                            {
                                addressText.Text = port > 0 ? $"{address}:{port}" : address;
                            }
                            else
                            {
                                addressText.Text = "未知";
                            }
                        }
                        
                        // 更新服务器介绍
                        if (descriptionStack.Children[1] is TextBlock descriptionText)
                        {
                            string htmlContent = !string.IsNullOrEmpty(serverDetail.DetailDescription) ? serverDetail.DetailDescription : server.BriefSummary ?? "暂无介绍";
                            AddLog($"更新服务器介绍，内容长度: {htmlContent.Length}");
                            // 处理HTML内容，将<>替换为空格
                            string plainText = RemoveHtmlTags(htmlContent);
                            descriptionText.Text = plainText;
                            AddLog("TextBlock内容已更新");
                        }
                    });
                }
                catch (Exception ex)
                {
                    AddLog($"获取服务器详细信息失败: {ex.Message}");
                    // 在UI线程中显示错误信息
                    await Dispatcher.InvokeAsync(() => {
                        // 更新服务器作者
                        if (authorStack.Children[1] is TextBlock authorText)
                        {
                            authorText.Text = "未知";
                        }
                        
                        // 更新游戏版本
                        if (versionStack.Children[1] is TextBlock versionText)
                        {
                            versionText.Text = "未知";
                        }
                        
                        // 更新服务器地址
                        if (addressStack.Children[1] is TextBlock addressText)
                        {
                            addressText.Text = "未知";
                        }
                    });
                }
            });
        }
        catch (Exception ex)
        {
            AddLog($"显示服务器详情页面失败: {ex.Message}");
            CustomMessageBox.Show($"显示服务器详情页面失败: {ex.Message}", "错误");
        }
    }
    
    private async void ShowLaunchModal(WPFLauncherApi.Entities.EntitiesWPFLauncher.NetGame.EntityNetGameItem server)
    {
        try
        {
            // 创建启动弹窗
            var modalWindow = new Window
            {
                Title = "启动游戏",
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"))
            };
            
            // 创建弹窗内容
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            
            // 标题
            var titleText = new TextBlock { Text = "启动游戏", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(15, 10, 15, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) };
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);
            
            // 提示信息
            var tipText = new TextBlock { Text = "出现问题可以前往论坛反馈哦~", FontSize = 12, Margin = new Thickness(15, 0, 15, 10), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) };
            Grid.SetRow(tipText, 1);
            grid.Children.Add(tipText);
            
            // 账号选择
            var accountLabel = new TextBlock { Text = "账号: 登录成功后的账号才会显示在账号列表中。", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(15, 0, 15, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) };
            Grid.SetRow(accountLabel, 2);
            grid.Children.Add(accountLabel);
            
            // 先声明roleComboBox，使其在账号选择变更事件中可访问
            var roleComboBox = new ComboBox { Height = 40, IsEditable = true, IsSynchronizedWithCurrentItem = true, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), BorderThickness = new Thickness(1), FontSize = 12 };
            
            var accountComboBox = new ComboBox { Margin = new Thickness(15, 0, 15, 10), Height = 40, IsEditable = true, IsSynchronizedWithCurrentItem = true, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")), BorderThickness = new Thickness(1), FontSize = 12 };
            // 获取实际的账号列表
            var loggedInAccounts = Aite.Core.Message.AccountMessage.GetLoginAccountList();
            AddLog($"获取到 {loggedInAccounts.Length} 个已登录账号");
            foreach (var account in loggedInAccounts)
            {
                string displayName = string.IsNullOrEmpty(account.Account) ? "未命名账号" : account.Account;
                AddLog($"添加账号到列表: {displayName}, Type: {account.Type}");
                // 直接添加字符串作为项
                accountComboBox.Items.Add(displayName);
            }
            if (accountComboBox.Items.Count > 0)
            {
                accountComboBox.SelectedIndex = 0;
                AddLog($"默认选中账号: {accountComboBox.SelectedItem}");
                // 强制更新显示
                accountComboBox.UpdateLayout();
            }
            // 添加账号选择变更事件，当更换账号时刷新角色列表
            accountComboBox.SelectionChanged += async (sender, e) => {
                try
                {
                    AddLog("账号选择变更，切换账号并刷新角色列表");
                    
                    // 获取选中的账号名称
                    string selectedAccountName = accountComboBox.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedAccountName))
                    {
                        // 切换到选中的账号
                        var loggedInAccounts = Aite.Core.Message.AccountMessage.GetLoginAccountList();
                        var selectedAccount = loggedInAccounts.FirstOrDefault(acc => 
                            (string.IsNullOrEmpty(acc.Account) ? "未命名账号" : acc.Account) == selectedAccountName
                        );
                        
                        if (selectedAccount != null && selectedAccount.Id.HasValue)
                        {
                            AddLog($"切换到账号: {selectedAccountName}");
                            // 强制切换账号
                            Aite.Core.Message.AccountMessage.SwitchAccountToForce(selectedAccount.Id.Value);
                            AddLog("账号切换成功");
                            
                            // 等待账号切换完成
                            await Task.Delay(500);
                        }
                    }
                    
                    // 清空现有角色列表
                    roleComboBox.Items.Clear();
                    // 重新获取角色列表
                    var roles = await WPFLauncherApi.Protocol.WPFLauncher.GetNetGameCharactersAsync(server.EntityId);
                    foreach (var role in roles)
                    {
                        // 直接添加角色名称字符串，这样ToString()就能得到正确的角色名称
                        roleComboBox.Items.Add(role.Name);
                    }
                    if (roleComboBox.Items.Count > 0)
                    {
                        roleComboBox.SelectedIndex = 0;
                        // 强制更新显示
                        roleComboBox.UpdateLayout();
                    }
                    AddLog($"角色列表刷新完成，共 {roleComboBox.Items.Count} 个角色");
                    
                    // 确保角色列表更新后，启动代理按钮能够正确获取选中的角色名称
                    roleComboBox.UpdateLayout();
                }
                catch (Exception ex)
                {
                    AddLog($"刷新角色列表失败: {ex.Message}");
                }
            };
            Grid.SetRow(accountComboBox, 3);
            grid.Children.Add(accountComboBox);
            

            
            // 游戏角色选择
            var roleLabel = new TextBlock { Text = "游戏名称: 请先添加 / 选择 游戏名称，才能启动。", FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(15, 10, 15, 5), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) };
            Grid.SetRow(roleLabel, 4);
            grid.Children.Add(roleLabel);
            
            var roleGrid = new Grid { Margin = new Thickness(15, 0, 15, 5) };
            roleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            roleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            
            // 获取实际的角色列表
            try
            {
                var roles = await WPFLauncherApi.Protocol.WPFLauncher.GetNetGameCharactersAsync(server.EntityId);
                foreach (var role in roles)
                {
                    // 直接添加角色名称字符串，这样ToString()就能得到正确的角色名称
                    roleComboBox.Items.Add(role.Name);
                }
                if (roleComboBox.Items.Count > 0)
                {
                    roleComboBox.SelectedIndex = 0;
                    // 强制更新显示
                    roleComboBox.UpdateLayout();
                }
            }
            catch (Exception ex)
            {
                AddLog($"获取角色列表失败: {ex.Message}");
            }
            Grid.SetColumn(roleComboBox, 0);
            roleGrid.Children.Add(roleComboBox);
            
            var addRoleButton = new Button { Content = "添加", Width = 100, Height = 40, Margin = new Thickness(0, 0, 0, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 14, FontWeight = FontWeights.SemiBold };
            addRoleButton.Click += async (sender, e) => {
                try
                {
                    // 显示添加角色对话框
                    var addRoleWindow = new Window
                    {
                        Title = "添加角色",
                        Width = 400,
                        Height = 250,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                        BorderThickness = new Thickness(1)
                    };
                    
                    // 添加边框和圆角
                    var border = new Border
                    {
                        CornerRadius = new CornerRadius(12),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(20)
                    };
                    
                    var addRoleGrid = new Grid();
                    addRoleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    addRoleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    addRoleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    addRoleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    
                    // 标题
                    var titleText = new TextBlock 
                    {
                        Text = "添加角色", 
                        FontSize = 18, 
                        FontWeight = FontWeights.Bold, 
                        Margin = new Thickness(0, 0, 0, 20), 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) 
                    };
                    Grid.SetRow(titleText, 0);
                    addRoleGrid.Children.Add(titleText);
                    
                    // 角色名称标签
                    var roleNameLabel = new TextBlock 
                    {
                        Text = "角色名称:", 
                        FontSize = 14, 
                        FontWeight = FontWeights.SemiBold, 
                        Margin = new Thickness(0, 0, 0, 8), 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")) 
                    };
                    Grid.SetRow(roleNameLabel, 1);
                    addRoleGrid.Children.Add(roleNameLabel);
                    
                    // 角色名称文本框
                    var roleNameTextBox = new TextBox 
                    {
                        Margin = new Thickness(0, 0, 0, 24), 
                        Height = 40,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF")),
                        BorderThickness = new Thickness(1),
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"))
                    };
                    Grid.SetRow(roleNameTextBox, 2);
                    addRoleGrid.Children.Add(roleNameTextBox);
                    
                    // 按钮网格
                    var addRoleButtonGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
                    addRoleButtonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    addRoleButtonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    addRoleButtonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetRow(addRoleButtonGrid, 3);
                    
                    // 随机名称按钮
                    var randomNameButton = new Button 
                    {
                        Content = "随机名称", 
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")), 
                        Foreground = Brushes.White, 
                        BorderThickness = new Thickness(0), 
                        Margin = new Thickness(5),
                        Height = 36,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold
                    };
                    randomNameButton.Click += (s, args) => {
                        roleNameTextBox.Text = GenerateRandomName();
                    };
                    Grid.SetColumn(randomNameButton, 0);
                    addRoleButtonGrid.Children.Add(randomNameButton);
                    
                    // 保存按钮
                    var saveButton = new Button 
                    {
                        Content = "保存", 
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), 
                        Foreground = Brushes.White, 
                        BorderThickness = new Thickness(0), 
                        Margin = new Thickness(5),
                        Height = 36,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold
                    };
                    saveButton.Click += async (s, args) => {
                        try
                        {
                            var roleName = roleNameTextBox.Text.Trim();
                            if (string.IsNullOrEmpty(roleName))
                            {
                                CustomMessageBox.Show("角色名称不能为空", "错误");
                                return;
                            }
                            
                            // 创建角色
                            await WPFLauncherApi.Protocol.WPFLauncher.CreateCharacterAsync(server.EntityId, roleName);
                            
                            // 防止缓存
                            await Aite.Core.Message.ServersGameMessage.GetUserName(server.EntityId, roleName);
                            
                            // 刷新角色列表
                            roleComboBox.Items.Clear();
                            var roles = await WPFLauncherApi.Protocol.WPFLauncher.GetNetGameCharactersAsync(server.EntityId);
                            int newRoleIndex = -1;
                            for (int i = 0; i < roles.Length; i++)
                            {
                                var role = roles[i];
                                // 直接添加角色名称字符串，这样ToString()就能得到正确的角色名称
                                roleComboBox.Items.Add(role.Name);
                                if (role.Name == roleName)
                                {
                                    newRoleIndex = i;
                                }
                            }
                            if (roleComboBox.Items.Count > 0)
                            {
                                // 尝试选中刚刚创建的角色
                                if (newRoleIndex >= 0)
                                {
                                    roleComboBox.SelectedIndex = newRoleIndex;
                                }
                                else
                                {
                                    roleComboBox.SelectedIndex = 0;
                                }
                                // 强制更新显示
                                roleComboBox.UpdateLayout();
                            }
                            
                            // 刷新服务器信息，确保创建代理时使用最新的服务器数据
                            try
                            {
                                var updatedServer = await WPFLauncherApi.Protocol.WPFLauncher.GetNetGameDetailByIdAsync(server.EntityId);
                                if (updatedServer != null)
                                {
                                    AddLog("服务器信息已更新，确保创建代理时使用最新数据");
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLog($"更新服务器信息失败: {ex.Message}");
                            }
                            
                            addRoleWindow.Close();
                            CustomMessageBox.Show("角色创建成功", "成功");
                        }
                        catch (Exception ex)
                        {
                            AddLog($"创建角色失败: {ex.Message}");
                            CustomMessageBox.Show($"创建角色失败: {ex.Message}", "错误");
                        }
                    };
                    Grid.SetColumn(saveButton, 1);
                    addRoleButtonGrid.Children.Add(saveButton);
                    
                    // 取消按钮
                    var cancelAddButton = new Button 
                    {
                        Content = "取消", 
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336")), 
                        Foreground = Brushes.White, 
                        BorderThickness = new Thickness(0), 
                        Margin = new Thickness(5),
                        Height = 36,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold
                    };
                    cancelAddButton.Click += (s, args) => {
                        addRoleWindow.Close();
                    };
                    Grid.SetColumn(cancelAddButton, 2);
                    addRoleButtonGrid.Children.Add(cancelAddButton);
                    
                    Grid.SetRow(addRoleButtonGrid, 3);
                    addRoleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    addRoleGrid.Children.Add(addRoleButtonGrid);
                    
                    border.Child = addRoleGrid;
                    addRoleWindow.Content = border;
                    addRoleWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    AddLog($"显示添加角色对话框失败: {ex.Message}");
                    CustomMessageBox.Show($"显示添加角色对话框失败: {ex.Message}", "错误");
                }
            };
            Grid.SetColumn(addRoleButton, 1);
            roleGrid.Children.Add(addRoleButton);
            
            Grid.SetRow(roleGrid, 5);
            grid.Children.Add(roleGrid);
            

            
            // 按钮区域
            var buttonGrid = new Grid { Margin = new Thickness(15, 5, 15, 15) };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var launchProxyButton = new Button { Content = "启动代理", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(5), Height = 35 };
            launchProxyButton.Click += async (sender, e) => {
                try
                {
                    if (accountComboBox.SelectedItem == null)
                    {
                        CustomMessageBox.Show("请选择账号", "错误");
                        return;
                    }
                    
                    if (roleComboBox.SelectedItem == null)
                    {
                        CustomMessageBox.Show("请选择游戏角色", "错误");
                        return;
                    }
                    
                    launchProxyButton.Content = "启动中...";
                    launchProxyButton.IsEnabled = false;
                    
                    AddLog($"开始启动代理服务器: {server.Name}");
                    
                    // 使用FanNEL的ProxiesMessage启动代理
                    string roleName = "";
                    if (roleComboBox.SelectedItem != null)
                    {
                        roleName = roleComboBox.SelectedItem.ToString();
                    }
                    
                    // 在后台线程中执行启动代理操作，避免UI线程卡死
                    var proxyBase = await Task.Run(async () => {
                        return await Aite.Core.Message.ProxiesMessage.StartProxyAsync(server.EntityId, roleName);
                    });
                    
                    int port = 25565;
                    if (proxyBase is Aite.Core.Entities.NEL.EntityProxy entityProxy)
                    {
                        port = entityProxy.Interceptor.LocalPort;
                    }
                    else if (proxyBase is Aite.Core.Entities.NEL.RunningProxy runningProxy)
                    {
                        port = 25565;
                    }
                    
                    // 保存代理信息
                    var proxyInfo = new ProxyInfo
                    {
                        ServerId = server.EntityId,
                        RoleName = roleName,
                        ServerName = server.Name,
                        Port = port.ToString() // 使用实际的端口号
                    };
                    // 确保线程安全
                    lock (_runningProxies)
                    {
                        _runningProxies.Add(proxyInfo);
                    }
                    AddLog($"保存代理信息: {proxyInfo.ServerName} - {proxyInfo.RoleName}");
                    
                    // 刷新代理列表，显示新启动的代理
                    await RefreshProxiesList();
                    
                    AddLog($"代理服务器 {server.Name} 启动成功");
                    CustomMessageBox.Show($"代理服务器 {server.Name} 启动成功", "成功");
                    modalWindow.Close();
                }
                catch (Exception ex)
                {
                    AddLog($"启动代理服务器失败: {ex.Message}");
                    AddLog($"错误堆栈: {ex.StackTrace}");
                    CustomMessageBox.Show($"启动代理服务器失败: {ex.Message}", "错误");
                }
                finally
                {
                    launchProxyButton.Content = "启动代理";
                    launchProxyButton.IsEnabled = true;
                }
            };
            Grid.SetColumn(launchProxyButton, 0);
            buttonGrid.Children.Add(launchProxyButton);
            
            var cancelButton = new Button { Content = "取消", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(5), Height = 35 };
            cancelButton.Click += (sender, e) => {
                modalWindow.Close();
            };
            Grid.SetColumn(cancelButton, 1);
            buttonGrid.Children.Add(cancelButton);
            
            Grid.SetRow(buttonGrid, 6);
            grid.Children.Add(buttonGrid);
            
            modalWindow.Content = grid;
            modalWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            AddLog($"显示启动弹窗失败: {ex.Message}");
            CustomMessageBox.Show($"显示启动弹窗失败: {ex.Message}", "错误");
        }
    }
    
    private async Task RefreshServersList()
    {
        if (isRefreshingServers)
        {
            AddLog("服务器列表正在刷新中...");
            return;
        }
        
        try
        {
            isRefreshingServers = true;
            serversOffset = 0;
            AddLog("开始刷新服务器列表...");
            
            LoadFavoriteServers();
            
            var loggedInAccounts = Aite.Core.Message.AccountMessage.GetLoginAccountList();
            if (loggedInAccounts.Length == 0)
            {
                AddLog("未登录任何账号，请先登录");
                return;
            }
            
            AddLog($"已登录账号数量: {loggedInAccounts.Length}");
            
            var serversWrapPanel = FindVisualChild<WrapPanel>(ServersPage);
            if (serversWrapPanel == null)
            {
                var border = ServersPage.Children[0] as Border;
                if (border != null)
                {
                    var stackPanel = border.Child as StackPanel;
                    if (stackPanel != null)
                    {
                        var scrollViewer = stackPanel.Children.OfType<ScrollViewer>().FirstOrDefault();
                        if (scrollViewer != null)
                        {
                            serversWrapPanel = scrollViewer.Content as WrapPanel;
                        }
                        if (serversWrapPanel == null)
                        {
                            serversWrapPanel = stackPanel.Children.OfType<WrapPanel>().FirstOrDefault();
                        }
                    }
                }
            }
            
            if (serversWrapPanel == null)
            {
                AddLog("未找到服务器列表WrapPanel");
                return;
            }
            
            await Dispatcher.InvokeAsync(() => {
                serversWrapPanel.Children.Clear();
            });
            
            serversWrapPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double availableWidth = serversWrapPanel.DesiredSize.Width;
            availableWidth = Math.Max(availableWidth, 800);
            double cardWidth = (availableWidth - 96) / 4;
            cardWidth = Math.Max(cardWidth, 150);
            double cardHeight = cardWidth * 0.9;
            
            await LoadServersInBatches(serversWrapPanel, cardWidth, cardHeight);
        }
        catch (Exception ex)
        {
            AddLog($"获取服务器列表失败: {ex.Message}");
        }
        finally
        {
            isRefreshingServers = false;
        }
    }
    
    private async Task LoadServersInBatches(WrapPanel serversWrapPanel, double cardWidth, double cardHeight)
    {
        bool loading = true;
        while (true)
        {
            if (!isServersPageActive)
            {
                await Task.Delay(1000);
                continue;
            }
            
            bool ok = await LoadMoreServers(serversWrapPanel, cardWidth, cardHeight, 12, loading);
            loading = false;
            
            if (!ok)
            {
                break;
            }
            
            await Task.Delay(700);
        }
    }
    
    private async Task<bool> LoadMoreServers(WrapPanel serversWrapPanel, double cardWidth, double cardHeight, int pageSize, bool throwError = true)
    {
        try
        {
            // AddLog($"正在加载服务器列表，偏移量: {serversOffset}, 每页: {pageSize}");
            
            var servers = await Task.Run(async () => {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var result = await Aite.Core.Message.ServersGameMessage.GetServerList(serversOffset, pageSize);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        AddLog($"第 {i+1} 次获取失败: {ex.Message}");
                        if (i < 2)
                        {
                            await Task.Delay(500);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                throw new Exception("获取服务器列表失败");
            });
            
            if (servers == null || servers.Length == 0)
            {
                AddLog("没有更多服务器可加载");
                return false;
            }
            
            AddLog($"获取到 {servers.Length} 个服务器");
            
            var onlineServers = servers.Where(server => {
                if (int.TryParse(server.OnlineCount, out int onlineCount))
                {
                    return onlineCount > 0;
                }
                return true;
            }).ToArray();
            
            var sortedServers = onlineServers.OrderByDescending(server => IsServerFavorite(server.EntityId)).ToArray();
            
            await Dispatcher.InvokeAsync(() => {
                foreach (var server in sortedServers)
                {
                    var serverCard = new Border {
                        Margin = new Thickness(12),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                        Width = cardWidth,
                        Height = cardHeight,
                        Cursor = Cursors.Hand
                    };
                    
                    var cardGrid = new Grid();
                    cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    
                    bool isFavorite = IsServerFavorite(server.EntityId);
                    
                    if (isFavorite)
                    {
                        serverCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4081"));
                        serverCard.BorderThickness = new Thickness(2);
                    }
                    
                    var cardStack = new StackPanel {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    var image = new Image {
                        Width = cardWidth - 24,
                        Height = (cardWidth - 24) * 0.7,
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    
                    try
                    {
                        if (!string.IsNullOrEmpty(server.TitleImageUrl) && server.TitleImageUrl.Contains("http"))
                        {
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.UriSource = new Uri(server.TitleImageUrl);
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.EndInit();
                            image.Source = bitmapImage;
                        }
                        else
                        {
                            image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_icon.png"));
                        }
                    }
                    catch
                    {
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/default_server_icon.png"));
                    }
                    
                    cardStack.Children.Add(image);
                    
                    var serverName = new TextBlock {
                        Text = server.Name,
                        FontSize = Math.Max(10, cardWidth / 14),
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(8, 0, 8, 4),
                        TextWrapping = TextWrapping.Wrap
                    };
                    cardStack.Children.Add(serverName);
                    
                    var onlineText = new TextBlock {
                        Text = $"在线: {server.OnlineCount ?? "0"}",
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(8, 0, 8, 8),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
                    };
                    cardStack.Children.Add(onlineText);
                    
                    Grid.SetRow(cardStack, 0);
                    cardGrid.Children.Add(cardStack);
                    
                    serverCard.MouseLeftButtonDown += (sender, e) => {
                        AddLog($"点击服务器: {server.Name}");
                        ShowServerDetailPage(server);
                    };
                    
                    serverCard.Child = cardGrid;
                    serversWrapPanel.Children.Add(serverCard);
                }
            });
            
            serversOffset += pageSize;
            // AddLog($"已加载服务器总数: {serversWrapPanel.Children.Count}");
            
            return true;
        }
        catch (Exception ex)
        {
            if (throwError)
            {
                AddLog($"加载服务器列表失败: {ex.Message}");
            }
            return false;
        }
    }

    // 查找视觉子元素
    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
                return t;
            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AddLog("应用程序关闭");
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"关闭应用程序失败: {ex.Message}", "错误");
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }




}