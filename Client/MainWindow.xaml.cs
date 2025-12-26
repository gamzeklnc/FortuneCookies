using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using FortuneCookie.Client.Services;
using FortuneCookie.Shared;

namespace FortuneCookie.Client
{
    public partial class MainWindow : Window
    {
        private NetworkService _networkService;
        private string? _username; // Fixed: Nullable
        private Storyboard _crackAnimation;
        private Storyboard _resetAnimation;

        public MainWindow()
        {
            InitializeComponent();
            _networkService = new NetworkService();
            _networkService.OnLoginSuccess += OnLoginSuccess;
            _networkService.OnLoginFailed += (msg) => MessageBox.Show(msg);
            
            _networkService.OnRegisterSuccess += (msg) => 
            {
                Dispatcher.Invoke(() => 
                {
                    MessageBox.Show(msg);
                    GoToLogin_Click(this, new RoutedEventArgs()); // Fixed: Non-null args
                    LoginUsernameBox.Text = RegUsernameBox.Text;
                });
            };
            
            _networkService.OnRegisterFailed += (msg) => MessageBox.Show(msg);
            
            _networkService.OnFortuneReceived += OnFortuneReceived;
            _networkService.OnHistoryReceived += OnHistoryReceived;
            _networkService.OnBroadcastReceived += OnBroadcastReceived;
            _networkService.OnUserListReceived += OnUserListReceived;
            _networkService.OnMessageReceived += OnMessageReceived;
            _networkService.OnMyFortunesReceived += OnMyFortunesReceived;
            
            _crackAnimation = (Storyboard)FindResource("CrackAnimation");
            _resetAnimation = (Storyboard)FindResource("ResetAnimation");
        }

        // Navigation
        private void GoToRegister_Click(object sender, RoutedEventArgs e)
        {
            LoginGrid.Visibility = Visibility.Collapsed;
            RegisterGrid.Visibility = Visibility.Visible;
        }

        private void GoToLogin_Click(object sender, RoutedEventArgs e)
        {
            RegisterGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
        }
        
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
            _username = null;
            ChatListBox.Items.Clear();
            UserListBox.ItemsSource = null;
        }

