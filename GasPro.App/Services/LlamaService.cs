using LLama;
using LLama.Common;
using System.Collections.Generic;

namespace GasPro.Services
{
    public class LlamaService
    {
        private LLamaWeights _model;
        private LLamaContext _context;
        private InteractiveExecutor _executor;
        private InferenceParams _inferenceParams;

        public void Initialize(string modelPath)
        {
            // 🐺 LA LLAMADA A VULKAN: Obligamos al sistema a usar la RTX 3050 como si fuera un juego
            LLama.Native.NativeLibraryConfig.Instance.WithVulkan();

            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 4096,
                GpuLayerCount = 99, // 🚀 Mandamos todas las capas neuronales a la gráfica
                UseMemorymap = true
            };

            // Cargamos el modelo UNA SOLA VEZ para no saturar la memoria
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);

            _inferenceParams = new InferenceParams()
            {
                MaxTokens = 200,
                SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline() { Temperature = 0.4f },
                AntiPrompts = new List<string> { "<|eot_id|>", "Usuario:", "User:" }
            };
        }

        public async IAsyncEnumerable<string> GenerateResponseStreamAsync(string promptFinal)
        {
            await foreach (var text in _executor.InferAsync(promptFinal, _inferenceParams))
            {
                yield return text;
            }
        }
    }
}