using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Vosk;

namespace GasPro.Services
{
    public class AudioService
    {
        private Model _voskModel;
        private VoskRecognizer _recognizer;
        private int _micDeviceIndex;

        // Inicializamos Vosk y buscamos el micro automáticamente (Tu misma lógica)
        public void Initialize(string modelPath)
        {
            Vosk.Vosk.SetLogLevel(0); // Silencia la consola de Vosk
            _voskModel = new Model(modelPath);
            _recognizer = new VoskRecognizer(_voskModel, 16000.0f);

            _micDeviceIndex = 0;
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                string nombre = caps.ProductName.ToLower();
                if ((nombre.Contains("mic") || nombre.Contains("array") || nombre.Contains("audio"))
                    && !nombre.Contains("stereo") && !nombre.Contains("mezcla") && !nombre.Contains("mix"))
                {
                    _micDeviceIndex = n;
                    break;
                }
            }
        }

        // Método asíncrono que escucha hasta que digas "gas"
        public async Task<string> ListenForPromptAsync()
        {
            return await Task.Run(() =>
            {
                string promptExtraido = "";

                // Abrimos el flujo del micrófono
                using var waveIn = new WaveInEvent
                {
                    DeviceNumber = _micDeviceIndex,
                    WaveFormat = new WaveFormat(16000, 16, 1)
                };

                waveIn.DataAvailable += (s, a) =>
                {
                    if (_recognizer.AcceptWaveform(a.Buffer, a.BytesRecorded))
                    {
                        string jsonResult = _recognizer.Result();
                        using var doc = JsonDocument.Parse(jsonResult);
                        string text = doc.RootElement.GetProperty("text").GetString()?.ToLower() ?? "";

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // Limpiamos la frase y buscamos nuestra palabra clave
                            string textoLimpio = text.Replace(".", "").Replace(",", "").Trim();
                            int indiceGas = textoLimpio.IndexOf("gas");

                            if (indiceGas != -1)
                            {
                                // Extraemos solo lo que dijiste después de "gas" o "gas pro"
                                string prompt = textoLimpio.Substring(indiceGas + 3).Trim();
                                if (prompt.StartsWith("pro")) prompt = prompt.Substring(3).Trim();

                                if (!string.IsNullOrWhiteSpace(prompt))
                                {
                                    promptExtraido = prompt; // Señal para romper el bucle
                                }
                            }
                        }
                    }
                };

                waveIn.StartRecording();

                // Mantenemos el hilo esperando tu pregunta válida
                while (string.IsNullOrEmpty(promptExtraido))
                {
                    Thread.Sleep(50);
                }

                waveIn.StopRecording();

                return promptExtraido;
            });
        }
    }
}