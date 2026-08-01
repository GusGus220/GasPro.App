using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GasPro.Services.Memory
{
    public class JsonVectorStore : IVectorStore, IDisposable
    {
        private readonly string _path;
        private readonly IEmbeddingService _embedder;
        private readonly List<MemoryRecord> _records = new List<MemoryRecord>();
        private readonly object _lock = new object();

        private readonly int _batchSize;
        private readonly int _flushIntervalMs;
        private readonly int _ttlDays;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _writerTask;
        private volatile bool _dirty = false;

        public JsonVectorStore(string path, IEmbeddingService embedder, int batchSize = 16, int flushIntervalMs = 2000, int ttlDays = 30)
        {
            _path = path;
            _embedder = embedder;
            _batchSize = Math.Max(1, batchSize);
            _flushIntervalMs = Math.Max(200, flushIntervalMs);
            _ttlDays = Math.Max(1, ttlDays);

            // Start background writer
            _writerTask = Task.Run(() => WriterLoopAsync(_cts.Token));
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = await File.ReadAllTextAsync(_path);
                    var data = JsonSerializer.Deserialize<List<MemoryRecord>>(json);
                    if (data != null)
                    {
                        lock (_lock) { _records.AddRange(data); }
                    }
                }

                // Compact on startup
                CompactInternal();
            }
            catch { }
        }

        public Task AddAsync(MemoryRecord record)
        {
            if (record == null) return Task.CompletedTask;
            if (string.IsNullOrWhiteSpace(record.Id)) record.Id = Guid.NewGuid().ToString();
            if (record.Timestamp == default) record.Timestamp = DateTime.UtcNow;
            if (record.Embedding == null || record.Embedding.Length == 0)
            {
                record.Embedding = _embedder.Embed(record.Text);
            }

            lock (_lock)
            {
                _records.Add(record);
                _dirty = true;
            }

            return Task.CompletedTask; // fast return, writer will persist
        }

        public Task<List<MemoryRecord>> QueryAsync(string text, int topK)
        {
            var queryEmb = _embedder.Embed(text);
            var list = new List<(MemoryRecord rec, float score)>();
            lock (_lock)
            {
                foreach (var r in _records)
                {
                    if (r.Embedding == null) continue;
                    float s = CosineSimilarity(queryEmb, r.Embedding);
                    list.Add((r, s));
                }
            }

            var top = list.OrderByDescending(x => x.score).Take(topK).Where(x => x.score > 0.01f).Select(x => x.rec).ToList();
            return Task.FromResult(top);
        }

        private async Task WriterLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(_flushIntervalMs, ct).ConfigureAwait(false);

                    if (!_dirty) continue;

                    List<MemoryRecord> snapshot;
                    lock (_lock)
                    {
                        CompactInternal();
                        snapshot = new List<MemoryRecord>(_records);
                        _dirty = false;
                    }

                    try
                    {
                        var json = JsonSerializer.Serialize(snapshot);
                        await File.WriteAllTextAsync(_path, json, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Log and continue
                        Console.WriteLine($"[JsonVectorStore write error] {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void CompactInternal()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-_ttlDays);
                _records.RemoveAll(r => r.Timestamp < cutoff);
            }
            catch { }
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null) return 0f;
            int n = Math.Min(a.Length, b.Length);
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < n; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na == 0 || nb == 0) return 0f;
            return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
        }

        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                _writerTask?.Wait(2000);
                // One final flush
                List<MemoryRecord> snapshot;
                lock (_lock) { CompactInternal(); snapshot = new List<MemoryRecord>(_records); }
                try { File.WriteAllText(_path, JsonSerializer.Serialize(snapshot)); } catch { }
            }
            catch { }
        }
    }
}
