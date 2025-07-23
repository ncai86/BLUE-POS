using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;




namespace POS_Sim_WPF_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public static class Globals
    {
        public static string SessionToken { get; set; }
        public static string GroupID { get; set; }
    }

    public class SaleItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }


    //public class IssueModel
    //{
    //    public PurchaseModel issue{ get; set; }
    //}

    public class IssueModel
    {
        public Purchase Purchase { get; set; }
    }

    public class Purchase
    {
        public List<Receipt> Receipts { get; set; }
    }

    public class Receipt
    {
        public string ReceiptNumber { get; set; }
        public string ReceiptDate { get; set; }
        public object TotalAmount { get; set; } // You can replace 'object' with a specific class if needed
        public List<object> TotalsPerVat { get; set; } // Define a class if structure is known
        public List<PurchaseItem> PurchaseItems { get; set; }
    }

    public class PurchaseItem
    {
        public int VatRate { get; set; }
        public int Quantity { get; set; }
        public int UnitQuantity { get; set; }
        public string GoodId { get; set; }
        public string GoodDescription { get; set; }
        public string GoodDetailDescription { get; set; }
        public string GoodCategory { get; set; }
        public string MeasurementUnit { get; set; }
        public string SerialNumber { get; set; }
        public string ProductCode { get; set; }
        public Amount Amount { get; set; }
        public Amount UnitAmount { get; set; }
        public string GoodCustomsClassification { get; set; }
        public string MasterAmount { get; set; }
        public List<object> CustomParameters { get; set; } // Define class if needed
    }

    public class Amount
    {
        public decimal GrossAmount { get; set; } = 0m;
    }


    public class HtmlLauncher
    {
        public static void OpenHtmlInBrowser(string htmlContent)
        {
            // Create a temporary HTML file
            string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tempPage.html");
            File.WriteAllText(tempFilePath, htmlContent);

            // Open the file in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = tempFilePath,
                UseShellExecute = true
            });
        }
    }



    public partial class MainWindow : Window
    {
        private static readonly ObservableCollection<SaleItem> saleItems = [];

        public ObservableCollection<SaleItem> SaleItems { get; set; } = saleItems;

        private HubConnection? _hubConnection;

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
            int tokenLifetime = 100;

            var payloadObject = new
            {
                Username = username,
                Password = password,
                TokenLifetime = tokenLifetime
            };

            string jsonPayload = JsonSerializer.Serialize(payloadObject);

            try
            {
                using HttpClient client = new();
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

                    using JsonDocument doc = JsonDocument.Parse(responseBody);
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
                else
                {
                    MessageBox.Show($"Error: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occurred: " + ex.Message);
            }
        }

        private async void LaunchIC2_Click(object sender, RoutedEventArgs e)
        {
            webviewContainer.Visibility = Visibility.Visible;
            await webviewContainer.EnsureCoreWebView2Async(null);

            // create issue model
            var client = new HttpClient();

            var issueModel = new IssueModel
            {
                Purchase = new Purchase
                {
                    Receipts =
                    [
                        new Receipt
                        {
                            ReceiptNumber = ReceiptNumVal.Content.ToString(),
                            ReceiptDate = "2025-07-18",
                            PurchaseItems =
                            [
                                new PurchaseItem
                                {
                                    VatRate = 10,
                                    Quantity = 1,
                                    GoodId = "123",
                                    GoodDescription = "good",
                                    GoodDetailDescription = "detaildescrip",
                                    SerialNumber = "3434",
                                    Amount = new Amount { GrossAmount = 1000 },
                                    UnitAmount = new Amount { GrossAmount = 1000 }
                                }
                            ]
                        }
                    ]
                }
            };



            string issueModelJson = JsonSerializer.Serialize(issueModel);
            MessageBox.Show(Globals.GroupID);

            //////testing out
            //var httpClient = new HttpClient();
            ////httpClient.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

            //var formData = new Dictionary<string, string>
            //{
            //    {"sessiontoken", Globals.SessionToken },
            //    {"groupid", Globals.GroupID },
            //    {"action", "issuesilent" },
            //    {"application", "integra" },
            //    {"language", "ja" },
            //    {"issuemodel", issueModelJson }
            //};

            //// Create the content
            //var content = new FormUrlEncodedContent(formData);


            //var response = await client.PostAsync("https://ic2integra.test.globalblue.com/ui/integra", content);
            //var result = await response.Content.ReadAsStringAsync();


            //webviewContainer.CoreWebView2.NavigateToString(result);
            //MessageBox.Show(issueModelJson);
            string html = $@"
            <html>
              <body onload='document.forms[0].submit()'>
                <form method='POST' action='https://ic2integra.test.globalblue.com/ui/integra' enctype='application/x-www-form-urlencoded'>
                  <input type='hidden' name='sessiontoken' value='{Globals.SessionToken}' />
                  <input type='hidden' name='groupid' value='5344' />
                  <input type='hidden' name='action' value='issuesilent' />
                  <input type='hidden' name='application' value='integra' />
                  <input type='hidden' name='language' value='ja' />
                  <input type='hidden' name='issuemodel' value='{issueModelJson}' />
                </form>
              </body>
            </html>";

            webviewContainer.CoreWebView2.OpenDevToolsWindow();

            //HtmlLauncher.OpenHtmlInBrowser(html);

            webviewContainer.NavigateToString(html);
            //webviewContainer.Source = new Uri(html);


            //string postDataString = $"sessiontoken={Globals.SessionToken}" +
            //                        $"&groupid={Globals.GroupID}" +
            //                        $"&action=issuesilent" +
            //                        $"&application=integra" +
            //                        $"&language=ja" +
            //                        $"&issuemodel={issueModelJson}";

            //byte[] postData = Encoding.UTF8.GetBytes(postDataString);
            //Stream postDataStream = new MemoryStream(postData);

            //CoreWebView2WebResourceRequest request = webviewContainer.CoreWebView2.Environment.CreateWebResourceRequest(
            //    "https://ic2integra.test.globalblue.com/ui/integra",
            //    "POST",
            //    postDataStream,
            //    "Content-Type: application/x-www-form-urlencoded"
            //);

            //webviewContainer.CoreWebView2.NavigateWithWebResourceRequest(request);
            // This script intercepts window.location.href assignments and replaces them with a POST form submission


            //webviewContainer.CoreWebView2.GetDevToolsProtocolEventReceiver("Network.requestWillBeSent")
            //    .DevToolsProtocolEventReceived += (s, e) =>
            //    {
            //        string json = e.ParameterObjectAsJson;
            //        Console.WriteLine("Request JSON: " + json);
            //    };

            //await webviewContainer.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");



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

                //Update Total
                decimal total = SaleItems.Sum(i => i.TotalPrice);
                TotalAmountValue.Content = $"¥{total:0}";

                decimal vat = (total / 110) * 10;
                TotalVatValue.Content = $"¥{vat:0}";
            }
        }


        private void ToggleTaxFreeEnableButton_Checked(object sender, RoutedEventArgs e)
        {
            //if (TaxFreeToggle.IsChecked == true)
            //{
            //    // The toggle is ON (enabled)
            //    MessageBox.Show("button enabled.");
            //}
            //else
            //{
            //    // The toggle is OFF (disabled)
            //    MessageBox.Show("button disabled.");
            //}
        }

        private void ToggleTaxFreeEnableButton_Unchecked(object sender, RoutedEventArgs e)
        {
            //if (TaxFreeToggle.IsChecked == true)
            //{
            //    // The toggle is ON (enabled)
            //    MessageBox.Show("button enabled.");
            //}
            //else
            //{
            //    // The toggle is OFF (disabled)
            //    MessageBox.Show("button disabled.");
            //}
            //TaxFreeToggle.Background = (Brush)new BrushConverter().ConvertFromString("#FF001EC8");
        }

        private async void WSTest_Click(object sender, RoutedEventArgs e)
        {
            Guid newGuid = Guid.NewGuid();

            string timestamp = DateTime.Now.ToString("yyMMdd_HHmmsszzz").Replace(":", "");
            string groupID = $"{newGuid}{timestamp}".Replace("-", "");
            Globals.GroupID = groupID;
            Debug.WriteLine("Generated GUID: " + groupID);

            string url = $"https://ic2integra.test.globalblue.com/bridge/chat?groupId=5344&sender=BluePOS";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(url, o => { o.AccessTokenProvider = () => Task.FromResult(Globals.SessionToken); })
                .Build();

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _hubConnection.StartAsync();
                stopwatch.Stop();

                Debug.WriteLine("✅ Hub connection started successfully.");
                Debug.WriteLine($"⏱️ Time elapsed: {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to start hub connection: {ex.Message}");
            }

        }

        private void PayBtn_Click(object sender, RoutedEventArgs e)
        {
            //need to add payment simulation// 

            int number = int.Parse(ReceiptNumVal.Content.ToString());
            number += 1;

            int desiredLength = ReceiptNumVal.Content.ToString().Length;
            string result = number.ToString("D" + ReceiptNumVal.Content.ToString().Length);
            ReceiptNumVal.Content = result;

        }

    }
}