        // Auth
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _username = LoginUsernameBox.Text;
            string password = LoginPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(password)) return;
             await _networkService.ConnectAsync(_username, password);
        }
        
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
             string u = RegUsernameBox.Text;
             string p = RegPasswordBox.Password;
             if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p)) return;
             await _networkService.RegisterAsync(u, p);
        }

        private void OnLoginSuccess(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LoginGrid.Visibility = Visibility.Collapsed;
                MainGrid.Visibility = Visibility.Visible;
                WelcomeText.Text = $"Merhaba, {_username}";
                NotificationText.Text = message;
                ResetCookieState();
            });
        }
        
        // Simulation Logic
        // Simulation Logic
        private void ResetCookieState()
        {
            // Reset to Frame 1
            var image = new BitmapImage(new Uri("pack://application:,,,/Images/seq_1.png"));
            CookieDisplayImage.Source = image;
            CookieDisplayImage.Visibility = Visibility.Visible;
            
            CookieButton.Visibility = Visibility.Visible;
            CookieButton.IsEnabled = true;
            
            FortuneCardBorder.Visibility = Visibility.Collapsed;
            FortuneText.Text = "";
            
            ResetButton.Visibility = Visibility.Collapsed;
            ShareButton.Visibility = Visibility.Collapsed;
        }

        private async void OpenCookie_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CookieButton.IsEnabled = false;
                
                // 1. Immediate Visual Feedback (Frame 2 + Shake)
                var uri2 = new Uri("pack://application:,,,/Images/seq_2.png");
                CookieDisplayImage.Source = new BitmapImage(uri2);
                
                var shakeSb = (Storyboard)FindResource("ShakeCookie");
                shakeSb.Begin();
                
                // 2. Determine Category
                FortuneCategory category = FortuneCategory.General;
                if (CategoryBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                {
                    Enum.TryParse(tag, out category);
                }

                // 3. Request logic
                await _networkService.RequestFortuneAsync(category);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in OpenCookie: {ex.Message}\n{ex.StackTrace}");
                CookieButton.IsEnabled = true; // Re-enable if failed
            }
        }

        private void OnFortuneReceived(Fortune fortune)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // Wait for animation duration (User requested "yavaş geçsin")
                    await Task.Delay(1500);
                    
                    var shakeSb = (Storyboard)FindResource("ShakeCookie");
                    shakeSb.Stop();

                    // Do NOT switch to final paper image. Keep Cracked Cookie (seq_2) visible.
                    // Show the White Box with Fortune
                    FortuneCardBorder.Visibility = Visibility.Visible;
                    CookieDisplayImage.Visibility = Visibility.Collapsed;
                    
                    // Show Text & UI
                    FortuneText.Text = fortune.Text;
                    
                    ResetButton.Visibility = Visibility.Visible;
                    ShareButton.Visibility = Visibility.Visible;
                    CookieButton.Visibility = Visibility.Collapsed; 
                }
                catch (Exception ex)
                {
                     MessageBox.Show($"Error in OnFortuneReceived: {ex.Message}");
                }
            });
        }
        
        private void ResetCookie_Click(object sender, RoutedEventArgs e)
        {
            ResetCookieState();
        }


        private void OnBroadcastReceived(string message) 
        { 
            Dispatcher.Invoke(() => 
            {
                // Message comes as formatted string "📢 GÜNÜN FALI: ...". 
                // Wait, ReceiveLoop constructs this string. 
                // If I want to show numbers, I should update ReceiveLoop to format it better OR handle Fortune object in OnBroadcastReceived.
                // Currently NetworkService passes just string 'message'.
                // I'll stick to string for now for the Banner, maybe Append numbers if they are crucial, but banner space is small.
                // I will update NetworkService to append numbers to the string message!
                
                BroadcastText.Text = message;
                BroadcastBanner.Visibility = Visibility.Visible;
                
                // Auto hide after 10 seconds? Optional.
                Task.Delay(10000).ContinueWith(_ => Dispatcher.Invoke(() => BroadcastBanner.Visibility = Visibility.Collapsed));
            }); 
        }

        private void CloseBroadcast_Click(object sender, RoutedEventArgs e)
        {
            BroadcastBanner.Visibility = Visibility.Collapsed;
        }

        private void OnUserListReceived(List<string> users) { Dispatcher.Invoke(() => UserListBox.ItemsSource = users); }
        
        private void OnMessageReceived(DirectMessagePayload dm)
        {
            Dispatcher.Invoke(() =>
            {
                ChatListBox.Items.Add($"{dm.FromUser}: {dm.Message}");
                 if (ChatListBox.Items.Count > 0)
                    ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
            });
        }
        
        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string targetUser = UserListBox.SelectedItem as string;
            string msg = MsgBox.Text;
            if (string.IsNullOrWhiteSpace(targetUser) || string.IsNullOrWhiteSpace(msg)) return;

            await _networkService.SendDirectMessageAsync(targetUser, msg);
            ChatListBox.Items.Add($"Ben -> {targetUser}: {msg}");
            MsgBox.Clear();
        }
        
        private async void ShareFortune_Click(object sender, RoutedEventArgs e)
        {
             string targetUser = UserListBox.SelectedItem as string;
             string fortune = FortuneText.Text;
             if (string.IsNullOrWhiteSpace(targetUser)) return;
             await _networkService.SendDirectMessageAsync(targetUser, $"[FALIM] {fortune}");
             ChatListBox.Items.Add($"Ben -> {targetUser}: {fortune}");
        }

        private void ShowFortuneTab_Click(object sender, RoutedEventArgs e) 
        { 
            FortuneTab.Visibility = Visibility.Visible; 
            SubmitTab.Visibility = Visibility.Collapsed;
            HistoryTab.Visibility = Visibility.Collapsed;
            LuckyTab.Visibility = Visibility.Collapsed;
        }
        
        private async void ShowSubmitTab_Click(object sender, RoutedEventArgs e) 
        { 
            FortuneTab.Visibility = Visibility.Collapsed; 
            SubmitTab.Visibility = Visibility.Visible;
            HistoryTab.Visibility = Visibility.Collapsed;
            LuckyTab.Visibility = Visibility.Collapsed;
            
            await _networkService.RequestMyFortunesAsync();
        }

        private async void SubmitFortune_Click(object sender, RoutedEventArgs e)
        {
            var text = SubmitTextBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return;
            
             // Determine Category
            FortuneCategory category = FortuneCategory.General;
            if (SubmitCategoryBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                Enum.TryParse(tag, out category);
            }

            await _networkService.SubmitFortuneAsync(text, category);
            SubmitTextBox.Text = "";
            System.Windows.MessageBox.Show("Falınız gönderildi!");
            
            // Refresh list
            await _networkService.RequestMyFortunesAsync();
        }
        
        private void OnMyFortunesReceived(List<Fortune> fortunes)
        {
             Dispatcher.Invoke(() => 
             {
                 var displayList = new List<FortuneDisplayItem>();
                 
                 // If empty, we can show a special item or just empty list. 
                 // If we want "No items" message, we can add a dummy item or handle in UI.
                 // For now, clean list.
                 
                 foreach(var f in fortunes)
                 {
                     string catName = "Genel";
                     string color = "#D7CCC8"; // Beige
                     string icon = "🍪";

                     switch(f.Category)
                     {
                         case FortuneCategory.General: catName = "Genel"; color = "#8D6E63"; icon = "🎲"; break;
                         case FortuneCategory.Funny: catName = "Komik"; color = "#FFA726"; icon = "😂"; break;
                         case FortuneCategory.Wise: catName = "Bilge"; color = "#5C6BC0"; icon = "🦉"; break;
                         case FortuneCategory.Motivational: catName = "Motivasyon"; color = "#66BB6A"; icon = "💪"; break;
                         case FortuneCategory.Romantic: catName = "Romantik"; color = "#EC407A"; icon = "❤️"; break;
                         case FortuneCategory.Cursed: catName = "Lanetli"; color = "#455A64"; icon = "👻"; break;
                     }

                     displayList.Add(new FortuneDisplayItem 
                     { 
                         CategoryDisplay = catName, 
                         Text = f.Text,
                         CategoryColor = color,
                         Icon = icon
                     });
                 }
                 
                 SubmitHistoryList.ItemsSource = displayList;
             });
        }

        public class FortuneDisplayItem
        {
            public string CategoryDisplay { get; set; }
            public string Text { get; set; }
            public string CategoryColor { get; set; }
            public string Icon { get; set; }
        }

        private void OnHistoryReceived(List<FortuneHistoryDto> history)
        {
            Dispatcher.Invoke(() =>
            {
                HistoryListBox.ItemsSource = history;
            });
        }

        private async void ShowHistoryTab_Click(object sender, RoutedEventArgs e)
        {
            FortuneTab.Visibility = Visibility.Collapsed;
            SubmitTab.Visibility = Visibility.Collapsed;
            HistoryTab.Visibility = Visibility.Visible;
            LuckyTab.Visibility = Visibility.Collapsed;
            
            await _networkService.RequestHistoryAsync();
        }

        private void ShowLuckyTab_Click(object sender, RoutedEventArgs e)
        {
            FortuneTab.Visibility = Visibility.Collapsed;
            SubmitTab.Visibility = Visibility.Collapsed;
            HistoryTab.Visibility = Visibility.Collapsed;
            LuckyTab.Visibility = Visibility.Visible;
        }

        private async void GenerateNumbers_Click(object sender, RoutedEventArgs e)
        {
             var btn = (Button)sender;
             btn.IsEnabled = false; // Prevent double click
             
             var rnd = new Random();
             int finalNumber = rnd.Next(1, 100);
             
             // Rolling Effect
             for(int i=0; i<20; i++)
             {
                 CurrentLuckyNumbers.Text = rnd.Next(1, 100).ToString();
                 await Task.Delay(50);
             }
             
             CurrentLuckyNumbers.Text = finalNumber.ToString();
             
             // Play Pop Animation
             var sb = (Storyboard)FindResource("LuckyNumberAnimation");
             sb.Begin();

             // Add to history
             LuckyHistoryList.Items.Insert(0, $"{DateTime.Now.ToShortTimeString()} -> {finalNumber}");
             
             btn.IsEnabled = true;
        }
    }
}