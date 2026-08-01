using System;
using SkiaSharp;
using GasPro.App.Rendering;

namespace GasPro.App.Rendering
{
    public class NeuronalRenderer : IDisposable
    {
        private readonly ParticleSystem _particleSystem;
        private readonly Random _random;

        private float _tiempoGlobal = 0f;

        private float _anguloAnillo1 = 0;
        private float _anguloAnillo2 = 0;
        private float _anguloAnillo3 = 0;
        private float _anguloTextos = 0;
        private float _escalaNucleo = 1.0f;

        private SKColor _colorActual;
        private SKColor _colorObjetivo;

        private readonly SKColor COLOR_REPOSO = SKColor.Parse("#00F0FF");
        private readonly SKColor COLOR_ESCUCHANDO = SKColor.Parse("#FF9900");
        private readonly SKColor COLOR_PENSANDO = SKColor.Parse("#FF3300");
        private readonly SKColor COLOR_HABLANDO = SKColor.Parse("#0077FF");
        private readonly SKColor COLOR_FONDO = SKColor.Parse("#02050A");

        private SKPaint _pincelGrid;
        private SKPaint _pincelParticula;
        private SKPaint _pincelCircuito;
        private SKPaint _pincelTexto;
        private SKPaint _pincelAnillos;
        private SKPaint _pincelOjo;
        private SKPaint _pincelCentro;
        private SKFont _fontConsolas;

        private readonly string[] _codigosFalsos = { "0x4F2A", "SYS.CHK", "MEM_OK", "TENS:42", "NET:ON", "0x88BC", "CORE.INIT", "VOSK.RDY" };

        public NeuronalRenderer(int particleCount = 100)
        {
            _random = new Random();
            _particleSystem = new ParticleSystem(particleCount, _random);

            _colorActual = COLOR_REPOSO;
            _colorObjetivo = COLOR_REPOSO;

            InicializarPinceles();
        }

