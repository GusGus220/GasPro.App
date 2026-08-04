using LLama;
using LLama.Common;
using System.Collections.Generic;

namespace GasPro.Services
{
    public class LlamaService
    {
        private LLamaWeights _model;
        private LLamaContext _context;

        // 🛠️ Cambiamos InteractiveExecutor por StatelessExecutor
        private StatelessExecutor _executor;

        private InferenceParams _inferenceParams;

        public void Initialize(string modelPath)
        {
            // 🐺 LA LLAMADA A VULKAN: Obligamos al sistema a usar la RTX 3050
            LLama.Native.NativeLibraryConfig.Instance.WithVulkan();

            var parameters = new ModelParams(@"models\qwen2.5-3b-instruct-q4_k_m.gguf")
            {
                ContextSize = 4096,
                GpuLayerCount = 99, // 🚀 Todas las capas a la GPU
                UseMemorymap = true
            };

            // Cargamos el modelo UNA SOLA VEZ
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);

            // 💡 Inicializamos el ejecutor sin estado
            _executor = new StatelessExecutor(_model, parameters);

            _inferenceParams = new InferenceParams()
            {
                MaxTokens = 200,
                SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline() { Temperature = 0.4f },
                AntiPrompts = new List<string> { "<|eot_id|>", "Usuario:", "User:", "<|im_end|>" }
            };
        }

        public async IAsyncEnumerable<string> GenerateResponseStreamAsync(string promptFinal)
        {
            // El StatelessExecutor procesa el prompt completo que le manda el Orquestador sin desincronizarse
            await foreach (var text in _executor.InferAsync(promptFinal, _inferenceParams))
            {
                yield return text;
            }
        }
    }
}