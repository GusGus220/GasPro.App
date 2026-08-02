using GasPro.App.Services.Handlers;
using GasPro.Services;
using System;
using System.IO;
using GasPro.Services.Memory;
using System.Collections.Generic;
using System.Diagnostics; // Usamos esto para imprimir errores sin crashear WPF
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using GasPro.App.Network; // Para que reconozca JarvisHub

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
        private const int MaxHistorySize = 5; // ampliado para mejor contexto corto
        private readonly GasPro.Services.Memory.IEmbeddingService _embedder;
        private GasPro.Services.Memory.IVectorStore _vectorStore;
        private readonly string _memoryPath;
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

            // Inicializamos el embedder y el vector store (memoria a largo plazo)
            _embedder = new HashedEmbeddingService(1024);
            // Solo almacenamos la ruta; el _vectorStore se asignará e inicializará en InitializeAsync
            _memoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory_store.json");

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

            // NOTE: la inicialización del store se realiza de forma asíncrona en InitializeAsync
        }

        // Lo convertimos en Async para que la UI no se congele al cargar Gigabytes
        public async Task InitializeAsync(string llamaModelPath, string voskModelPath)
        {
            // Inicializamos la memoria a largo plazo de forma asíncrona.
            // Si no se proporcionó un IVectorStore por DI, lo creamos aquí y lo inicializamos.
            try
            {
                if (_vectorStore == null)
                {
                    var memPath = _memoryPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory_store.json");
                    _vectorStore = new JsonVectorStore(memPath, _embedder);
                }

                await _vectorStore.InitializeAsync();
            }
            catch (Exception ex) { Debug.WriteLine($"[VectorStore init error]: {ex.Message}"); }

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

            // 🌐 LEVANTAR EL SERVIDOR DE TELEPATÍA (SIGNALR)
            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.Services.AddSignalR();
                var app = builder.Build();

                // Conectamos la ruta "/jarvis" con nuestra antena
                app.MapHub<JarvisHub>("/jarvis");

                // Le decimos que escuche en todas las IPs de tu red local por el puerto 5000
                _ = app.RunAsync("http://0.0.0.0:5000");

                Debug.WriteLine("Servidor SignalR iniciado exitosamente en el puerto 5000.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al iniciar el servidor SignalR: {ex.Message}");
            }

            // 👇 EL SALUDO DE JARVIS!
            _speechService.SpeakAsync("Sistemas en línea y motores calibrados. Estoy listo, señor.");
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

                // Recuperamos memorias semánticas relevantes (memoria a largo plazo)
                string promptFinal = systemPrompt;
                try
                {
                    var memories = await _vectorStore.QueryAsync(promptExtraido, 5);
                    if (memories != null && memories.Count > 0)
                    {
                        foreach (var mem in memories)
                        {
                            promptFinal += $"<|start_header_id|>memory<|end_header_id|>\n\n{mem.Text}<|eot_id|>";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Memory query error]: {ex.Message}");
                }

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

                // Guardamos en la memoria local (short-term) y también persistimos en el vector store (long-term)
                _chatHistory.Add(new LocalChatMessage { Role = "user", Content = promptExtraido });
                _chatHistory.Add(new LocalChatMessage { Role = "assistant", Content = respuestaCompleta.Trim() });

                // Persistimos de forma asíncrona en el vector store para memoria a largo plazo
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _vectorStore.AddAsync(new GasPro.Services.Memory.MemoryRecord
                        {
                            Id = Guid.NewGuid().ToString(),
                            Role = "user",
                            Text = promptExtraido
                        });

                        await _vectorStore.AddAsync(new GasPro.Services.Memory.MemoryRecord
                        {
                            Id = Guid.NewGuid().ToString(),
                            Role = "assistant",
                            Text = respuestaCompleta.Trim()
                        });
                    }
                    catch (Exception mex)
                    {
                        Debug.WriteLine($"[Memory store save error]: {mex.Message}");
                    }
                });

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