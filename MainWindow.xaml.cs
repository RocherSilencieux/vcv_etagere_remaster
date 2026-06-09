using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using vcv_etagere_remaster.Front.ViewModel;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel = null!;
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
            this.DataContext = _viewModel;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object? sender, EventArgs e) => _viewModel.Shutdown();

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
    }
}