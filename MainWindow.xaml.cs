using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using vcv_etagere_remaster.Core.Audio;
using vcv_etagere_remaster.Front.ViewModel;
using vcv_etagere_remaster.Front.ViewModel.Base;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private Engine _engine;
        private PortViewModelBase selectedPortVM = null;
        private FrameworkElement selectedPortVisual = null;
        public Path tempCable = null;
        private List<Cable> allCables = new List<Cable>();
        private bool isDraggingCable = false;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _engine = _viewModel.Engine;
            this.DataContext = _viewModel;
            this.Closed += MainWindow_Closed;

            // Détection globale des clics pour gérer le glisser-déposer de câble
            this.PreviewMouseLeftButtonDown += OnPortMouseDown;
            this.PreviewMouseLeftButtonUp += OnPortMouseUp;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // Stop and dispose the engine via exposed Engine property
            // Use Cable helper to stop and dispose the engine
            try
            {
                Cable.Stop(_engine);
            }
            catch { }
        }

        // =========================================================================
        // CLICKED CLICK
        // =========================================================================
        private void OnPortMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is PortViewModelBase clickedPortVM)
            {
                isDraggingCable = true;
                selectedPortVM = clickedPortVM;
                selectedPortVisual = element;
                this.CaptureMouse(); //capture the mouse movements
                tempCable = new Path { Stroke = Brushes.Yellow, StrokeThickness = 4, IsHitTestVisible = false }; //create a temp cable
                Panel.SetZIndex(tempCable, 1000);
                CableLayer.Children.Add(tempCable);

                this.MouseMove += DragCable;
                e.Handled = true;
            }
        }

        // =========================================================================
        // SLIDING CLICK
        // =========================================================================
        private void DragCable(object sender, MouseEventArgs e)
        {
            if (!isDraggingCable || tempCable == null || selectedPortVisual == null) return;

            var start = selectedPortVisual.TranslatePoint(new Point(selectedPortVisual.ActualWidth / 2, selectedPortVisual.ActualHeight / 2), CableLayer);
            var end = e.GetPosition(CableLayer);
            tempCable.Data = CreateBezier(start, end);
        }

        // =========================================================================
        // RELEASE CLICK
        // =========================================================================
        private void OnPortMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDraggingCable) return;

            isDraggingCable = false;
            this.ReleaseMouseCapture();
            this.MouseMove -= DragCable;

            if (tempCable != null)
            {
                CableLayer.Children.Remove(tempCable);
                tempCable = null;
            }
            var hitResult = VisualTreeHelper.HitTest(MainGrid, e.GetPosition(MainGrid)); //detect the element below the mouse

            FrameworkElement targetVisual = null;
            PortViewModelBase targetPortVM = null;

            if (hitResult != null && hitResult.VisualHit is FrameworkElement hitElement)
            {
                var current = hitElement;
                while (current != null)
                {
                    if (current.DataContext is PortViewModelBase pVM)
                    {
                        targetVisual = current;
                        targetPortVM = pVM;
                        break;
                    }
                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
            }
            if (targetPortVM != null && targetPortVM != selectedPortVM) //confirm port is valid
            {
                IPort sourceModel = GetInternalPort(selectedPortVM);
                IPort destModel = GetInternalPort(targetPortVM);

                if (sourceModel != null && destModel != null)
                {
                    IPort outPortModel = null;
                    IPort inPortModel = null;
                    FrameworkElement outVisual = null;
                    FrameworkElement inVisual = null;

                    if (sourceModel.Type == PortType.Output && destModel.Type == PortType.Input)
                    {
                        outPortModel = sourceModel; outVisual = selectedPortVisual;
                        inPortModel = destModel; inVisual = targetVisual;
                    }
                    else if (sourceModel.Type == PortType.Input && destModel.Type == PortType.Output)
                    {
                        outPortModel = destModel; outVisual = targetVisual;
                        inPortModel = sourceModel; inVisual = selectedPortVisual;
                    }

                    if (outPortModel != null && inPortModel != null)
                    {
                        var path = new Path { Stroke = Brushes.Orange, StrokeThickness = 6, Fill = Brushes.Transparent, Cursor = Cursors.Hand };

                        var start = outVisual.TranslatePoint(new Point(outVisual.ActualWidth / 2, outVisual.ActualHeight / 2), CableLayer);
                        var end = inVisual.TranslatePoint(new Point(inVisual.ActualWidth / 2, inVisual.ActualHeight / 2), CableLayer);

                        path.Data = CreateBezier(start, end);
                        Panel.SetZIndex(path, 900);

                        Cable newCable = new Cable(outPortModel, inPortModel); //create the cable preset
                        allCables.Add(newCable);
                        CableLayer.Children.Add(path);
                        newCable.AddCable(_engine); // Register the cable with the audio engine so it will be processed
                        path.MouseRightButtonDown += (s, args) => { RemoveCable(newCable, path); }; //right click = remove cable

                    }
                }
            }

            selectedPortVM = null;
            selectedPortVisual = null;
            e.Handled = true;
        }

        // =========================================================================
        // DRAW CABLE
        // =========================================================================
        private PathGeometry CreateBezier(Point start, Point end)
        {
            double offset = Math.Abs(end.X - start.X) * 0.5;
            var figure = new PathFigure { StartPoint = start };
            var bezier = new BezierSegment
            {
                Point1 = new Point(start.X + offset, start.Y),
                Point2 = new Point(end.X - offset, end.Y),
                Point3 = end,
                IsStroked = true
            };
            figure.Segments.Add(bezier);
            return new PathGeometry(new[] { figure });
        }
        //==============================
        //DELETE CABLE
        //==============================
        private void RemoveCable(Cable cable, Path path)
        {
            if (cable == null) return;
            cable.Destination.Value = 0;

            CableLayer.Children.Remove(path);
            allCables.Remove(cable);

            // Unregister from audio engine
            cable.RemoveCable(_engine);
        }
        //==============================
        //EXTRACT FROM VIEWMODEL TO INTERNAL
        //==============================
        private IPort GetInternalPort(PortViewModelBase vm)
        {
            if (vm == null) return null;

            var portProp = vm.GetType().GetProperty("Port");
            if (portProp != null) return portProp.GetValue(vm) as IPort;

            var modelProp = vm.GetType().GetProperty("Model");
            if (modelProp != null) return modelProp.GetValue(vm) as IPort;

            return null;
        }
    }
}