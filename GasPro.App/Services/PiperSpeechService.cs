using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using PiperSharp;
using PiperSharp.Models;

namespace GasPro.Services
{
    public class PiperSpeechService
    {
        private PiperProvider _piperProvider;
        private bool _isInitialized = false;

        // 🚦 LA COLA MÁGICA: Mantiene el orden exacto y permite procesar en segundo plano
        private readonly BlockingCollection<Task<byte[]>> _colaDeAudio = new BlockingCollection<Task<byte[]>>();

        public async Task InitializeAsync(string modelKey, string modelsDirectory)
        {
            try
            {
                Console.WriteLine("[Piper] Iniciando Pipeline de Voz Predictivo...");
                string cwd = Directory.GetCurrentDirectory();
                string piperDir = Path.Combine(cwd, "piper");

                modelKey = modelKey.Replace(".onnx", "");

                // Descarga segura oficial de PiperSharp
                if (!Directory.Exists(piperDir) || !File.Exists(Path.Combine(piperDir, "piper.exe")))
                {
                    await PiperDownloader.DownloadPiper().ExtractPiper(cwd);
                }

                var modelInfo = await PiperDownloader.GetModelByKey(modelKey);
                if (modelInfo == null) return;

                var downloadedModel = await modelInfo.DownloadModel(cwd);

                _piperProvider = new PiperProvider(new PiperConfiguration()
                {
                    ExecutableLocation = Path.Combine(piperDir, "piper.exe"),
                    WorkingDirectory = piperDir,
                    Model = downloadedModel
                });

                _isInitialized = true;

                // Arrancamos al "DJ" que se encargará de reproducir la cola en orden estricto
                Task.Run(() => ReproducirColaEnOrden());

                Console.WriteLine("[Piper] ¡Motor de Voz Fluido y Seguro activado!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error Piper] {ex.Message}");
            }
        }

        public void SpeakAsync(string text)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(text)) return;

            // Limpiamos basura de la IA (como asteriscos de Markdown) para que Piper no se trabe
            string textoLimpio = text.Replace("*", "").Replace("_", "").Replace("\"", "").Trim();

            // Si solo quedó un punto o un número suelto, lo ignoramos para que no corte el audio
            if (textoLimpio.Length < 2) return;

            try
            {
                // 1. Iniciamos la generación neuronal EN SEGUNDO PLANO al instante
                var tareaDeInferencia = _piperProvider.InferAsync(textoLimpio, AudioOutputType.Wav);

                // 2. Metemos la TAREA a la cola (Esto garantiza que NUNCA se salte el orden)
                _colaDeAudio.Add(tareaDeInferencia);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error al encolar] {ex.Message}");
            }
        }

        // Este método vive en segundo plano escuchando la cola para siempre
        private void ReproducirColaEnOrden()
        {
            // Pide el siguiente audio de la cola en orden FIFO (First In, First Out)
            foreach (var tareaAudio in _colaDeAudio.GetConsumingEnumerable())
            {
                try
                {
                    // Esperamos a que termine de generarse ESTA oración específica
                    byte[] audioBytes = tareaAudio.GetAwaiter().GetResult();

                    if (audioBytes != null && audioBytes.Length > 0)
                    {
                        // Reproducimos de forma limpia desde la memoria
                        using (var ms = new MemoryStream(audioBytes))
                        using (var waveReader = new WaveFileReader(ms))
                        using (var outputDevice = new WaveOutEvent())
                        {
                            outputDevice.Init(waveReader);
                            outputDevice.Play();

                            while (outputDevice.PlaybackState == PlaybackState.Playing)
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Fallo en reproducción] {ex.Message}");
                }
            }
        }

        public void WaitForSpeechToFinish()
        {
            // Espera a que el Orquestador termine de hablar antes de volver a escuchar tu micrófono
            while (_colaDeAudio.Count > 0)
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(300); // Pequeño margen para que el eco se disipe de tu cuarto
        }
    }
}