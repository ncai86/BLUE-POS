using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;





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
        public int VatRate { get; set; }
        public Boolean TaxFreeItem { get; set; }
    }

    public class Transaction
    {
        public string ReceiptNum { get; set; }
        public int TotalGrossAmount { get; set; }
        public string DocID { get; set; }
        public bool IsVoided { get; set; }
    }

    //public class ProductInfo
    //{
    //    public decimal Price { get; set; }
    //    public int VatRate { get; set; }
    //    public Boolean TaxFreeItem { get; set; }
    //}
    public class Message
    {
        [JsonPropertyName("Sender")]
        public string Sender { get; set; }

        [JsonPropertyName("Type")]
        public string Type { get; set; }

        [JsonPropertyName("Data")]
        public string Data { get; set; }
    }

    public class IssueModel
    {
        public Purchase purchase { get; set; }
    }

    public class Purchase
    {
        public List<Receipt> receipts { get; set; }
    }

    public class Receipt
    {
        public string receiptNumber { get; set; }
        public string receiptDate { get; set; }
        public object totalAmount { get; set; } // You can replace 'object' with a specific class if needed
        public List<object> totalsPerVat { get; set; } // Define a class if structure is known
        public List<PurchaseItem> purchaseItems { get; set; }
    }

    public class PurchaseItem
    {
        public int vatRate { get; set; }
        public int quantity { get; set; }
        public int unitQuantity { get; set; }
        public string goodId { get; set; }
        public string goodDescription { get; set; }
        public string goodDetailDescription { get; set; }
        public string goodCategory { get; set; }
        public string measurementUnit { get; set; }
        public string serialNumber { get; set; }
        public string productCode { get; set; }
        public Amount amount { get; set; }
        public Amount unitAmount { get; set; }
        public string goodCustomsClassification { get; set; }
        public string masterAmount { get; set; }
        public List<object> customParameters { get; set; } // Define class if needed
    }

    public class Amount
    {

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal grossAmount { get; set; }
        public decimal netAmount { get; set; } = 0m;
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


    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }
    }





    public partial class MainWindow : Window
    {
        private static readonly ObservableCollection<SaleItem> saleItems = [];
        private int _latestTotalGrossAmount;
        private string _latestDocID;

        public ObservableCollection<SaleItem> SaleItems { get; set; } = saleItems;

        public ObservableCollection<Transaction> Transactions { get; set; } = [];

        private HubConnection? _hubConnection;

        public MainWindow()
        {
            InitializeComponent();
            POSDisplayListBox.ItemsSource = SaleItems;
            DataContext = this;

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("pack://application:,,,/Assets/Others/Loader.gif");
            image.EndInit();
            ImageBehavior.SetAnimatedSource(LoaderImage, image);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }



        Process chromeProcess;
        private void OpenHtmlInChrome(string htmlContent)
        {
            // Save HTML to a temporary file
            string tempFilePath = Path.Combine(Path.GetTempPath(), "tempPage.html");
            File.WriteAllText(tempFilePath, htmlContent);


            string persistentProfilePath = Path.Combine(Path.GetTempPath(), "ChromeProfile_Persistent");
            Directory.CreateDirectory(persistentProfilePath);



            try
            {
                // Open the file in Edge
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                    Arguments = $"--new-window --user-data-dir=\"{persistentProfilePath}\" \"{tempFilePath}\"",
                    UseShellExecute = false
                });

                if (process != null)
                {
                    chromeProcess = process;
                }
                else
                {
                    MessageBox.Show("Failed to start Edge browser.");
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error launching Edge: {ex.Message}");
            }



        }

        private void CloseChromeWindow()
        {
            Debug.WriteLine("test close edge window");
            if (chromeProcess != null && !chromeProcess.HasExited)
            {
                chromeProcess.Kill();
                chromeProcess = null;
            }
        }




        private async void saveCredentialBtn_Click(object sender, RoutedEventArgs e)
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
                    TaxFreeToggle.Visibility = Visibility.Visible;
                    //webviewContainer.Visibility = Visibility.Collapsed;
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
                purchase = new Purchase
                {
                    receipts =
                    [
                        new Receipt
                        {
                            receiptNumber = ReceiptNumVal.Content.ToString(),
                            receiptDate = "2025-07-18",
                            purchaseItems =
                            [
                                new PurchaseItem
                                {
                                    vatRate = 10,
                                    quantity = 1,
                                    goodId = "123",
                                    goodDescription = "good",
                                    goodDetailDescription = "detaildescrip",
                                    serialNumber = "3434",
                                    amount = new Amount { netAmount = 1000 },
                                    //UnitAmount = new Amount { GrossAmount = 1000 }
                                }
                            ]
                        }
                    ]
                }
            };



            string issueModelJson = JsonSerializer.Serialize(issueModel);
            MessageBox.Show(issueModelJson);
            TestContainer.Text = issueModelJson;
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
            //string html = $@"
            //<html>
            //  <body onload='document.forms[0].submit()'>
            //    <form method='GET' action='https://ic2integra.test.globalblue.com/ui/integra' enctype='application/x-www-form-urlencoded'>
            //      <input type='hidden' name='sessiontoken' value='{Globals.SessionToken}' />
            //      <input type='hidden' name='groupid' value='5511' />
            //      <input type='hidden' name='action' value='issuesilent' />
            //      <input type='hidden' name='application' value='integra' />
            //      <input type='hidden' name='language' value='ja' />
            //    </form>
            //  </body>
            //</html>";

            string html = $@"
            <html>
              <body onload='document.forms[0].submit()'>
                <form method='POST' action='https://ic2integra.test.globalblue.com/ui/integra' enctype='application/x-www-form-urlencoded'>
                  <input type='hidden' name='sessiontoken' value='{Globals.SessionToken}' />
                  <input type='hidden' name='groupid' value='{Globals.GroupID}' />
                  <input type='hidden' name='action' value='issuesilent' />
                  <input type='hidden' name='application' value='integra' />
                  <input type='hidden' name='language' value='ja' />
                  <input type='hidden' name='issuemodel' value='{issueModelJson}' />
                </form>
              </body>
            </html>";


            //webviewContainer.CoreWebView2.OpenDevToolsWindow();

            //HtmlLauncher.OpenHtmlInBrowser(html);

            webviewContainer.NavigateToString(html);

            await Task.Delay(5000);
            //test sending issuemodel
            Message message = new Message();
            message.Sender = "testgetmodel";
            message.Type = "EvtIssueModel";
            message.Data = $"{issueModelJson}";
            string toSend = JsonSerializer.Serialize(message);

            await _hubConnection.InvokeAsync("sendEvent", toSend, Globals.GroupID);
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
                string[] tagParts = (button.Tag?.ToString() ?? "").Split('|');
                string itemName = tagParts[0];
                decimal itemPrice = Convert.ToInt16(tagParts[1]);
                int vatRate = Convert.ToInt32(tagParts[2]);
                Boolean taxFreeItem = Convert.ToBoolean(tagParts[3]);

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
                        TotalPrice = itemPrice,
                        VatRate = vatRate,
                        TaxFreeItem = taxFreeItem
                    });
                }

                POSDisplayListBox.Items.Refresh();

                //Update Total
                decimal total = SaleItems.Sum(i => i.TotalPrice);
                TotalAmountValue.Content = $"¥{total:0}";
                _latestTotalGrossAmount = Convert.ToInt32(total);
                decimal vat = (total / 110) * 10;
                TotalVatValue.Content = $"¥{vat:0}";


                if (SaleItems.Count != 0)
                {
                    // List is empty
                    PayBtn.IsEnabled = true;
                    PayBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF22CC63"));
                }


            }
        }

        private void ClearAllListedBtn_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                //POSDisplayListBox.Items.Clear();
                SaleItems.Clear();
                PayBtn.IsEnabled = false;
                PayBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7F7F7F"));
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
                _latestTotalGrossAmount = Convert.ToInt32(total);
                decimal vat = (total / 110) * 10;
                TotalVatValue.Content = $"¥{vat:0}";
            }


            if (SaleItems.Count == 0)
            {
                // List is empty
                PayBtn.IsEnabled = false;
                PayBtn.Background = (Brush)new BrushConverter().ConvertFromString("#FF7F7F7F");
            }

        }


        private void ToggleTaxFreeEnableButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleTaxFreeEnableButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private async void WSTest_Click(object sender, RoutedEventArgs e)
        {
            Guid newGuid = Guid.NewGuid();

            string timestamp = DateTime.Now.ToString("yyMMdd_HHmm").Replace(":", "");
            string groupID = $"{newGuid}{timestamp}".Replace("-", "");
            Globals.GroupID = groupID;
            Debug.WriteLine("Generated GUID: " + groupID);

            //Set up WS connection
            string url = $"https://ic2integra.test.globalblue.com/bridge/chat?groupId={groupID}&sender=BluePOS";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(url, o => { o.AccessTokenProvider = () => Task.FromResult(Globals.SessionToken); })
                .Build();

            //Set up event handlers

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            // Receive for WS closed event to handle connection closed scenarios
            _hubConnection.Closed += async (error) => { };

            // Receive WS server events 
            _hubConnection.On<string, string, string>("onEvent", (message, groupId, sender) =>
            {
                //JsonConvert.DeserializeObject<Message>(message);
                var deserializedMessage = JsonSerializer.Deserialize<Message>(message);

                if (!string.IsNullOrWhiteSpace(deserializedMessage.Data))
                {

                    var datanode = JsonNode.Parse(deserializedMessage.Data);

                    string formatted = JsonSerializer.Serialize(datanode, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });


                    Debug.WriteLine($"Server Event, Received message from {sender} in group {groupId}: \nSender: {deserializedMessage.Sender}\nType: {deserializedMessage.Type}\nData:{formatted}");

                }
            });

            // Receive WS application events 
            _hubConnection.On<string>("onGMBEvent", (message) =>
            {
                //JsonConvert.DeserializeObject<Message>(message); 
                var deserializedMessage = JsonSerializer.Deserialize<Message>(message);
                Debug.WriteLine($"App Event, Received message: {message}");
            });


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

        private async void PayBtn_Click(object sender, RoutedEventArgs e)
        {

            //slidedown loader
            var animation = new DoubleAnimation
            {
                From = -100,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            //slideup loader
            var animation2 = new DoubleAnimation
            {
                From = 0,
                To = -200,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            GridTransform.BeginAnimation(TranslateTransform.YProperty, animation);

            await Task.Delay(2500);

            var newImage = new BitmapImage();
            newImage.BeginInit();
            newImage.UriSource = new Uri("pack://application:,,,/Assets/Others/checkmark.gif");
            newImage.EndInit();

            ImageBehavior.SetAnimatedSource(LoaderImage, newImage);

            PayLoadingLabel.Content = "Payment Completed";

            await Task.Delay(2500);

            //GridTransform.BeginAnimation(TranslateTransform.YProperty, animation2);

            //Check Toggle
            if (TaxFreeToggle.IsChecked == true)
            {
                //show loader
                var gbloaderImage = new BitmapImage();
                gbloaderImage.BeginInit();
                gbloaderImage.UriSource = new Uri("pack://application:,,,/Assets/Others/Loader.gif");
                gbloaderImage.EndInit();
                ImageBehavior.SetAnimatedSource(LoaderImage, gbloaderImage);
                PayLoadingLabel.Content = "Loading Tax Free UI";


                //await Task.Delay(1000);

                Guid newGuid = Guid.NewGuid();

                string timestamp = DateTime.Now.ToString("yyMMdd_HHmm").Replace(":", "");
                string groupID = $"{newGuid}{timestamp}".Replace("-", "");
                Globals.GroupID = groupID;
                Debug.WriteLine("Generated GUID: " + groupID);

                //Set up WS connection
                string url = $"https://ic2integra.test.globalblue.com/bridge/chat?groupId={groupID}&sender=BluePOS";
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(url, o => { o.AccessTokenProvider = () => Task.FromResult(Globals.SessionToken); })
                    .Build();

                //Set up event handlers
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                // Receive for WS closed event to handle connection closed scenarios
                _hubConnection.Closed += async (error) => { };

                // Receive WS server events 
                _hubConnection.On<string, string, string>("onEvent", (message, groupId, sender) =>
                {
                    //JsonConvert.DeserializeObject<Message>(message);
                    var deserializedMessage = JsonSerializer.Deserialize<Message>(message);

                    if (!string.IsNullOrWhiteSpace(deserializedMessage.Data))
                    {
                        var datanode = JsonNode.Parse(deserializedMessage.Data);
                        string formatted = JsonSerializer.Serialize(datanode, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                        Debug.WriteLine($"Server Event, Received message from {sender} in group {groupId}: \nSender: {deserializedMessage.Sender}\nType: {deserializedMessage.Type}\nData:{formatted}");


                        if (deserializedMessage.Type == "EvtIssueSuccess")
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (UIType_Webview.IsChecked == true)
                                {
                                    MessageBox.Show("✅ Issue was successful!");
                                    // You can also trigger any other UI updates or logic here
                                    webviewContainer.Visibility = Visibility.Collapsed;
                                }
                                else if (UIType_Browser.IsChecked == true)
                                {
                                    CloseChromeWindow();
                                    MessageBox.Show("closing window");
                                }


                                try
                                {
                                    //POSDisplayListBox.Items.Clear();

                                    SaleItems.Clear();

                                    _latestDocID = datanode?["NumericDocIdentifier"]?.ToString();
                                    if (int.TryParse(datanode?["TotalGrossAmount"]?.ToString(), out int grossAmount))
                                    {
                                        _latestTotalGrossAmount = grossAmount;
                                        Debug.WriteLine($"my gross amount: {grossAmount}");
                                        Debug.WriteLine($"my gross amount2: {_latestTotalGrossAmount}");
                                    }

                                    //Add to list of transactions
                                    var transaction = new Transaction
                                    {
                                        ReceiptNum = ReceiptNumVal?.Content?.ToString() ?? string.Empty, // You can implement this method
                                        TotalGrossAmount = _latestTotalGrossAmount, // Implement based on your logic
                                        DocID = _latestDocID
                                    };

                                    Transactions.Add(transaction);

                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Error clearing items: {ex.Message}");
                                }

                            });
                        }

                    }
                });

                // Receive WS application events 
                _hubConnection.On<string>("onGMBEvent", (message) =>
                {
                    //JsonConvert.DeserializeObject<Message>(message); 
                    var deserializedMessage = JsonSerializer.Deserialize<Message>(message);
                    Debug.WriteLine($"App Event, Received message: {message}");
                });


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



                // create issue model
                var client = new HttpClient();


                var purchaseItems = new List<PurchaseItem>();

                foreach (var saleItem in SaleItems)
                {
                    var purchaseItem = new PurchaseItem
                    {
                        vatRate = saleItem.VatRate,
                        quantity = saleItem.Quantity,
                        goodDescription = saleItem.Name,
                        amount = new Amount { netAmount = saleItem.TotalPrice }
                    };

                    purchaseItems.Add(purchaseItem);
                }


                var issueModel = new IssueModel
                {
                    purchase = new Purchase
                    {
                        receipts = new List<Receipt>
                            {
                                new Receipt
                                {
                                    receiptNumber = ReceiptNumVal.Content.ToString(),
                                    receiptDate = DateTime.Now.ToString("yyyy-MM-dd"),
                                    purchaseItems = purchaseItems
                                }
                            }
                    }
                };


                string issueModelJson = JsonSerializer.Serialize(issueModel);
                //MessageBox.Show(issueModelJson);
                TestContainer.Text = issueModelJson;

                string html = $@"
                <html>
                  <body onload='document.forms[0].submit()'>
                    <form method='POST' action='https://ic2integra.test.globalblue.com/ui/integra' enctype='application/x-www-form-urlencoded'>
                      <input type='hidden' name='sessiontoken' value='{Globals.SessionToken}' />
                      <input type='hidden' name='groupid' value='{Globals.GroupID}' />
                      <input type='hidden' name='action' value='issuesilent' />
                      <input type='hidden' name='application' value='integra' />
                      <input type='hidden' name='language' value='ja' />
                      <input type='hidden' name='issuemodel' value='{issueModelJson}' />
                    </form>
                  </body>
                </html>";


                //webviewContainer.CoreWebView2.OpenDevToolsWindow();

                //HtmlLauncher.OpenHtmlInBrowser(html);

                if (UIType_Webview.IsChecked == true)
                {
                    ////Unhide webview
                    webviewContainer.Visibility = Visibility.Visible;
                    await webviewContainer.EnsureCoreWebView2Async(null);
                    webviewContainer.NavigateToString(html);
                }
                else if (UIType_Browser.IsChecked == true)
                {
                    OpenHtmlInChrome(html);

                }



                //await Task.Delay(5000);
                //test sending issuemodel
                Message message = new Message();
                message.Sender = "testgetmodel";
                message.Type = "EvtIssueModel";
                message.Data = $"{issueModelJson}";
                string toSend = JsonSerializer.Serialize(message);

                await _hubConnection.InvokeAsync("sendEvent", toSend, Globals.GroupID);
                await Task.Delay(1000);
                GridTransform.BeginAnimation(TranslateTransform.YProperty, animation2);

            }
            else
            {
                var transaction = new Transaction
                {
                    ReceiptNum = ReceiptNumVal?.Content?.ToString() ?? string.Empty, // You can implement this method
                    TotalGrossAmount = _latestTotalGrossAmount, // Implement based on your logic
                    DocID = _latestDocID
                };

                Transactions.Add(transaction);
                GridTransform.BeginAnimation(TranslateTransform.YProperty, animation2);
                await Task.Delay(2500);
            }

            try
            {
                //POSDisplayListBox.Items.Clear();
                SaleItems.Clear();
                if (SaleItems.Count == 0)
                {
                    // List is empty
                    PayBtn.IsEnabled = false;
                    PayBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7F7F7F"));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing items: {ex.Message}");
            }




            //need to add payment simulation// 
            int number = int.Parse(ReceiptNumVal.Content.ToString());
            number += 1;

            int desiredLength = ReceiptNumVal.Content.ToString().Length;
            string result = number.ToString("D" + ReceiptNumVal.Content.ToString().Length);
            ReceiptNumVal.Content = result;
            //MessageBox.Show("test");


            var loaderImage = new BitmapImage();
            loaderImage.BeginInit();
            loaderImage.UriSource = new Uri("pack://application:,,,/Assets/Others/Loader.gif");
            loaderImage.EndInit();

            ImageBehavior.SetAnimatedSource(LoaderImage, loaderImage);


            TotalAmountValue.Content = "¥";
            TotalVatValue.Content = "¥";


        }


        private void VoidButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Transaction transaction)
            {
                // Your logic to void the transaction
                transaction.IsVoided = true;
                button.IsEnabled = false;
                TransactionList.Items.Refresh();

                // Optionally refresh the ListView or notify the user
                MessageBox.Show($"Transaction {transaction.ReceiptNum} has been voided.");

            }
        }

    }
}