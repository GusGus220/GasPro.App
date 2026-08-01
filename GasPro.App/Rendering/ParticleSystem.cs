using System;
using System.Collections.Generic;

namespace GasPro.App.Rendering
{
    internal class Particle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocidadY { get; set; }
        public float Tamaño { get; set; }
        public byte Opacidad { get; set; }
    }

    internal class ParticleSystem
    {
        private readonly List<Particle> _particles;
        private readonly Random _random;

        public IReadOnlyList<Particle> Particles => _particles;

        public ParticleSystem(int initialCount, Random random = null)
        {
            _random = random ?? new Random();
            _particles = new List<Particle>(initialCount);
            Init(initialCount);
        }

        public void Init(int count)
        {
            _particles.Clear();
            for (int i = 0; i < count; i++)
            {
                _particles.Add(new Particle
                {
                    X = _random.Next(0, 1920),
                    Y = _random.Next(0, 1080),
                    VelocidadY = ((float)_random.NextDouble() * 1.5f) + 0.5f,
                    Tamaño = ((float)_random.NextDouble() * 2f) + 0.5f,
                    Opacidad = (byte)_random.Next(30, 150)
                });
            }
        }

        public void Update(float width, float height)
        {
            foreach (var p in _particles)
            {
                p.Y -= p.VelocidadY;
                if (p.Y < 0)
                {
                    p.Y = height;
                    p.X = _random.Next(0, (int)Math.Max(1, width));
                }
            }
        }
    }
}
