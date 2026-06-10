using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NAudio.Dsp;
using vcv_etagere_remaster.Front.ViewModel.Modules;

namespace vcv_etagere_remaster.Front.View.Modules
{
    public partial class ScopeView : UserControl
    {
        private const int FftSize = 1024;
        private readonly float[] _leftBuffer = new float[FftSize];
        private readonly float[] _rightBuffer = new float[FftSize];
        private readonly Complex[] _fftLeft = new Complex[FftSize];
        private readonly Complex[] _fftRight = new Complex[FftSize];

        public ScopeView()
        {
            InitializeComponent();
            SizeChanged += (s, e) => DrawGrid();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering += OnRendering;
            DrawGrid();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void DrawGrid()
        {
            GridCanvas.Children.Clear();
            double w = ScreenGrid.ActualWidth;
            double h = ScreenGrid.ActualHeight;
            if (w == 0 || h == 0) return;

            // Draw horizontal dotted lines
            int hDivisions = 8;
            for (int i = 1; i < hDivisions; i++)
            {
                double y = (h / hDivisions) * i;
                var line = new Line
                {
                    X1 = 0, Y1 = y, X2 = w, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 70)), StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 2, 4 }
                };
                GridCanvas.Children.Add(line);
            }

            // Draw vertical dotted lines
            int vDivisions = 10;
            for (int i = 1; i < vDivisions; i++)
            {
                double x = (w / vDivisions) * i;
                var line = new Line
                {
                    X1 = x, Y1 = 0, X2 = x, Y2 = h,
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 70)), StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 2, 4 }
                };
                GridCanvas.Children.Add(line);
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var vm = DataContext as ScopeViewModel;
            if (vm == null || vm.Model == null) return;

            double width = ScreenGrid.ActualWidth;
            double height = ScreenGrid.ActualHeight;
            if (width < 10 || height < 10) return;

            // Retrieve audio buffer snapshot
            vm.Model.GetBufferSnapshot(_leftBuffer, _rightBuffer);

            bool showScope = ModeScopeBtn.IsChecked == true || ModeCombinedBtn.IsChecked == true;
            bool showSpectrum = ModeSpectrumBtn.IsChecked == true || ModeCombinedBtn.IsChecked == true;

            // --- Draw Oscilloscope (Time Domain) ---
            if (showScope)
            {
                LeftWavePath.Visibility = Visibility.Visible;
                RightWavePath.Visibility = Visibility.Visible;

                double center = height / 2.0;
                double amp = height * 0.45; // scale to fit nicely

                // Left Waveform
                StreamGeometry waveLGeom = new StreamGeometry();
                using (StreamGeometryContext ctx = waveLGeom.Open())
                {
                    ctx.BeginFigure(new Point(0, center - _leftBuffer[0] * amp), false, false);
                    for (int i = 1; i < FftSize; i++)
                    {
                        double x = (double)i / (FftSize - 1) * width;
                        double y = center - _leftBuffer[i] * amp;
                        ctx.LineTo(new Point(x, y), true, false);
                    }
                }
                LeftWavePath.Data = waveLGeom;

                // Right Waveform
                StreamGeometry waveRGeom = new StreamGeometry();
                using (StreamGeometryContext ctx = waveRGeom.Open())
                {
                    ctx.BeginFigure(new Point(0, center - _rightBuffer[0] * amp), false, false);
                    for (int i = 1; i < FftSize; i++)
                    {
                        double x = (double)i / (FftSize - 1) * width;
                        double y = center - _rightBuffer[i] * amp;
                        ctx.LineTo(new Point(x, y), true, false);
                    }
                }
                RightWavePath.Data = waveRGeom;
            }
            else
            {
                LeftWavePath.Visibility = Visibility.Collapsed;
                RightWavePath.Visibility = Visibility.Collapsed;
            }

            // --- Draw Spectrum Analyzer (Frequency Domain) ---
            if (showSpectrum)
            {
                LeftFftPath.Visibility = Visibility.Visible;
                LeftFftFill.Visibility = Visibility.Visible;
                RightFftPath.Visibility = Visibility.Visible;
                RightFftFill.Visibility = Visibility.Visible;

                // Prepare FFT inputs with Hann window
                for (int i = 0; i < FftSize; i++)
                {
                    float window = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (FftSize - 1))));
                    _fftLeft[i].X = _leftBuffer[i] * window;
                    _fftLeft[i].Y = 0f;
                    _fftRight[i].X = _rightBuffer[i] * window;
                    _fftRight[i].Y = 0f;
                }

                // Execute FFT
                FastFourierTransform.FFT(true, 10, _fftLeft);
                FastFourierTransform.FFT(true, 10, _fftRight);

                // Draw curves
                int halfSize = FftSize / 2;
                double logMin = Math.Log(1.0);
                double logMax = Math.Log(halfSize);

                // --- Left Channel Spectrum ---
                StreamGeometry lineL = new StreamGeometry();
                StreamGeometry fillL = new StreamGeometry();

                using (var ctxLine = lineL.Open())
                using (var ctxFill = fillL.Open())
                {
                    double firstY = GetFftY(_fftLeft[0], height);
                    ctxLine.BeginFigure(new Point(0, firstY), false, false);
                    ctxFill.BeginFigure(new Point(0, height), true, true);
                    ctxFill.LineTo(new Point(0, firstY), false, false);

                    for (int i = 1; i < halfSize; i++)
                    {
                        double normX = (Math.Log(i + 1) - logMin) / (logMax - logMin);
                        double x = normX * width;
                        double y = GetFftY(_fftLeft[i], height);

                        ctxLine.LineTo(new Point(x, y), true, false);
                        ctxFill.LineTo(new Point(x, y), true, false);
                    }

                    ctxFill.LineTo(new Point(width, height), false, false);
                }

                LeftFftPath.Data = lineL;
                LeftFftFill.Data = fillL;

                // --- Right Channel Spectrum ---
                StreamGeometry lineR = new StreamGeometry();
                StreamGeometry fillR = new StreamGeometry();

                using (var ctxLine = lineR.Open())
                using (var ctxFill = fillR.Open())
                {
                    double firstY = GetFftY(_fftRight[0], height);
                    ctxLine.BeginFigure(new Point(0, firstY), false, false);
                    ctxFill.BeginFigure(new Point(0, height), true, true);
                    ctxFill.LineTo(new Point(0, firstY), false, false);

                    for (int i = 1; i < halfSize; i++)
                    {
                        double normX = (Math.Log(i + 1) - logMin) / (logMax - logMin);
                        double x = normX * width;
                        double y = GetFftY(_fftRight[i], height);

                        ctxLine.LineTo(new Point(x, y), true, false);
                        ctxFill.LineTo(new Point(x, y), true, false);
                    }

                    ctxFill.LineTo(new Point(width, height), false, false);
                }

                RightFftPath.Data = lineR;
                RightFftFill.Data = fillR;
            }
            else
            {
                LeftFftPath.Visibility = Visibility.Collapsed;
                LeftFftFill.Visibility = Visibility.Collapsed;
                RightFftPath.Visibility = Visibility.Collapsed;
                RightFftFill.Visibility = Visibility.Collapsed;
            }
        }

        private double GetFftY(Complex c, double height)
        {
            double mag = Math.Sqrt(c.X * c.X + c.Y * c.Y);
            // dB calculation
            double dB = 20.0 * Math.Log10(mag + 1e-4);
            // Map -45dB..0dB to 0..1 range
            double normY = Math.Clamp((dB + 45.0) / 45.0, 0.0, 1.0);
            return height - (normY * height);
        }
    }
}
