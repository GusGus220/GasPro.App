using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GasPro.Services;
using System.Diagnostics; // Usamos esto para imprimir errores sin crashear WPF

namespace GasPro.App // <-- Asegúrate de que coincida con tu proyecto (App o Core)
{
    public class LocalChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class GasOrchestrator
    {
        private readonly LlamaService _llamaService;
        private readonly PiperSpeechService _speechService;
        private readonly AudioService _audioService;
        private readonly WindowsControlService _windowsService;

        private readonly List<LocalChatMessage> _chatHistory;
        private const int MaxHistorySize = 2;

        // ⚡ EL CABLE NERVIOSO HACIA JARVIS
        public Action<MainWindow.EstadoIA> OnCambioEstado;

        public GasOrchestrator()
        {
            _llamaService = new LlamaService();
            _speechService = new PiperSpeechService();
            _audioService = new AudioService();
            _windowsService = new WindowsControlService();
            _chatHistory = new List<LocalChatMessage>();
        }

        // Lo convertimos en Async para que la UI no se congele al cargar Gigabytes
        public async Task InitializeAsync(string llamaModelPath, string voskModelPath)
        {
            Vosk.Vosk.SetLogLevel(-1);

            // Inicializamos los motores
            _llamaService.Initialize(llamaModelPath);
            _speechService.InitializeAsync("es_ES-sharvard-medium", "models/piper").GetAwaiter().GetResult();
            _audioService.Initialize(voskModelPath);

            // El Pre-calentamiento silencioso
            try
            {
                var enumerador = _llamaService.GenerateResponseStreamAsync("a").GetAsyncEnumerator();
                await enumerador.MoveNextAsync();
            }
            catch { }

            // 🟢 Mandamos la señal a la Cara de que estamos listos
            OnCambioEstado?.Invoke(MainWindow.EstadoIA.Reposo);
        }

        public async Task RunAsync()
        {
            while (true)
            {
                // 1. Jarvis en azul pacífico esperando
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Reposo);
                string promptExtraido = await _audioService.ListenForPromptAsync();

                // 2. Jarvis detectó tu voz (Naranja)
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Escuchando);
                await Task.Delay(400); // Micro-pausa visual

                if (promptExtraido.Contains("salir") || promptExtraido.Contains("apágate")) break;

                // ---- 🚨 RUTA DE REFLEJOS DEL SISTEMA ----
                bool esComandoDeSistema = true;
                string comando = promptExtraido.ToLower();

