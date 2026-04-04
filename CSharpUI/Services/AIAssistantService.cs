using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThreeDBuilder.Services
{
    public class AIAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public AIAssistantService(string apiKey, string apiUrl = "https://api.openai.com/v1/chat/completions")
        {
            _apiKey = apiKey;
            _apiUrl = apiUrl;
            _httpClient = new HttpClient();
        }

        public async Task<string> GenerateShapeFromDescription(string description)
        {
            try
            {
                var prompt = $@"
Du bist ein 3D-CAD-Assistent. Der Nutzer beschreibt eine Form, die er erstellen möchte.
Antworte mit einem JSON-Objekt mit folgenden Feldern:
- shape_type: (box, sphere, cylinder, cone, torus, prism, pyramid, tube, ellipsoid, hemisphere, l_profile, t_profile, star, polygon, thread_cyl)
- parameters: {{Objekt mit den Parametern}}
- explanation: Kurze Erklärung auf Deutsch

Nutzerbeschreibung: {description}

Antworte NUR mit dem JSON-Objekt, keine zusätzlichen Erklärungen.";

                var response = await CallOpenAI(prompt);
                return response;
            }
            catch (Exception ex)
            {
                return $"Fehler bei der Formgenerierung: {ex.Message}";
            }
        }

        public async Task<string> GetTutorialForFeature(string feature)
        {
            try
            {
                var prompt = $@"
Du bist ein hilfsbereiter 3D-CAD-Assistent. 
Erkläre kurz und verständlich auf Deutsch (max. 3 Sätze), wie man diese Funktion nutzt:
Feature: {feature}

Sei freundlich und ermutigend!";

                return await CallOpenAI(prompt);
            }
            catch (Exception ex)
            {
                return $"Fehler beim Abrufen des Tutorials: {ex.Message}";
            }
        }

        public async Task<string> AnswerQuestion(string question)
        {
            try
            {
                var prompt = $@"
Du bist ein freundlicher und hilfreicher 3D-CAD-Assistent für ein Programm namens '3D Builder Pro'.
Beantworte die Frage des Nutzers kurz und verständlich auf Deutsch (max. 3 Sätze).
Sei ermutigend und positiv!

Frage: {question}";

                return await CallOpenAI(prompt);
            }
            catch (Exception ex)
            {
                return $"Fehler beim Beantworten der Frage: {ex.Message}";
            }
        }

        private async Task<string> CallOpenAI(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"API-Fehler: {response.StatusCode}";
                }

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var message = jsonResponse.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return message ?? "Keine Antwort erhalten";
            }
            catch (Exception ex)
            {
                return $"Fehler: {ex.Message}";
            }
        }
    }
}
