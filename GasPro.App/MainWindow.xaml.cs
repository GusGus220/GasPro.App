using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace GasPro.App
{
    public partial class MainWindow : Window
    {
        private GasOrchestrator _cerebroGasPro;

        public enum EstadoIA { Reposo, Escuchando, Pensando, Hablando }
        private EstadoIA _estadoActual = EstadoIA.Reposo;

        private class Particula
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float VelocidadY { get; set; }
            public float Tamaño { get; set; }
            public byte Opacidad { get; set; }
        }

        private List<Particula> _particulas;
        private Random _random = new Random();
        private string[] _codigosFalsos = { "0x4F2A", "SYS.CHK", "MEM_OK", "TENS:42", "NET:ON", "0x88BC", "CORE.INIT", "VOSK.RDY" };

        private DispatcherTimer _timerMotorGrafico;
        private float _tiempoGlobal = 0;

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

        // ==========================================
        // 🚀 OPTIMIZACIÓN MÁXIMA: PINCELES CACHEADOS
        // ==========================================
        private SKPaint _pincelGrid;
        private SKPaint _pincelParticula;
        private SKPaint _pincelCircuito;
        private SKPaint _pincelTexto;
        private SKPaint _pincelAnillos;
        private SKPaint _pincelOjo;
        private SKPaint _pincelCentro;
        private SKFont _fontConsolas;

        public MainWindow()
        {
            InitializeComponent();
            _colorActual = COLOR_REPOSO;
            _colorObjetivo = COLOR_REPOSO;

            InicializarParticulas(100);
            InicializarPinceles(); // Creamos la memoria gráfica UNA sola vez

            _timerMotorGrafico = new DispatcherTimer();
            _timerMotorGrafico.Interval = TimeSpan.FromMilliseconds(16);
            _timerMotorGrafico.Tick += MotorGrafico_Tick;
            _timerMotorGrafico.Start();

            this.Loaded += IniciarInteligenciaArtificial;
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
            // Mantenemos el nivel de transparencia (Alpha) original del color actual
            return new SKColor(r, g, b, actual.Alpha);
        }

        private float InterpolarFloat(float actual, float objetivo, float velocidad)
        {
            return actual + (objetivo - actual) * velocidad;
        }

        private void InicializarParticulas(int cantidad)
        {
            _particulas = new List<Particula>();
            for (int i = 0; i < cantidad; i++)
            {
                _particulas.Add(new Particula
                {
                    X = _random.Next(0, 1920),
                    Y = _random.Next(0, 1080),
                    VelocidadY = ((float)_random.NextDouble() * 1.5f) + 0.5f,
                    Tamaño = ((float)_random.NextDouble() * 2f) + 0.5f,
                    Opacidad = (byte)_random.Next(30, 150)
                });
            }
        }

        private void MotorGrafico_Tick(object sender, EventArgs e)
        {
            _tiempoGlobal += 0.05f;
            _colorActual = InterpolarColor(_colorActual, _colorObjetivo, 0.05f);

            foreach (var p in _particulas)
            {
                p.Y -= p.VelocidadY;
                if (p.Y < 0)
                {
                    p.Y = (float)LienzoNeuronal.ActualHeight;
                    p.X = _random.Next(0, (int)LienzoNeuronal.ActualWidth);
                }
            }

            switch (_estadoActual)
            {
                case EstadoIA.Reposo:
                    _anguloAnillo1 += 0.5f; _anguloAnillo2 -= 0.3f; _anguloAnillo3 += 0.1f; _anguloTextos -= 0.2f;
                    _escalaNucleo = InterpolarFloat(_escalaNucleo, 1.0f, 0.1f);
                    break;
                case EstadoIA.Escuchando:
                    _anguloAnillo1 += 2.0f; _anguloAnillo2 -= 1.5f; _anguloAnillo3 += 0.5f; _anguloTextos -= 1.0f;
                    _escalaNucleo = InterpolarFloat(_escalaNucleo, 1.2f, 0.1f);
                    break;
                case EstadoIA.Pensando:
                    _anguloAnillo1 += 4.0f; _anguloAnillo2 -= 5.0f; _anguloAnillo3 += 3.0f; _anguloTextos += 2.0f;
                    _escalaNucleo = 1.1f + (float)Math.Sin(_tiempoGlobal * 10) * 0.1f;
                    break;
                case EstadoIA.Hablando:
                    _anguloAnillo1 += 1.0f; _anguloAnillo2 -= 0.8f; _anguloAnillo3 += 0.2f; _anguloTextos -= 0.4f;
                    _escalaNucleo = 1.0f + (float)Math.Abs(Math.Sin(_tiempoGlobal * 5)) * 0.3f;
                    break;
            }

            LienzoNeuronal.InvalidateVisual();
        }

        private void LienzoNeuronal_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear(COLOR_FONDO);

            float ancho = e.Info.Width;
            float alto = e.Info.Height;
            float centroX = ancho / 2f;
            float centroY = alto / 2f;

            // -- CAPA 0: GRID --
            _pincelGrid.Color = _colorActual.WithAlpha(15);
            for (int x = 0; x < ancho; x += 60) canvas.DrawLine(x, 0, x, alto, _pincelGrid);
            for (int y = 0; y < alto; y += 60) canvas.DrawLine(0, y, ancho, y, _pincelGrid);

            // -- CAPA 1: PARTÍCULAS --
            foreach (var p in _particulas)
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

        public void CambiarEstado(EstadoIA nuevoEstado)
        {
            _estadoActual = nuevoEstado;

            if (nuevoEstado == EstadoIA.Reposo) _colorObjetivo = COLOR_REPOSO;
            else if (nuevoEstado == EstadoIA.Escuchando) _colorObjetivo = COLOR_ESCUCHANDO;
            else if (nuevoEstado == EstadoIA.Pensando) _colorObjetivo = COLOR_PENSANDO;
            else if (nuevoEstado == EstadoIA.Hablando) _colorObjetivo = COLOR_HABLANDO;
        }

        private async void IniciarInteligenciaArtificial(object sender, RoutedEventArgs e)
        {
            _cerebroGasPro = new GasOrchestrator();

            _cerebroGasPro.OnCambioEstado = (nuevoEstado) =>
            {
                Dispatcher.Invoke(() => CambiarEstado(nuevoEstado));
            };

            try
            {
                string directorioBase = AppDomain.CurrentDomain.BaseDirectory;
                string rutaLlama = System.IO.Path.Combine(directorioBase, "models", "llama", "Llama-3.2-3B-Instruct-Q4_K_M.gguf");
                string rutaVosk = System.IO.Path.Combine(directorioBase, "models", "vosk");

                await Task.Run(async () => await _cerebroGasPro.InitializeAsync(rutaLlama, rutaVosk));
                _ = Task.Run(async () => await _cerebroGasPro.RunAsync());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fatal al encender el núcleo: {ex.Message}");
            }
        }
    }
}