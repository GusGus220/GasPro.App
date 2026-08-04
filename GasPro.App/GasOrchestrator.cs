using GasPro.App.Services.Handlers;
using GasPro.Services;
using System;
using System.IO;
using GasPro.Services.Memory;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using GasPro.App.Network;

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
        private const int MaxHistorySize = 5;
        private readonly GasPro.Services.Memory.IEmbeddingService _embedder;
        private GasPro.Services.Memory.IVectorStore _vectorStore;
        private readonly string _memoryPath;
        private readonly List<IComandoHandler> _listaReflejos;
        private CancellationTokenSource? _currentCts;

        // ⚡ EL CABLE NERVIOSO HACIA JARVIS
        public Action<MainWindow.EstadoIA> OnCambioEstado;
        private readonly IEnumerable<ISystemCommandHandler> _systemHandlers;

        public GasOrchestrator(IEnumerable<ISystemCommandHandler> systemHandlers = null)
        {
            _llamaService = new LlamaService();
            _speechService = new PiperSpeechService();
            _audioService = new AudioService();
            _windowsService = new WindowsControlService();
            _chatHistory = new List<LocalChatMessage>();

            _embedder = new HashedEmbeddingService(1024);
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
        }

        public async Task InitializeAsync(string llamaModelPath, string voskModelPath)
        {
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

            _llamaService.Initialize(llamaModelPath);
            _speechService.InitializeAsync("es_ES-sharvard-medium", "models/piper").GetAwaiter().GetResult();
            _audioService.Initialize(voskModelPath);

            try
            {
                var enumerador = _llamaService.GenerateResponseStreamAsync("a").GetAsyncEnumerator();
                await enumerador.MoveNextAsync();
            }
            catch { }

            OnCambioEstado?.Invoke(MainWindow.EstadoIA.Reposo);

            // 🌐 LEVANTAR EL SERVIDOR DE TELEPATÍA (SIGNALR)
            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.Services.AddSignalR(options =>
                {
                    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
                });
                var app = builder.Build();

                app.MapHub<JarvisHub>("/jarvis");

                // 🔌 CONECTAMOS EL CABLE TÁCTICO
                GasPro.App.Network.JarvisHub.OnComandoTacticoRecibido += ProcesarAudioCelular;

                _ = app.RunAsync("http://0.0.0.0:5000");
                Debug.WriteLine("Servidor SignalR iniciado exitosamente en el puerto 5000.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al iniciar el servidor SignalR: {ex.Message}");
            }

            _speechService.SpeakAsync("Sistemas en línea y motores calibrados. Estoy listo señor.");
        }

        // ========================================================
        // 🎙️ FLUJO 1: MICRÓFONO DE LA PC
        // ========================================================
        public async Task RunAsync()
        {
            while (true)
            {
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Reposo);

                string promptExtraido = await _audioService.ListenForPromptAsync();

                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Escuchando);
                await Task.Delay(400);

                if (string.IsNullOrWhiteSpace(promptExtraido)) continue;
                promptExtraido = promptExtraido.Trim();

                if (promptExtraido.Contains("salir") || promptExtraido.Contains("apágate")) break;

                // Mandamos el texto al Cerebro Central
                await ProcesarTextoYResponderAsync(promptExtraido);
            }
        }

        // ========================================================
        // 📱 FLUJO 2: AUDIO DESDE EL CELULAR
        // ========================================================
        private async void ProcesarAudioCelular(string rutaAudioArchivo)
        {
            Debug.WriteLine($"🧠 Jarvis procesando archivo táctico...");
            OnCambioEstado?.Invoke(MainWindow.EstadoIA.Pensando);

            try
            {
                // Delegamos la transcripción al servicio experto
                string textoReconocido = _audioService.TranscribirArchivoWav(rutaAudioArchivo);

                if (!string.IsNullOrWhiteSpace(textoReconocido))
                {
                    Debug.WriteLine($"🗣️ Celular dijo: {textoReconocido}");

                    // Mandamos el texto al Cerebro Central (¡El mismo que usa la PC!)
                    await ProcesarTextoYResponderAsync(textoReconocido);
                }
                else
                {
                    Debug.WriteLine("⚠️ No se entendió el audio táctico.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al procesar audio del celular: {ex.Message}");
            }
            finally
            {
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Reposo);
            }
        }

        // ========================================================
        // 🧠 EL CEREBRO CENTRAL (Reutilizable para PC y Celular)
        // ========================================================
        private async Task ProcesarTextoYResponderAsync(string textoEntrada)
        {
            string comando = textoEntrada.ToLower();

            // 🛑 1. FILTRO DE INTERRUPCIÓN (Puesto al inicio de todo)
            if (comando.Contains("cállate") || comando.Contains("silencio") || comando.Contains("para"))
            {
                _currentCts?.Cancel(); // ¡Mata el proceso de Qwen y frena todo al instante!
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Reposo);
                Debug.WriteLine("\n[GAS]: ¡Me callo!");
                return; // Corta el flujo por completo
            }

            // 1. Verificamos si es un comando rápido (Spotify, Volumen, etc.)
            bool handled = await TryHandleSystemCommandAsync(comando);
            if (handled)
            {
                _speechService.WaitForSpeechToFinish();
                return;
            }

            // 2. Si no es comando, pasa a Qwen (Modo Pensar)
            OnCambioEstado?.Invoke(MainWindow.EstadoIA.Pensando);

            // Usamos el system prompt oficial con formato ChatML de Qwen
            string promptFinal = "<|im_start|>system\nEres GAS PRO, asistente de IA avanzado. Ubicación: Piura, Perú. Respuestas concisas, precisas y basadas en hechos.<|im_end|>\n";

            try
            {
                var memories = await _vectorStore.QueryAsync(textoEntrada, 5);
                if (memories != null && memories.Count > 0)
                {
                    // Inyectamos las memorias como contexto del sistema o del usuario en formato ChatML
                    promptFinal += "<|im_start|>system\n[Información de memoria relevante]:\n";
                    foreach (var mem in memories)
                    {
                        promptFinal += $"- {mem.Text}\n";
                    }
                    promptFinal += "<|im_end|>\n";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Memory query error]: {ex.Message}");
            }

            // Inyectamos el historial de chat con las etiquetas correctas de Qwen (<|im_start|> / <|im_end|>)
            foreach (var msg in _chatHistory)
            {
                if (msg.Role == "user")
                    promptFinal += $"<|im_start|>user\n{msg.Content}<|im_end|>\n";
                else
                    promptFinal += $"<|im_start|>assistant\n{msg.Content}<|im_end|>\n";
            }

            // Finalmente, metemos la entrada actual del usuario y preparamos para que el asistente responda
            promptFinal += $"<|im_start|>user\n{textoEntrada}<|im_end|>\n<|im_start|>assistant\n";

            string respuestaCompleta = "";
            string bufferOracion = "";
            bool empezoAHablar = false;

            // 🔥 CONFIGURAMOS EL TOKEN DE CANCELACIÓN PARA ESTA SESIÓN
            _currentCts?.Cancel();
            _currentCts = new CancellationTokenSource();
            var token = _currentCts.Token;

            try
            {
                // Mantenemos el método original de LlamaService y controlamos la cancelación dentro del bucle
                await foreach (var text in _llamaService.GenerateResponseStreamAsync(promptFinal))
                {
                    token.ThrowIfCancellationRequested();

                    string textoLimpio = text.Replace("*", "").Replace("#", "").Replace("<|eot_id|>", "");

                    bufferOracion += textoLimpio;
                    respuestaCompleta += textoLimpio;
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
            catch (OperationCanceledException)
            {
                // Esto se ejecuta cuando el usuario dice "cállate" a mitad del texto
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Reposo);
                Debug.WriteLine("\n[GAS]: Generación interrumpida por el usuario.");
                return; // Salimos limpiamente sin guardar basura ni colapsar
            }
            catch (Exception ex)
            {
                OnCambioEstado?.Invoke(MainWindow.EstadoIA.Hablando);
                _speechService.SpeakAsync("Señor, mi red neuronal acaba de colapsar.");
                Debug.WriteLine($"\n[ERROR FATAL DE LLAMA]: {ex.Message}\n{ex.StackTrace}");
            }

            // Guardar memorias (Corto y Largo plazo) solo si terminó de hablar con éxito
            _chatHistory.Add(new LocalChatMessage { Role = "user", Content = textoEntrada });
            _chatHistory.Add(new LocalChatMessage { Role = "assistant", Content = respuestaCompleta.Trim() });

            _ = Task.Run(async () =>
            {
                try
                {
                    await _vectorStore.AddAsync(new GasPro.Services.Memory.MemoryRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        Role = "user",
                        Text = textoEntrada
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

        // ========================================================
        // ⚙️ MANEJADOR DE COMANDOS DEL SISTEMA
        // ========================================================
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