        private void InicializarPinceles()
        {
            _pincelGrid = new SKPaint { StrokeWidth = 1, IsAntialias = true };
            _pincelParticula = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
            _pincelCircuito = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
            _pincelTexto = new SKPaint { IsAntialias = true };
            _pincelAnillos = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true };
            _pincelOjo = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 30) };
            _pincelCentro = new SKPaint { Color = COLOR_FONDO, Style = SKPaintStyle.Fill, IsAntialias = true };
            _fontConsolas = new SKFont(SKTypeface.FromFamilyName("Consolas"), 14);
        }

        private SKColor InterpolarColor(SKColor actual, SKColor objetivo, float velocidad)
        {
            byte r = (byte)(actual.Red + (objetivo.Red - actual.Red) * velocidad);
            byte g = (byte)(actual.Green + (objetivo.Green - actual.Green) * velocidad);
            byte b = (byte)(actual.Blue + (objetivo.Blue - actual.Blue) * velocidad);
            return new SKColor(r, g, b, actual.Alpha);
        }

        private float InterpolarFloat(float actual, float objetivo, float velocidad)
        {
            return actual + (objetivo - actual) * velocidad;
        }

        public void Update(float deltaTime, object estado)
        {
            if (estado is not MainWindow.EstadoIA estadoIA) return;

            _tiempoGlobal += deltaTime;
            _colorActual = InterpolarColor(_colorActual, _colorObjetivo, 0.05f);

            _particleSystem.Update(800, 600); // bounds are updated at draw time; this is a reasonable fallback

            switch (estadoIA)
            {
                case MainWindow.EstadoIA.Reposo:
                    _anguloAnillo1 += 0.5f; _anguloAnillo2 -= 0.3f; _anguloAnillo3 += 0.1f; _anguloTextos -= 0.2f;
                    _escalaNucleo = InterpolarFloat(_escalaNucleo, 1.0f, 0.1f);
                    _colorObjetivo = COLOR_REPOSO;
                    break;
                case MainWindow.EstadoIA.Escuchando:
                    _anguloAnillo1 += 2.0f; _anguloAnillo2 -= 1.5f; _anguloAnillo3 += 0.5f; _anguloTextos -= 1.0f;
                    _escalaNucleo = InterpolarFloat(_escalaNucleo, 1.2f, 0.1f);
                    _colorObjetivo = COLOR_ESCUCHANDO;
                    break;
                case MainWindow.EstadoIA.Pensando:
                    _anguloAnillo1 += 4.0f; _anguloAnillo2 -= 5.0f; _anguloAnillo3 += 3.0f; _anguloTextos += 2.0f;
                    _escalaNucleo = 1.1f + (float)Math.Sin(_tiempoGlobal * 10) * 0.1f;
                    _colorObjetivo = COLOR_PENSANDO;
                    break;
                case MainWindow.EstadoIA.Hablando:
                    _anguloAnillo1 += 1.0f; _anguloAnillo2 -= 0.8f; _anguloAnillo3 += 0.2f; _anguloTextos -= 0.4f;
                    _escalaNucleo = 1.0f + (float)Math.Abs(Math.Sin(_tiempoGlobal * 5)) * 0.3f;
                    _colorObjetivo = COLOR_HABLANDO;
                    break;
            }
        }

        public void Draw(SKCanvas canvas, float ancho, float alto)
        {
            canvas.Clear(COLOR_FONDO);

            float centroX = ancho / 2f;
            float centroY = alto / 2f;

            // -- CAPA 0: GRID --
            _pincelGrid.Color = _colorActual.WithAlpha(15);
            for (int x = 0; x < ancho; x += 60) canvas.DrawLine(x, 0, x, alto, _pincelGrid);
            for (int y = 0; y < alto; y += 60) canvas.DrawLine(0, y, ancho, y, _pincelGrid);

            // -- CAPA 1: PARTÍCULAS --
            foreach (var p in _particleSystem.Particles)
            {
                _pincelParticula.Color = _colorActual.WithAlpha(p.Opacidad);
                canvas.DrawCircle(p.X, p.Y, p.Tamaño, _pincelParticula);
            }

            canvas.Save();
            canvas.Translate(centroX, centroY);

            // -- CAPA 2: CIRCUITOS --
            _pincelCircuito.Color = _colorActual.WithAlpha(60);
            canvas.DrawLine(-600, 0, -260, 0, _pincelCircuito);
            canvas.DrawLine(260, 0, 600, 0, _pincelCircuito);
            canvas.DrawRect(-400, -5, 10, 10, _pincelCircuito);
            canvas.DrawRect(400, -5, 10, 10, _pincelCircuito);

            // -- CAPA 3: TEXTOS --
            canvas.Save();
            canvas.RotateDegrees(_anguloTextos);
            _pincelTexto.Color = _colorActual.WithAlpha(180);
            float radioTextos = 280;
            for (int i = 0; i < _codigosFalsos.Length; i++)
            {
                float angulo = (i * (360f / _codigosFalsos.Length));
                float x = (float)Math.Cos(angulo * Math.PI / 180) * radioTextos;
                float y = (float)Math.Sin(angulo * Math.PI / 180) * radioTextos;

                canvas.Save();
                canvas.Translate(x, y);
                canvas.RotateDegrees(angulo + 90);
                canvas.DrawText(_codigosFalsos[i], 0, 0, _fontConsolas, _pincelTexto);
                canvas.Restore();
            }
            canvas.Restore();

            // -- CAPA 4: ANILLOS --
            canvas.Save();
            canvas.RotateDegrees(_anguloAnillo3);
            _pincelAnillos.Color = _colorActual.WithAlpha(40);
            _pincelAnillos.StrokeWidth = 2;
            _pincelAnillos.PathEffect = SKPathEffect.CreateDash(new float[] { 2, 8, 20, 10 }, 0);
            canvas.DrawCircle(0, 0, 240, _pincelAnillos);
            canvas.Restore();

            canvas.Save();
            canvas.RotateDegrees(_anguloAnillo2);
            _pincelAnillos.Color = _colorActual.WithAlpha(180);
            _pincelAnillos.StrokeWidth = 4;
            _pincelAnillos.PathEffect = SKPathEffect.CreateDash(new float[] { 80, 20, 10, 20 }, 0);
            canvas.DrawCircle(0, 0, 180, _pincelAnillos);
            _pincelAnillos.PathEffect = null;
            _pincelAnillos.StrokeWidth = 2;
            canvas.DrawArc(new SKRect(-190, -190, 190, 190), 0, 90, false, _pincelAnillos);
            canvas.DrawArc(new SKRect(-190, -190, 190, 190), 180, 90, false, _pincelAnillos);
            canvas.Restore();

            canvas.Save();
            canvas.RotateDegrees(_anguloAnillo1);
            _pincelAnillos.Color = _colorActual.WithAlpha(255);
            _pincelAnillos.StrokeWidth = 10;
            _pincelAnillos.PathEffect = SKPathEffect.CreateDash(new float[] { 200, 50 }, 0);
            canvas.DrawCircle(0, 0, 120 * _escalaNucleo, _pincelAnillos);
            canvas.Restore();

            // -- CAPA 5: EL OJO --
            _pincelOjo.Color = _colorActual.WithAlpha((byte)(50 * _escalaNucleo));
            canvas.DrawCircle(0, 0, 90 * _escalaNucleo, _pincelOjo);

            _pincelAnillos.StrokeWidth = 3;
            _pincelAnillos.PathEffect = null;
            canvas.DrawCircle(0, 0, 40 * _escalaNucleo, _pincelCentro);
            canvas.DrawCircle(0, 0, 40 * _escalaNucleo, _pincelAnillos);

            canvas.Restore();
        }

        public void Dispose()
        {
            _pincelGrid?.Dispose();
            _pincelParticula?.Dispose();
            _pincelCircuito?.Dispose();
            _pincelTexto?.Dispose();
            _pincelAnillos?.Dispose();
            _pincelOjo?.Dispose();
            _pincelCentro?.Dispose();
            _fontConsolas?.Dispose();
        }
    }
}
