using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mark1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InputTextBox = this.FindControl<TextBox>("InputTextBox");
            ResponseTextBox = this.FindControl<TextBox>("ResponseTextBox");
            InputTextBoxApikey = this.FindControl<TextBox>("InputTextBoxApikey");
            ProviderComboBox = this.FindControl<ComboBox>("ProviderComboBox");

            // Cargar la configuración al iniciar
            Configuration config = LoadConfiguration();
            if (config != null)
            {
                // Asignar la clave API al TextBox
                InputTextBoxApikey.Text = config.ApiKey;

                // Seleccionar el proveedor en el ComboBox
                var item = ProviderComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == config.SelectedProvider);

                if (item != null)
                {
                    ProviderComboBox.SelectedItem = item;
                }
            }
        }

        // Configuración: Guardar los cambios de configuración
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

        // Guardar configuración en un archivo JSON
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

        // Cargar configuración desde el archivo JSON
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

        // Enviar consulta al proveedor seleccionado
        private async void OnSendButtonClick(object sender, RoutedEventArgs e)
        {
            string userInput = InputTextBox?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(userInput))
            {
                Console.WriteLine("Por favor ingresa una consulta.");
                return;
            }

            string apiKey = InputTextBoxApikey.Text ?? string.Empty;
            string? selectedProvider = (ProviderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("La clave API está vacía.");
                return;
            }

            if (string.IsNullOrEmpty(selectedProvider))
            {
                Console.WriteLine("Por favor selecciona un proveedor.");
                return;
            }

            // Llamar a la API del proveedor seleccionado
            string response = await MakeRequestToProvider(selectedProvider, apiKey, userInput);
            ResponseTextBox.Text = response;
        }

        // Llamar a la API del proveedor según la selección
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
                    return "Proveedor no válido.";
            }
        }

        // Llamar a la API de OpenAI
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
                return jsonResponse?.choices?[0]?.text?.ToString()?.Trim() ?? "Sin respuesta.";
            }
        }

        // Llamar a la API de Gemini
        public async Task<string> CallGemineFunction(string apiKey, string prompt)
{
    using (var client = new HttpClient())
    {
        string apiUrlGemine = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

        // Crear el objeto de datos para la solicitud
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

        // Convertir el objeto a JSON
        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

        // Enviar la solicitud POST
        var response = await client.PostAsync(apiUrlGemine, jsonContent);

        // Asegurarse de que la respuesta sea exitosa
        response.EnsureSuccessStatusCode();

        // Leer la respuesta como string
        var responseBody = await response.Content.ReadAsStringAsync();

        // Parsear la respuesta JSON
        var jsonResponse = JObject.Parse(responseBody);

        // Acceder al valor del texto en la respuesta (suponiendo que la estructura de la respuesta es la misma que el ejemplo)
        var text = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "Error de Respuesta";

        return text;
    }
}




        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Obtiene el ComboBox desde el sender (el control que disparó el evento)
            ComboBox comboBox = sender as ComboBox;

            // Obtiene el proveedor seleccionado
            string selectedProvider = (comboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

            // Verifica que el proveedor seleccionado no esté vacío
            if (string.IsNullOrEmpty(selectedProvider))
            {
                Console.WriteLine("No provider selected.");
                return;
            }

            // Aquí puedes llamar a la función correspondiente para cada proveedor
            string apiKey = InputTextBoxApikey.Text ?? string.Empty;
        }


        // Llamar a la API de Hugging Face
        private async Task<string> CallHuggyFaceFunction(string apiKey, string userInput)
        {
            string apiUrl = "https://api-inference.huggingface.co/models/openai-community/gpt2";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var requestData = new { inputs = userInput };
                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, jsonContent);

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                return jsonResponse?[0]?.generated_text?.ToString()?.Trim() ?? "Sin respuesta.";
            }
        }
    }



}
