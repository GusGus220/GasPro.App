using GasPro.App.Services.Handlers;
using GasPro.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics; // Usamos esto para imprimir errores sin crashear WPF
using System.Threading.Tasks;

namespace GasPro.App
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
        private readonly List<IComandoHandler> _listaReflejos;

        // ⚡ EL CABLE NERVIOSO HACIA JARVIS
        public Action<MainWindow.EstadoIA> OnCambioEstado;
        private readonly IEnumerable<ISystemCommandHandler> _systemHandlers;

        // Soporta inyección de handlers para DI/testing. Si es null, se crean handlers por defecto.
        public GasOrchestrator(IEnumerable<ISystemCommandHandler> systemHandlers = null)
        {
            _llamaService = new LlamaService();
            _speechService = new PiperSpeechService();
            _audioService = new AudioService();
            _windowsService = new WindowsControlService();
            _chatHistory = new List<LocalChatMessage>();

            _systemHandlers = systemHandlers ?? new List<ISystemCommandHandler>
            {
                new SpotifyHandler(_speechService, _windowsService),
                new BrowserHandler(_speechService, _windowsService),
                new DiscordHandler(_speechService, _windowsService),
                new TimeHandler(_speechService),
                new DateHandler(_speechService),
                new ClickHandler(_speechService, _windowsService),
                new MediaHandler(_speechService, _windowsService),
                new OnOffHandler(_speechService, _windowsService),
                new VolumeHandler(_speechService, _windowsService)
            };
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

                string comando = promptExtraido.ToLower();

                // Intentamos procesar comandos del sistema de forma separada
                bool handled = await TryHandleSystemCommandAsync(comando);
                if (handled)
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

        // Delegamos a los handlers registrados
        private async Task<bool> TryHandleSystemCommandAsync(string comando)
        {
            foreach (var handler in _systemHandlers)
            {
                try
                {
                    if (handler.CanHandle(comando))
                    {
                        OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                        await handler.HandleAsync(comando);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR en handler {handler.GetType().Name}]: {ex.Message}");
                }
            }

            return false;
        }
    }
}