using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mark1
{
    public partial class MainWindow : Window
    {

        public ObservableCollection<ChatMessage> ChatHistory { get; set; }


        public TextBox? ResponseTextBox { get; }


        public MainWindow()
        {
            InitializeComponent();
            InputTextBox = this.FindControl<TextBox>("InputTextBox");
            ResponseTextBox = this.FindControl<TextBox>("ResponseTextBox");
            InputTextBoxApikey = this.FindControl<TextBox>("InputTextBoxApikey");
            ProviderComboBox = this.FindControl<ComboBox>("ProviderComboBox");




            ChatHistory = new ObservableCollection<ChatMessage>();
            DataContext = this;


            // Load configuration on startup
            Configuration config = LoadConfiguration();
            if (config != null)
            {
                // Assign API key to TextBox
                InputTextBoxApikey.Text = config.ApiKey;

                // Select provider in ComboBox
                var item = ProviderComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == config.SelectedProvider);

                if (item != null)
                {
                    ProviderComboBox.SelectedItem = item;
                }
            }
        }

        private void ConfigurationProvider(object sender, RoutedEventArgs e)
        {
            string apiKey = InputTextBoxApikey.Text ?? string.Empty;
            string? selectedProvider = (ProviderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Por favor, ingresa una clave de API.");
                return;
            }

            if (string.IsNullOrEmpty(selectedProvider))
            {
                Console.WriteLine("Por favor, selecciona un proveedor.");
                return;
            }

            SaveConfiguration(apiKey, selectedProvider);
            Console.WriteLine($"Proveedor: {selectedProvider}\nClave API configurada correctamente.");
        }

        private void SaveConfiguration(string apiKey, string selectedProvider)
        {
            Configuration config = new Configuration
            {
                ApiKey = apiKey,
                SelectedProvider = selectedProvider
            };

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            string filePath = "config.json"; // Ruta del archivo JSON
            System.IO.File.WriteAllText(filePath, json);

            Console.WriteLine("Configuración guardada.");
        }

        private Configuration? LoadConfiguration()
        {
            string filePath = "config.json"; // Ruta del archivo JSON

            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("El archivo de configuración está vacío.");
                    return null;
                }

                try
                {
                    Configuration config = JsonConvert.DeserializeObject<Configuration>(json) ?? new Configuration();
                    Console.WriteLine($"Leyendo Configuración: {config.ApiKey} - {config.SelectedProvider}");
                    return config;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("Error al leer la configuración: " + ex.Message);
                    return null;
                }
            }
            else
            {
                Console.WriteLine("No se encontró el archivo de configuración.");
                return null;
            }
        }


        private async void OnSendButtonClick(object sender, RoutedEventArgs e)
        {

            string userInput = InputTextBox?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(userInput))
            {
                Console.WriteLine("Please enter a query.");
                return;
            }

            string apiKey = InputTextBoxApikey.Text ?? string.Empty;
            string? selectedProvider = (ProviderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key is empty.");
                return;
            }

            if (string.IsNullOrEmpty(selectedProvider))
            {
                Console.WriteLine("Please select a provider.");
                return;
            }

            ChatHistory.Add(new ChatMessage
            {
                Message = $"You: {userInput}",
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            });

            string response = await MakeRequestToProvider(selectedProvider, apiKey, userInput);

            ChatHistory.Add(new ChatMessage
            {
                Message = $"Bot: {response}",
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            });


            InputTextBox?.Clear();
        }



        private async Task<string> MakeRequestToProvider(string provider, string apiKey, string userInput)
        {
            switch (provider)
            {
                case "Open":
                    return await CallOpenIAFunction(apiKey, userInput);
                case "Gemine":
                    return await CallGemineFunction(apiKey, userInput);
                case "Huggy":
                    return await CallHuggyFaceFunction(apiKey, userInput);
                default:
                    return "Invalid provider.";
            }
        }

        private async Task<string> CallOpenIAFunction(string apiKey, string userInput)
        {
            string apiUrl = "https://api.openai.com/v1/completions";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var requestData = new { prompt = userInput, max_tokens = 100 };
                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, jsonContent);

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                return jsonResponse?.choices?[0]?.text?.ToString()?.Trim() ?? "No response.";
            }
        }

        public async Task<string> CallGemineFunction(string apiKey, string prompt)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string apiUrlGemine = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                    var requestData = new
                    {
                        contents = new[] {
                    new {
                        parts = new[] {
                            new { text = prompt }
                        }
                    }
                }
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiUrlGemine, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JObject.Parse(responseBody);

                        var text = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                        return text ?? "No content found in response";
                    }
                    else
                    {
                        string errorMessage = $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                        return errorMessage;
                    }
                }
                catch (HttpRequestException ex)
                {
                    return $"HttpRequestException: {ex.Message}";
                }
                catch (Exception ex)
                {
                    return $"Exception: {ex.Message}";
                }
            }
        }


        private async Task<string> CallHuggyFaceFunction(string apiKey, string userInput)
        {
            string apiUrl = "https://api-inference.huggingface.co/models/openai-community/gpt2";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var requestData = new { inputs = userInput };
                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(apiUrl, jsonContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        return $"Error {response.StatusCode}: {response.ReasonPhrase}";
                    }

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    return jsonResponse?[0]?.generated_text?.ToString()?.Trim() ?? "No response.";
                }
                catch (HttpRequestException ex)
                {
                    return $"Request error: {ex.Message}";
                }
                catch (Exception ex)
                {
                    return $"Unexpected error: {ex.Message}";
                }
            }
        }


        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            string selectedProvider = (comboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(selectedProvider))
            {
                Console.WriteLine("No provider selected.");
                return;
            }

            string apiKey = InputTextBoxApikey.Text ?? string.Empty;
        }



    }

    public class ChatMessage
    {
        public string Message { get; set; }
        public string Timestamp { get; set; }
    }

}
