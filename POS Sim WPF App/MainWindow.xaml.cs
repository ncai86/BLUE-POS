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
using System.Net.Http;
using Microsoft.Web.WebView2.Core;
using System.Text.RegularExpressions;


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

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
    }
}