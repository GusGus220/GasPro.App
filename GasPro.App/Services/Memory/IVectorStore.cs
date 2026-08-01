using System.Collections.Generic;
using System.Threading.Tasks;

namespace GasPro.Services.Memory
{
    public class MemoryRecord
    {
        public string Id { get; set; }
        public string Role { get; set; }
        public string Text { get; set; }
        public float[] Embedding { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public interface IVectorStore
    {
        Task AddAsync(MemoryRecord record);
        Task<List<MemoryRecord>> QueryAsync(string text, int topK);
        Task InitializeAsync();
    }
}