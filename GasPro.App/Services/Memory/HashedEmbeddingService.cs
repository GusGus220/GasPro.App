using System;
using System.Text.RegularExpressions;

namespace GasPro.Services.Memory
{
    public class HashedEmbeddingService : IEmbeddingService
    {
        private readonly int _dimension;

        public HashedEmbeddingService(int dimension = 1024)
        {
            _dimension = Math.Max(64, dimension);
        }

        public float[] Embed(string text)
        {
            var vec = new float[_dimension];
            if (string.IsNullOrWhiteSpace(text)) return vec;

            // tokenizar palabras simples
            var tokens = Regex.Split(text.ToLowerInvariant(), "\\W+");
            if (tokens.Length == 0) return vec;

            foreach (var t in tokens)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                int h = t.GetHashCode();
                int idx = Math.Abs(h) % _dimension;
                vec[idx] += 1f;
            }

            // L2 normalización
            double sum = 0;
            for (int i = 0; i < _dimension; i++) sum += vec[i] * vec[i];
            if (sum > 0)
            {
                double norm = Math.Sqrt(sum);
                for (int i = 0; i < _dimension; i++) vec[i] = (float)(vec[i] / norm);
            }

            return vec;
        }
    }
}