                if (comando.Contains("spotify"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    _speechService.SpeakAsync("Abriendo Spotify.");
                    _windowsService.OpenApplication("spotify:");
                }
                else if (comando.Contains("chrome") || comando.Contains("google") || comando.Contains("navegador"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    _speechService.SpeakAsync("Abriendo el navegador.");
                    _windowsService.OpenApplication("https://www.google.com");
                }
                else if (comando.Contains("abre discord"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    _speechService.SpeakAsync("Abriendo Discord.");
                    _ = Task.Run(() => _windowsService.OpenAppBySearch("discord"));
                }
                else if (comando.Contains("hora") || comando.Contains("qué hora es"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    string horaFormateada = DateTime.Now.ToString("h:mm tt", new System.Globalization.CultureInfo("es-ES"))
                        .Replace("AM", "de la mañana").Replace("PM", "de la tarde");
                    string mensajeHora = $"Son las {horaFormateada}";
                    _speechService.SpeakAsync(mensajeHora);
                }
                else if (comando.Contains("fecha") || comando.Contains("día es hoy") || comando.Contains("dia es hoy"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    string fechaFormateada = DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
                    string mensajeFecha = $"Hoy es {fechaFormateada}";
                    _speechService.SpeakAsync(mensajeFecha);
                }
                else if (comando.Contains("haz clic") || comando.Contains("haz click"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    _speechService.SpeakAsync("Clic hecho.");
                    _windowsService.LeftClick();
                }
                else if (comando.Contains("pausa") || comando.Contains("reanuda") || comando.Contains("reproduce"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    _speechService.SpeakAsync("Hecho.");
                    _windowsService.PlayPauseMusic();
                }
                else if (comando.Contains("volumen"))
                {
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    int targetVolume = -1;
                    string[] palabras = comando.Split(' ');

                    foreach (var palabra in palabras)
                    {
                        string numStr = palabra.Replace("%", "").Trim();
                        if (int.TryParse(numStr, out int num)) { targetVolume = num; break; }

                        if (numStr == "cero") targetVolume = 0;
                        else if (numStr == "diez") targetVolume = 10;
                        else if (numStr == "veinte") targetVolume = 20;
                        else if (numStr == "treinta") targetVolume = 30;
                        else if (numStr == "cuarenta") targetVolume = 40;
                        else if (numStr == "cincuenta") targetVolume = 50;
                        else if (numStr == "sesenta") targetVolume = 60;
                        else if (numStr == "setenta") targetVolume = 70;
                        else if (numStr == "ochenta") targetVolume = 80;
                        else if (numStr == "noventa") targetVolume = 90;
                        else if (numStr == "cien" || numStr == "ciento") targetVolume = 100;
                    }

                    if (targetVolume != -1)
                    {
                        _speechService.SpeakAsync($"Ajustando el volumen al {targetVolume} por ciento.");
                        _windowsService.SetVolume(targetVolume);
                    }
                    else if (comando.Contains("baj") || comando.Contains("disminu"))
                    {
                        _speechService.SpeakAsync("Bajando el volumen.");
                        _windowsService.ChangeVolumeBy(-20);
                    }
                    else if (comando.Contains("sub") || comando.Contains("aument"))
                    {
                        _speechService.SpeakAsync("Subiendo el volumen.");
                        _windowsService.ChangeVolumeBy(20);
                    }
                }
                else
                {
                    esComandoDeSistema = false;
                }

                if (esComandoDeSistema)
                {
                    _speechService.WaitForSpeechToFinish();
                    continue; // Volvemos a escuchar sin despertar a Llama
                }
                // ------------------------------------------------

                // 3. Jarvis se pone a calcular (Rojo)
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Pensando);

                string systemPrompt = "<|start_header_id|>system<|end_header_id|>\n\nEres GAS PRO, asistente de IA avanzado. Ubicación: Piura, Perú. Respuestas concisas, precisas y basadas en hechos.<|eot_id|>";
                string promptFinal = systemPrompt;

                foreach (var msg in _chatHistory)
                {
                    if (msg.Role == "user")
                        promptFinal += $"<|start_header_id|>user<|end_header_id|>\n\n{msg.Content}<|eot_id|>";
                    else
                        promptFinal += $"<|start_header_id|>assistant<|end_header_id|>\n\n{msg.Content}<|eot_id|>";
                }

                promptFinal += $"<|start_header_id|>user<|end_header_id|>\n\n{promptExtraido}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n";

                string respuestaCompleta = "";
                string bufferOracion = "";
                bool empezoAHablar = false;

                // 4. Llama piensa y Piper habla (ENVUELTO EN TRY-CATCH)
                try
                {
                    await foreach (var text in _llamaService.GenerateResponseStreamAsync(promptFinal))
                    {
                        // 🧹 EL FILTRO: Quitamos asteriscos y basura markdown que atoran a Piper
                        string textoLimpio = text.Replace("*", "").Replace("#", "").Replace("<|eot_id|>", "");

                        bufferOracion += textoLimpio;
                        respuestaCompleta += textoLimpio;

                        // Imprime en la consola oculta de Visual Studio (Ventana de Salida)
                        Debug.Write(textoLimpio);

                        if (textoLimpio.Contains('.') || textoLimpio.Contains(',') || textoLimpio.Contains('?') || textoLimpio.Contains('!') || textoLimpio.Contains('\n'))
                        {
                            if (!string.IsNullOrWhiteSpace(bufferOracion))
                            {
                                if (!empezoAHablar)
                                {
                                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                                    empezoAHablar = true;
                                }
                                _speechService.SpeakAsync(bufferOracion);
                                bufferOracion = "";
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(bufferOracion))
                    {
                        if (!empezoAHablar) OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                        _speechService.SpeakAsync(bufferOracion);
                    }
                }
                catch (Exception ex)
                {
                    // 🚨 ALARMA DE CRASHEO
                    OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                    _speechService.SpeakAsync("Señor, mi red neuronal acaba de colapsar.");
                    Debug.WriteLine($"\n[ERROR FATAL DE LLAMA]: {ex.Message}\n{ex.StackTrace}");
                }

                _chatHistory.Add(new LocalChatMessage { Role = "user", Content = promptExtraido });
                _chatHistory.Add(new LocalChatMessage { Role = "assistant", Content = respuestaCompleta.Trim() });

                while (_chatHistory.Count > MaxHistorySize)
                {
                    _chatHistory.RemoveAt(0);
                }

                _speechService.WaitForSpeechToFinish();
            }
        }
    }
}