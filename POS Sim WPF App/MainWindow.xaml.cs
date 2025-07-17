using Microsoft.Web.WebView2.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace POS_Sim_WPF_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public static class Globals
    {
        public static string SessionToken { get; set; }
    }

    public class SaleItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }

    
    public partial class MainWindow : Window
    {
        public ObservableCollection<SaleItem> SaleItems { get; set; } = new ObservableCollection<SaleItem>();

        public MainWindow()
        {
            InitializeComponent();
            POSDisplayListBox.ItemsSource = SaleItems;
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void TokTest_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTextbox.Text.Trim();
            string password = passwordTextbox.Password;
            int tokenLifetime = 3600;

            var payloadObject = new
            {
                Username = username,
                Password = password,
                TokenLifetime = tokenLifetime
            };

            string jsonPayload = JsonSerializer.Serialize(payloadObject);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set base address and headers if needed
                    client.BaseAddress = new Uri("https://ic2integra.test.globalblue.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    // client.DefaultRequestHeaders.Add("YourCustomHeader", "value"); // optional

                    // Prepare your payload if POST
            //        var jssonPayload = @"{
            //    ""Username"": ""test_89999"",
            //    ""Password"": ""test_89999"",
            //    ""TokenLifetime"": 1200
                   //}";

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // POST the request
                    HttpResponseMessage response = await client.PostAsync("service/api/UserAuthentication/RequestSessionToken", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Token Response:\n" + responseBody);
                        //TestContainer.Text = responseBody;
                        Globals.SessionToken = responseBody;

                        using (JsonDocument doc = JsonDocument.Parse(responseBody))
                        {
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("Token", out JsonElement tokenElement))
                            {
                                Globals.SessionToken = tokenElement.GetString();
                                MessageBox.Show("Token stored globally:\n" + Globals.SessionToken);
                                TestContainer.Text = tokenElement.GetString();
                            }
                            else
                            {
                                MessageBox.Show("Token not found in response.");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Error: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message);
            }
        }

        private async void LaunchIC2_Click(object sender, RoutedEventArgs e)
        {
            await webviewContainer.EnsureCoreWebView2Async(null);

            string sessionToken = Globals.SessionToken; // from earlier
            string groupId = "456"; // your actual group ID

            string html = $@"
            <html>
              <body onload='document.forms[0].submit()'>
                <form method='POST' action='https://ic2integra.test.globalblue.com/ui/integra' enctype='application/x-www-form-urlencoded'>
                  <input type='hidden' name='sessiontoken' value='{sessionToken}' />
                  <input type='hidden' name='groupid' value='{groupId}' />
                  <input type='hidden' name='action' value='issuesilent' />
                  <input type='hidden' name='application' value='integra' />
                  <input type='hidden' name='language' value='ja' />
                </form>
              </body>
            </html>";

            webviewContainer.NavigateToString(html);
            //webviewContainer.Source = new Uri(html);
        }

        private void SettingsClear_Click(object sender, RoutedEventArgs e)
        {
            usernameTextbox.Clear();
            passwordTextbox.Clear();
            //webviewContainer.Visibility = Visibility.Collapsed;
        }

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string itemName = button.Content.ToString();
                decimal itemPrice = Convert.ToInt16(button.Tag);

                var existingItem = SaleItems.FirstOrDefault(i => i.Name == itemName);
                if (existingItem != null)
                {
                    existingItem.Quantity++;
                    existingItem.TotalPrice += itemPrice;
                }
                else
                {
                    SaleItems.Add(new SaleItem
                    {
                        Name = itemName,
                        Quantity = 1,
                        TotalPrice = itemPrice
                    });
                }

                POSDisplayListBox.Items.Refresh();

                //Update Total
                decimal total = SaleItems.Sum(i => i.TotalPrice);
                TotalAmountValue.Content = $"¥{total:0}";

                decimal vat = (total / 110) * 10;
                TotalVatValue.Content = $"¥{vat:0}";   

            }
        }

        private void ClearAllListedBtn_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                //POSDisplayListBox.Items.Clear();
                SaleItems.Clear();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing items: {ex.Message}");
            }

            //SaleItems.Clear();
            //POSDisplayListBox.Items.Refresh();
        }


        private void RemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SaleItem item)
            {
                SaleItems.Remove(item);
            }
        }
    }
}