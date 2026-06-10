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

            // Current mouse position relative to the CableLayer
            var pos = e.GetPosition(CableLayer);

            // Compute start point as center of the selected port visual translated to CableLayer coords
            var start = selectedPortVisual.TranslatePoint(new Point(selectedPortVisual.ActualWidth / 2, selectedPortVisual.ActualHeight / 2), CableLayer);
            var end = pos;

            tempCable.Data = CreateBezier(start, end);
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
            // Ne démarrer le drag que si le clic vient du header (Tag="DragHandle")
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
                        path.Tag = Tuple.Create(outVisual, inVisual);
                        allCables.Add(newCable);
                        CableLayer.Children.Add(path);
                        newCable.AddCable(_engine); // Register the cable with the audio engine so it will be processed
                        path.MouseRightButtonDown += (s, args) => { RemoveCable(newCable, path); }; //right click = remove cable
                        //allCables.Add(new UpdateCable
                        //{
                        //    LogicCable = newCable,
                        //    VisualPath = path,
                        //    OutputVisual = outVisual,
                        //    InputVisual = inVisual
                        //});

                    }
                }
            }
            isDraggingCable = false;
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
            path.MouseRightButtonDown -= (s, args) => { RemoveCable(cable, path); };
            if (cable != null)
            {
                cable.Destination.Value = 0;
                CableLayer.Children.Remove(path);
                allCables.Remove(cable);
                cable.RemoveCable(_engine); // Unregister from audio engine
            }
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

            // On parcourt tous les enfants du Canvas qui sont des "Path" (les câbles)
            foreach (var child in CableLayer.Children)
            {
                if (child is Path cablePath && cablePath.Tag is Tuple<FrameworkElement, FrameworkElement> ports)
                {
                    var outVisual = ports.Item1;
                    var inVisual = ports.Item2;

                    if (outVisual == null || inVisual == null) continue;

                    // Recalcul des positions centrales par rapport au Canvas
                    var start = outVisual.TranslatePoint(new Point(outVisual.ActualWidth / 2, outVisual.ActualHeight / 2), CableLayer);
                    var end = inVisual.TranslatePoint(new Point(inVisual.ActualWidth / 2, inVisual.ActualHeight / 2), CableLayer);

                    // Mise à jour de la courbe
                    cablePath.Data = CreateBezier(start, end);
                }
            }
        }
    }
}