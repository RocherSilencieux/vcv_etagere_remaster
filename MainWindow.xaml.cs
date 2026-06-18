using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using vcv_etagere_remaster.Core.Audio;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Front.ViewModel;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel = null!;
        private Engine _engine = null!;
        private PortViewModelBase? selectedPortVM = null;
        private FrameworkElement? selectedPortVisual = null;
        public Path? tempCable = null;
        private List<Cable> allCables = new List<Cable>();
        private bool isDraggingCable = false;
        private const double GridSize = 20.0;
        private static readonly string[] CableColors = new[]
        {
            "#00f5ff", // Neon Cyan
            "#ff007f", // Hot Pink
            "#ff6b00", // Electric Orange
            "#ffca80", // Amber Gold
            "#39ff14", // Acid Green
            "#9d4edd", // Violet
            "#00b4d8"  // Sky Blue
        };
        private static readonly Random _randomColorGen = new Random();

        // --- État du drag ---
        private ModuleViewModuleBase? _draggedModule;
        private Point _dragStartMouse;
        private Point _dragStartModulePos;
        private bool _isDragging;

        // Position mémorisée au moment du clic droit
        private Point _rightClickPosition;

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

            _viewModel.Modules.CollectionChanged += OnModulesCollectionChanged;
            _viewModel.ModuleRemoving += OnModuleRemoving;
            this.Loaded += (s, e) => TriggerSplashAnimation();

            // Performance monitoring
            CompositionTarget.Rendering += UpdatePerformanceStats;
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
            var hitObj = e.OriginalSource as DependencyObject;
            FrameworkElement? clickedVisual = null;
            PortViewModelBase? clickedPortVM = null;

            var current = hitObj;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is PortViewModelBase pVM)
                {
                    clickedVisual = fe;
                    clickedPortVM = pVM;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            if (clickedPortVM != null && clickedVisual != null)
            {
                var ellipse = FindPortEllipse(clickedVisual);

                isDraggingCable = true;
                selectedPortVM = clickedPortVM;
                selectedPortVisual = ellipse;

                // Disable hit-testing on CableLayer during dragging so that existing cables do not interfere with target selection
                CableLayer.IsHitTestVisible = false;

                this.CaptureMouse();
                tempCable = new Path { Stroke = Brushes.Yellow, StrokeThickness = 4, IsHitTestVisible = false };
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

            var pos = e.GetPosition(CableLayer);
            var start = selectedPortVisual.TranslatePoint(new Point(selectedPortVisual.ActualWidth / 2, selectedPortVisual.ActualHeight / 2), CableLayer);
            var end = pos;

            tempCable.Data = CreateBezier(start, end, false);
        }

        // ─────────────────────────────────────────────
        //  CLIC DROIT : mémoriser la position sur le canvas
        // ─────────────────────────────────────────────
        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rightClickPosition = e.GetPosition(ModuleCanvas);
        }

        // ─────────────────────────────────────────────
        //  MENU CONTEXTUEL : créer un module
        // ─────────────────────────────────────────────
        private void AddModuleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string moduleType)
            {
                double snappedX = Math.Round(_rightClickPosition.X / GridSize) * GridSize;
                double snappedY = Math.Round(_rightClickPosition.Y / GridSize) * GridSize;
                _viewModel.AddModule(moduleType, snappedX, snappedY);
            }
        }

        // ─────────────────────────────────────────────
        //  DRAG — MouseDown : détecter le DragHandle
        // ─────────────────────────────────────────────
        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsClickFromDragHandle(e.OriginalSource as DependencyObject))
                return;

            var vm = FindModuleViewModel(e.OriginalSource as DependencyObject);
            if (vm == null) return;

            _draggedModule = vm;
            _dragStartMouse = e.GetPosition(ModuleCanvas);
            _dragStartModulePos = new Point(vm.GridX, vm.GridY);
            _isDragging = true;

            ModuleCanvas.CaptureMouse();
            e.Handled = true;
        }

        // ─────────────────────────────────────────────
        //  DRAG — MouseMove : déplacer librement
        // ─────────────────────────────────────────────
        private void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedModule == null) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                FinishDrag();
                return;
            }

            var current = e.GetPosition(ModuleCanvas);
            double dx = current.X - _dragStartMouse.X;
            double dy = current.Y - _dragStartMouse.Y;

            // Déplacement fluide pendant le drag (pas de snap)
            _draggedModule.GridX = Math.Max(0, _dragStartModulePos.X + dx);
            _draggedModule.GridY = Math.Max(0, _dragStartModulePos.Y + dy);
            UpdateCablesPosition();
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

            // Perform hit-test relative to ModuleCanvas for accurate visual intersection
            var hitResult = VisualTreeHelper.HitTest(ModuleCanvas, e.GetPosition(ModuleCanvas));

            // Re-enable hit-testing on the CableLayer immediately
            CableLayer.IsHitTestVisible = true;

            FrameworkElement? targetVisual = null;
            PortViewModelBase? targetPortVM = null;

            if (hitResult != null)
            {
                var current = hitResult.VisualHit;
                while (current != null)
                {
                    if (current is FrameworkElement fe && fe.DataContext is PortViewModelBase pVM)
                    {
                        targetVisual = fe;
                        targetPortVM = pVM;
                        break;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            if (targetPortVM != null && targetPortVM != selectedPortVM && selectedPortVisual != null)
            {
                IPort? sourceModel = GetInternalPort(selectedPortVM);
                IPort? destModel = GetInternalPort(targetPortVM);

                if (sourceModel != null && destModel != null)
                {
                    IPort? outPortModel = null;
                    IPort? inPortModel = null;
                    FrameworkElement? outVisual = null;
                    FrameworkElement? inVisual = null;

                    if (sourceModel.Type == PortType.Output && destModel.Type == PortType.Input)
                    {
                        outPortModel = sourceModel; outVisual = selectedPortVisual;
                        inPortModel = destModel; inVisual = FindPortEllipse(targetVisual);
                    }
                    else if (sourceModel.Type == PortType.Input && destModel.Type == PortType.Output)
                    {
                        outPortModel = destModel; outVisual = FindPortEllipse(targetVisual);
                        inPortModel = sourceModel; inVisual = selectedPortVisual;
                    }

                    if (outPortModel != null && inPortModel != null && outVisual != null && inVisual != null)
                    {
                        // Check if this connection already exists to prevent duplicate cabling
                        bool exists = allCables.Any(c => c.Source == outPortModel && c.Destination == inPortModel);
                        if (!exists)
                        {
                            string colorStr = CableColors[_randomColorGen.Next(CableColors.Length)];
                            var color = (Color)ColorConverter.ConvertFromString(colorStr);
                            var brush = new SolidColorBrush(color);

                            var cableContainer = new Canvas
                            {
                                IsHitTestVisible = true
                            };

                            // 1. Shadow path
                            var shadowPath = new Path
                            {
                                Name = "CableShadow",
                                Stroke = new SolidColorBrush(Color.FromArgb(90, 10, 10, 15)),
                                StrokeThickness = 6,
                                Fill = Brushes.Transparent,
                                IsHitTestVisible = false,
                                RenderTransform = new TranslateTransform(0, 12)
                            };

                            // Loops Shadow path
                            var loopsShadowPath = new Path
                            {
                                Name = "LoopsShadow",
                                Stroke = new SolidColorBrush(Color.FromArgb(90, 10, 10, 15)),
                                StrokeThickness = 6,
                                Fill = Brushes.Transparent,
                                IsHitTestVisible = false,
                                RenderTransform = new TranslateTransform(0, 12)
                            };

                            // 2. Dark outline border path
                            var borderPath = new Path
                            {
                                Name = "CableBorder",
                                Stroke = new SolidColorBrush(GetDarkerColor(color)),
                                StrokeThickness = 8,
                                Fill = Brushes.Transparent,
                                IsHitTestVisible = false
                            };

                            // Loops Dark outline border path
                            var loopsBorderPath = new Path
                            {
                                Name = "LoopsBorder",
                                Stroke = new SolidColorBrush(GetDarkerColor(color)),
                                StrokeThickness = 8,
                                Fill = Brushes.Transparent,
                                IsHitTestVisible = false
                            };

                            // 3. Main colored path
                            var mainPath = new Path
                            {
                                Name = "CableMain",
                                Stroke = brush,
                                StrokeThickness = 5,
                                Fill = Brushes.Transparent,
                                Cursor = Cursors.Hand
                            };

                            // Loops Main colored path
                            var loopsMainPath = new Path
                            {
                                Name = "LoopsMain",
                                Stroke = brush,
                                StrokeThickness = 5,
                                Fill = Brushes.Transparent,
                                IsHitTestVisible = false
                            };

                            // 4. White/Light highlight path
                            var highlightPath = new Path
                            {
                                Name = "CableHighlight",
                                Stroke = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                                StrokeThickness = 1.5,
                                Fill = Brushes.Transparent,
                                IsHitTestVisible = false,
                                RenderTransform = new TranslateTransform(0, -1)
                            };

                            // Loops White/Light highlight path
                            var loopsHighlightPath = new Path
                            {
                                Name = "LoopsHighlight",
                                Stroke = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                                StrokeThickness = 1.5,
                                Fill = Brushes.Transparent,
                                IsHitTestVisible = false,
                                RenderTransform = new TranslateTransform(0, -1)
                            };

                            var start = outVisual.TranslatePoint(new Point(outVisual.ActualWidth / 2, outVisual.ActualHeight / 2), CableLayer);
                            var end = inVisual.TranslatePoint(new Point(inVisual.ActualWidth / 2, inVisual.ActualHeight / 2), CableLayer);

                            var curveGeometry = CreateBezierCurveOnly(start, end, true);
                            var loopsGeometry = CreateLoopsOnly(start, end, true);

                            shadowPath.Data = curveGeometry;
                            loopsShadowPath.Data = loopsGeometry;
                            borderPath.Data = curveGeometry;
                            loopsBorderPath.Data = loopsGeometry;
                            mainPath.Data = curveGeometry;
                            loopsMainPath.Data = loopsGeometry;
                            highlightPath.Data = curveGeometry;
                            loopsHighlightPath.Data = loopsGeometry;

                            // Create plugs
                            var startPlug = CreatePlugVisual(color);
                            var endPlug = CreatePlugVisual(color);

                            Canvas.SetLeft(startPlug, start.X - 12);
                            Canvas.SetTop(startPlug, start.Y - 12);
                            Canvas.SetLeft(endPlug, end.X - 12);
                            Canvas.SetTop(endPlug, end.Y - 12);

                            // Add to container in correct Z-order
                            cableContainer.Children.Add(shadowPath);
                            cableContainer.Children.Add(loopsShadowPath);
                            cableContainer.Children.Add(borderPath);
                            cableContainer.Children.Add(loopsBorderPath);
                            cableContainer.Children.Add(mainPath);
                            cableContainer.Children.Add(loopsMainPath);
                            cableContainer.Children.Add(highlightPath);
                            cableContainer.Children.Add(loopsHighlightPath);
                            cableContainer.Children.Add(startPlug);
                            cableContainer.Children.Add(endPlug);

                            Panel.SetZIndex(cableContainer, 900);

                            Cable newCable = new Cable(outPortModel, inPortModel);
                            newCable.Visual = cableContainer;
                            
                            var connectionTag = Tuple.Create(outVisual, inVisual);
                            cableContainer.Tag = connectionTag;

                            allCables.Add(newCable);
                            CableLayer.Children.Add(cableContainer);
                            
                             newCable.AddCable(_engine);
 
                             mainPath.ToolTip = "Clic pour supprimer le câble";
                             mainPath.MouseLeftButtonDown += (s, args) =>
                             {
                                 RemoveCable(newCable, cableContainer);
                                 args.Handled = true;
                             };
                        }
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
        private PathGeometry CreateBezier(Point start, Point end, bool endIsPort = true)
        {
            double r = 12.0; // Radius of the loop around the port

            // Start loop points
            Point startLoopBottom = new Point(start.X, start.Y + r);
            Point startLoopTop = new Point(start.X, start.Y - r);

            // Determine target point for the gravity curve
            Point targetEndPoint = endIsPort ? new Point(end.X, end.Y + r) : end;

            // Gravity Bezier curve between the start loop bottom and target end point
            double dx = targetEndPoint.X - startLoopBottom.X;
            double dy = targetEndPoint.Y - startLoopBottom.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Gravity effect (sagging downwards)
            double horizontalFactor = Math.Clamp(Math.Abs(dx) / (distance + 0.001), 0.0, 1.0);
            double baseSag = 40.0 + (distance * 0.35) * horizontalFactor;

            double hOffset = dx * 0.25;

            Point control1 = new Point(startLoopBottom.X + hOffset, startLoopBottom.Y + baseSag);
            Point control2 = endIsPort 
                ? new Point(targetEndPoint.X - hOffset, targetEndPoint.Y + baseSag)
                : new Point(targetEndPoint.X, targetEndPoint.Y + baseSag * 0.5);

            var figure = new PathFigure
            {
                StartPoint = startLoopBottom,
                IsClosed = false
            };

            // 1. Loop around the start port (clockwise complete circle)
            figure.Segments.Add(new ArcSegment(startLoopTop, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new ArcSegment(startLoopBottom, new Size(r, r), 0, false, SweepDirection.Clockwise, true));

            // 2. The main gravity Bezier curve
            figure.Segments.Add(new BezierSegment(control1, control2, targetEndPoint, true));

            if (endIsPort)
            {
                // 3. Loop around the end port (clockwise complete circle)
                Point endLoopTop = new Point(end.X, end.Y - r);
                figure.Segments.Add(new ArcSegment(endLoopTop, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
                figure.Segments.Add(new ArcSegment(targetEndPoint, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            }

            return new PathGeometry(new[] { figure });
        }

        private PathGeometry CreateBezierCurveOnly(Point start, Point end, bool endIsPort = true)
        {
            double r = 12.0; // Radius of the loop around the port
            Point startLoopBottom = new Point(start.X, start.Y + r);
            Point targetEndPoint = endIsPort ? new Point(end.X, end.Y + r) : end;

            double dx = targetEndPoint.X - startLoopBottom.X;
            double dy = targetEndPoint.Y - startLoopBottom.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Gravity effect (sagging downwards)
            double horizontalFactor = Math.Clamp(Math.Abs(dx) / (distance + 0.001), 0.0, 1.0);
            double baseSag = 40.0 + (distance * 0.35) * horizontalFactor;

            double hOffset = dx * 0.25;

            Point control1 = new Point(startLoopBottom.X + hOffset, startLoopBottom.Y + baseSag);
            Point control2 = endIsPort 
                ? new Point(targetEndPoint.X - hOffset, targetEndPoint.Y + baseSag)
                : new Point(targetEndPoint.X, targetEndPoint.Y + baseSag * 0.5);

            var figure = new PathFigure
            {
                StartPoint = startLoopBottom,
                IsClosed = false
            };
            figure.Segments.Add(new BezierSegment(control1, control2, targetEndPoint, true));

            return new PathGeometry(new[] { figure });
        }

        private PathGeometry CreateLoopsOnly(Point start, Point end, bool endIsPort = true)
        {
            double r = 12.0; // Radius of the loop around the port
            Point startLoopBottom = new Point(start.X, start.Y + r);
            Point startLoopTop = new Point(start.X, start.Y - r);

            var figures = new List<PathFigure>();

            // 1. Loop around the start port (clockwise complete circle)
            var startFigure = new PathFigure
            {
                StartPoint = startLoopBottom,
                IsClosed = false
            };
            startFigure.Segments.Add(new ArcSegment(startLoopTop, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            startFigure.Segments.Add(new ArcSegment(startLoopBottom, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            figures.Add(startFigure);

            if (endIsPort)
            {
                // 2. Loop around the end port (clockwise complete circle)
                Point targetEndPoint = new Point(end.X, end.Y + r);
                Point endLoopTop = new Point(end.X, end.Y - r);

                var endFigure = new PathFigure
                {
                    StartPoint = targetEndPoint,
                    IsClosed = false
                };
                endFigure.Segments.Add(new ArcSegment(endLoopTop, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
                endFigure.Segments.Add(new ArcSegment(targetEndPoint, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
                figures.Add(endFigure);
            }

            return new PathGeometry(figures);
        }
        //==============================
        //DELETE CABLE
        //==============================
        private void RemoveCable(Cable cable, FrameworkElement visualContainer)
        {
            if (cable == null || visualContainer == null) return;
            
            cable.Destination.Value = 0;
            
            // Remove the container from the CableLayer
            CableLayer.Children.Remove(visualContainer);

            allCables.Remove(cable);
            cable.RemoveCable(_engine); // Unregister from audio engine
        }
        //==============================
        //EXTRACT FROM VIEWMODEL TO INTERNAL
        //==============================
        private IPort? GetInternalPort(PortViewModelBase? vm)
        {
            if (vm == null) return null;

            var portProp = vm.GetType().GetProperty("Port");
            if (portProp != null) return portProp.GetValue(vm) as IPort;

            var modelProp = vm.GetType().GetProperty("Model");
            if (modelProp != null) return modelProp.GetValue(vm) as IPort;

            return null;
        }


        // ─────────────────────────────────────────────
        //  DRAG — MouseUp : snap à la grille
        // ─────────────────────────────────────────────
        private void Canvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging || _draggedModule == null) return;

            // Snap à la grille au relâchement
            _draggedModule.GridX = Math.Round(_draggedModule.GridX / GridSize) * GridSize;
            _draggedModule.GridY = Math.Round(_draggedModule.GridY / GridSize) * GridSize;

            UpdateCablesPosition();
            FinishDrag();
        }

        private void FinishDrag()
        {
            _isDragging = false;
            _draggedModule = null;
            ModuleCanvas.ReleaseMouseCapture();
        }

        // ─────────────────────────────────────────────
        //  HELPERS — visual tree
        // ─────────────────────────────────────────────

        /// <summary>
        /// Remonte l'arbre visuel depuis la source et vérifie qu'on a cliqué sur un DragHandle
        /// sans passer par un contrôle interactif (Button, Slider, ComboBox…).
        /// </summary>
        private static bool IsClickFromDragHandle(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                // Contrôle interactif → pas de drag
                if (current is Button or Slider or ComboBox or ComboBoxItem
                             or CheckBox or ScrollBar or TextBox)
                    return false;

                // Header trouvé → drag OK
                if (current is FrameworkElement { Tag: "DragHandle" })
                    return true;

                // Limite : on ne dépasse pas le ContentPresenter (frontière du module)
                if (current is ContentPresenter)
                    break;

                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        /// <summary>
        /// Remonte l'arbre visuel pour trouver le ModuleViewModuleBase dans le DataContext.
        /// </summary>
        private static ModuleViewModuleBase? FindModuleViewModel(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (current is FrameworkElement { DataContext: ModuleViewModuleBase vm })
                    return vm;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        public void UpdateCablesPosition()
        {
            if (CableLayer == null) return;
            foreach (var child in CableLayer.Children)
            {
                if (child is Canvas cableContainer && cableContainer.Tag is Tuple<FrameworkElement, FrameworkElement> ports)
                {
                    var outVisual = ports.Item1;
                    var inVisual = ports.Item2;

                    if (outVisual == null || inVisual == null) continue;

                    // Recalculate center positions relative to CableLayer
                    var start = outVisual.TranslatePoint(new Point(outVisual.ActualWidth / 2, outVisual.ActualHeight / 2), CableLayer);
                    var end = inVisual.TranslatePoint(new Point(inVisual.ActualWidth / 2, inVisual.ActualHeight / 2), CableLayer);

                    // Recreate Bezier and loops geometry
                    var curveGeometry = CreateBezierCurveOnly(start, end, true);
                    var loopsGeometry = CreateLoopsOnly(start, end, true);
                    
                    // Apply geometry based on path Name
                    foreach (var subChild in cableContainer.Children)
                    {
                        if (subChild is Path path)
                        {
                            if (path.Name != null && path.Name.StartsWith("Loops"))
                            {
                                path.Data = loopsGeometry;
                            }
                            else
                            {
                                path.Data = curveGeometry;
                            }
                        }
                    }
                    
                    // Update plug positions (they are the last two children in the container)
                    if (cableContainer.Children.Count >= 2)
                    {
                        var startPlug = cableContainer.Children[cableContainer.Children.Count - 2] as FrameworkElement;
                        var endPlug = cableContainer.Children[cableContainer.Children.Count - 1] as FrameworkElement;
                        if (startPlug != null)
                        {
                            Canvas.SetLeft(startPlug, start.X - 12);
                            Canvas.SetTop(startPlug, start.Y - 12);
                        }
                        if (endPlug != null)
                        {
                            Canvas.SetLeft(endPlug, end.X - 12);
                            Canvas.SetTop(endPlug, end.Y - 12);
                        }
                    }
                }
            }
        }
        // ==============================
        // COLLECTION CHANGED : CLEAN UP CABLES
        // ==============================
        private void OnModulesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    if (oldItem is ModuleViewModuleBase moduleVM)
                    {
                        // Collect all port models of this module VM
                        var portsToDisconnect = new HashSet<IPort>();
                        foreach (var portVM in moduleVM.InputPorts)
                        {
                            if (portVM.Model != null)
                                portsToDisconnect.Add(portVM.Model);
                        }
                        foreach (var portVM in moduleVM.OutputPorts)
                        {
                            if (portVM.Model != null)
                                portsToDisconnect.Add(portVM.Model);
                        }

                        // Find all cables connected to any of these ports
                        var cablesToRemove = allCables
                            .Where(c => portsToDisconnect.Contains(c.Source) || portsToDisconnect.Contains(c.Destination))
                            .ToList();

                        foreach (var cable in cablesToRemove)
                        {
                            if (cable.Visual != null)
                            {
                                CableLayer.Children.Remove(cable.Visual);
                            }
                            else
                            {
                                // Fallback by looking up Tag in CableLayer Children
                                var containersToRemove = CableLayer.Children.OfType<Canvas>().Where(container => 
                                {
                                    if (container.Tag is Tuple<FrameworkElement, FrameworkElement> tuple)
                                    {
                                        var outPortVM = tuple.Item1.DataContext as PortViewModelBase;
                                        var inPortVM = tuple.Item2.DataContext as PortViewModelBase;
                                        return (outPortVM != null && portsToDisconnect.Contains(outPortVM.Model)) ||
                                               (inPortVM != null && portsToDisconnect.Contains(inPortVM.Model));
                                    }
                                    return false;
                                }).ToList();

                                foreach (var cToRemove in containersToRemove)
                                {
                                    CableLayer.Children.Remove(cToRemove);
                                }
                            }

                            allCables.Remove(cable);
                            cable.RemoveCable(_engine);
                        }
                    }
                }
            }
        }

        // --- Virtual Piano Logic & Handlers ---
        private bool _isPianoOpen = false;
        private int _currentOctave = 4;
        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();

        private readonly Dictionary<Key, int> _keyboardNoteMap = new Dictionary<Key, int>
        {
            { Key.A, 0 },  // C
            { Key.W, 1 },  // C#
            { Key.S, 2 },  // D
            { Key.E, 3 },  // D#
            { Key.D, 4 },  // E
            { Key.F, 5 },  // F
            { Key.T, 6 },  // F#
            { Key.G, 7 },  // G
            { Key.Y, 8 },  // G#
            { Key.H, 9 },  // A
            { Key.U, 10 }, // A#
            { Key.J, 11 }, // B
            { Key.K, 12 }  // C (Octave + 1)
        };

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                TogglePiano();
                e.Handled = true;
                return;
            }

            if (_isPianoOpen && _keyboardNoteMap.TryGetValue(e.Key, out int noteOffset))
            {
                if (!_pressedKeys.Contains(e.Key))
                {
                    _pressedKeys.Add(e.Key);
                    int midiNote = (_currentOctave + 1) * 12 + noteOffset;
                    _viewModel.SendMidiNote(midiNote);
                    HighlightPianoKey(noteOffset, true);
                }
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (_isPianoOpen && _keyboardNoteMap.TryGetValue(e.Key, out int noteOffset))
            {
                if (_pressedKeys.Contains(e.Key))
                {
                    _pressedKeys.Remove(e.Key);
                    HighlightPianoKey(noteOffset, false);
                    
                    if (_pressedKeys.Count == 0)
                    {
                        _viewModel.SendMidiRelease();
                    }
                }
                e.Handled = true;
            }
            base.OnKeyUp(e);
        }

        private void TogglePiano()
        {
            var fadeOut = (System.Windows.Media.Animation.Storyboard)this.Resources["PianoFadeOut"];
            var fadeIn = (System.Windows.Media.Animation.Storyboard)this.Resources["PianoFadeIn"];

            if (_isPianoOpen)
            {
                fadeOut.Completed += (s, e) => { PianoOverlay.Visibility = Visibility.Collapsed; };
                fadeOut.Begin();
                _isPianoOpen = false;
                _pressedKeys.Clear();
                _viewModel.SendMidiRelease();
            }
            else
            {
                PianoOverlay.Visibility = Visibility.Visible;
                fadeIn.Begin();
                _isPianoOpen = true;
            }
        }

        private void HighlightPianoKey(int noteOffset, bool highlight)
        {
            var button = FindPianoKeyButton(noteOffset);
            if (button != null)
            {
                if (highlight)
                {
                    bool isBlackKey = noteOffset == 1 || noteOffset == 3 || noteOffset == 6 || noteOffset == 8 || noteOffset == 10;
                    button.Background = isBlackKey
                        ? new SolidColorBrush(Color.FromRgb(255, 179, 71)) // Amber/orange for black keys
                        : new SolidColorBrush(Color.FromRgb(255, 208, 128)); // Soft warm amber for white keys
                }
                else
                {
                    button.ClearValue(Button.BackgroundProperty);
                }
            }
        }

        private Button? FindPianoKeyButton(int noteOffset)
        {
            foreach (var child in WhiteKeysGrid.Children)
            {
                if (child is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int val) && val == noteOffset)
                {
                    return btn;
                }
            }
            foreach (var child in BlackKeysGrid.Children)
            {
                if (child is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int val) && val == noteOffset)
                {
                    return btn;
                }
            }
            return null;
        }

        private void OctaveDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOctave > 0)
            {
                _currentOctave--;
                OctaveTxt.Text = _currentOctave.ToString();
            }
        }

        private void OctaveUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOctave < 8)
            {
                _currentOctave++;
                OctaveTxt.Text = _currentOctave.ToString();
            }
        }

        private void PianoKey_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int noteOffset))
            {
                btn.CaptureMouse();
                int midiNote = (_currentOctave + 1) * 12 + noteOffset;
                _viewModel.SendMidiNote(midiNote);
                e.Handled = true;
            }
        }

        private void PianoKey_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn.IsMouseCaptured)
                {
                    btn.ReleaseMouseCapture();
                }
                _viewModel.SendMidiRelease();
                e.Handled = true;
            }
        }

        private void PianoKey_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn && btn.IsMouseCaptured)
            {
                btn.ReleaseMouseCapture();
                _viewModel.SendMidiRelease();
            }
        }

        // --- Module Exit/Removal Animation ---
        private void OnModuleRemoving(ModuleViewModuleBase vm)
        {
            var container = ModulesControl.ItemContainerGenerator.ContainerFromItem(vm) as FrameworkElement;
            if (container != null)
            {
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.25)
                };
                
                var scaleOutX = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0.8,
                    Duration = TimeSpan.FromSeconds(0.25),
                    EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn, Amplitude = 0.5 }
                };

                var scaleOutY = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0.8,
                    Duration = TimeSpan.FromSeconds(0.25),
                    EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn, Amplitude = 0.5 }
                };

                var scale = container.RenderTransform as ScaleTransform;
                if (scale != null)
                {
                    fadeOut.Completed += (s, e) =>
                    {
                        _viewModel.Modules.Remove(vm);
                        _engine.RemoveModule(vm.Model);
                    };

                    container.BeginAnimation(FrameworkElement.OpacityProperty, fadeOut);
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOutX);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOutY);
                    return;
                }
            }

            // Fallback
            _viewModel.Modules.Remove(vm);
            _engine.RemoveModule(vm.Model);
        }

        // --- Startup Splash Animation ---
        private void TriggerSplashAnimation()
        {
            var sb = (System.Windows.Media.Animation.Storyboard)this.Resources["SplashAnimation"];
            sb.Completed += (s, e) =>
            {
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.55),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                fadeOut.Completed += (s2, e2) => { SplashOverlay.Visibility = Visibility.Collapsed; };
                SplashOverlay.BeginAnimation(FrameworkElement.OpacityProperty, fadeOut);
            };
            sb.Begin();
        }

        // --- Port visual lookup helpers ---
        private FrameworkElement? FindPortEllipse(FrameworkElement? visual)
        {
            if (visual == null) return null;
            if (visual is Ellipse) return visual;

            var current = visual;
            while (current != null)
            {
                if (current is StackPanel || current is Grid || current is Border || current is ContentPresenter)
                {
                    var ellipse = FindChild<Ellipse>(current);
                    if (ellipse != null) return ellipse;
                }
                current = VisualTreeHelper.GetParent(current) as FrameworkElement;
            }
            return visual;
        }

        private T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        // --- Premium VCV Rack-like Cable Helpers ---
        private FrameworkElement CreatePlugVisual(Color cableColor)
        {
            var grid = new Grid
            {
                Width = 24,
                Height = 24,
                IsHitTestVisible = false
            };

            // 1. Shadow for the plug
            var shadow = new Ellipse
            {
                Width = 24,
                Height = 24,
                Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                Margin = new Thickness(1, 2, 0, 0)
            };
            grid.Children.Add(shadow);

            // 2. Metallic outer ring (barrel)
            var metalRing = new Ellipse
            {
                Width = 22,
                Height = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var metalGrad = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            metalGrad.GradientStops.Add(new GradientStop(Color.FromRgb(220, 220, 220), 0.0));
            metalGrad.GradientStops.Add(new GradientStop(Color.FromRgb(160, 160, 160), 0.4));
            metalGrad.GradientStops.Add(new GradientStop(Color.FromRgb(100, 100, 100), 1.0));
            metalRing.Fill = metalGrad;
            grid.Children.Add(metalRing);

            // 3. Shiny colored plastic sleeve / body of the plug
            var plugBody = new Ellipse
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var bodyGrad = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            bodyGrad.GradientStops.Add(new GradientStop(Color.FromArgb(255, 
                (byte)Math.Min(255, cableColor.R + 60), 
                (byte)Math.Min(255, cableColor.G + 60), 
                (byte)Math.Min(255, cableColor.B + 60)), 0.0));
            bodyGrad.GradientStops.Add(new GradientStop(cableColor, 0.5));
            bodyGrad.GradientStops.Add(new GradientStop(GetDarkerColor(cableColor), 1.0));
            plugBody.Fill = bodyGrad;
            grid.Children.Add(plugBody);

            // 4. Center pin / rubber boot hole (dark core)
            var centerHole = new Ellipse
            {
                Width = 8,
                Height = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var holeGrad = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            holeGrad.GradientStops.Add(new GradientStop(Color.FromRgb(40, 40, 40), 0.0));
            holeGrad.GradientStops.Add(new GradientStop(Color.FromRgb(15, 15, 15), 1.0));
            centerHole.Fill = holeGrad;
            grid.Children.Add(centerHole);

            // 5. Tiny metal jack tip/reflection inside the center hole
            var tip = new Ellipse
            {
                Width = 3,
                Height = 3,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(-2, -2, 0, 0)
            };
            tip.Fill = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            grid.Children.Add(tip);

            return grid;
        }

        private Color GetDarkerColor(Color color)
        {
            return Color.FromArgb(color.A, (byte)(color.R * 0.4), (byte)(color.G * 0.4), (byte)(color.B * 0.4));
        }

        // --- Performance stats updater ---
        private int _frameCount = 0;
        private DateTime _lastFpsUpdate = DateTime.Now;

        private void UpdatePerformanceStats(object? sender, EventArgs e)
        {
            _frameCount++;
            var elapsed = DateTime.Now - _lastFpsUpdate;
            if (elapsed.TotalSeconds >= 0.5)
            {
                double fps = _frameCount / elapsed.TotalSeconds;
                FpsText.Text = $"{Math.Round(fps)} FPS";
                _frameCount = 0;
                _lastFpsUpdate = DateTime.Now;
            }

            if (_engine != null)
            {
                DspText.Text = $"{_engine.DspLoad:F1} %";
            }

            if (_viewModel != null)
            {
                ModulesText.Text = _viewModel.Modules.Count.ToString();
            }
            CablesText.Text = allCables.Count.ToString();
        }

        // --- Menu Bar Event Handlers ---
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClearPatchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var cablesCopy = allCables.ToList();
            foreach (var cable in cablesCopy)
            {
                if (cable.Visual != null)
                {
                    RemoveCable(cable, cable.Visual);
                }
            }
            allCables.Clear();

            var modulesCopy = _viewModel.Modules.ToList();
            foreach (var module in modulesCopy)
            {
                _viewModel.Modules.Remove(module);
                _engine.RemoveModule(module.Model);
            }
        }

        private void TogglePerfMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (PerformanceOverlay.Visibility == Visibility.Visible)
            {
                PerformanceOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                PerformanceOverlay.Visibility = Visibility.Visible;
            }
        }

        private void TogglePiano_Click(object sender, RoutedEventArgs e)
        {
            TogglePiano();
        }

        private void ToggleEngine_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.IsPlaying)
            {
                _engine.Stop();
                EngineStatusMenuItem.Header = "Start Engine";
            }
            else
            {
                _engine.Start();
                EngineStatusMenuItem.Header = "Stop Engine";
            }
        }
    }
}