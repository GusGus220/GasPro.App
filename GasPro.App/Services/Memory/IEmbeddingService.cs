using System;

namespace GasPro.Services.Memory
{
    public interface IEmbeddingService
    {
        float[] Embed(string text);
    }
}