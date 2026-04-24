using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Cursors = System.Windows.Input.Cursors;
using Cursor = System.Windows.Input.Cursor;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace VideoRecorderScreen.Views
{
    public partial class OverlayWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const double HandleSize = 10;
        private const double HandleHit = 14;

        private enum DragMode { None, Draw, Move, ResizeNW, ResizeN, ResizeNE, ResizeE, ResizeSE, ResizeS, ResizeSW, ResizeW }

        private readonly TaskCompletionSource<Rect?> _tcs;
        private DragMode _dragMode = DragMode.None;
        private DragMode _hoverMode = DragMode.None;
        private Point _dragStart;
        private Point _moveOffset;
        private Rect _selectionAtDragStart;
        private Rect _selection;
        private readonly Rectangle[] _handles = new Rectangle[8];

        private OverlayWindow(Rect initialRegion, TaskCompletionSource<Rect?> tcs)
        {
            InitializeComponent();
            _tcs = tcs;

            _selection = new Rect(
                initialRegion.X - SystemParameters.VirtualScreenLeft,
                initialRegion.Y - SystemParameters.VirtualScreenTop,
                initialRegion.Width,
                initialRegion.Height);

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            Loaded += OnLoaded;
            PreviewKeyDown += OnKeyDown;
        }

        public static Task<Rect?> ShowAsync(Rect initialRegion)
        {
            var tcs = new TaskCompletionSource<Rect?>();
            new OverlayWindow(initialRegion, tcs).Show();
            return tcs.Task;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

            CreateHandles();
            Activate();
            Keyboard.Focus(this);
            UpdateVisuals();
        }

        private void CreateHandles()
        {
            for (int i = 0; i < 8; i++)
            {
                _handles[i] = new Rectangle
                {
                    Width = HandleSize,
                    Height = HandleSize,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
                    StrokeThickness = 1.5,
                    IsHitTestVisible = false
                };
                Root.Children.Add(_handles[i]);
            }
        }

        private void Root_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right) { Cancel(); return; }
            var pos = e.GetPosition(Root);
            _dragStart = pos;
            _selectionAtDragStart = _selection;
            _dragMode = HitTest(pos);
            if (_dragMode == DragMode.Move)
                _moveOffset = new Point(pos.X - _selection.X, pos.Y - _selection.Y);
            Root.CaptureMouse();
        }

        private void Root_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(Root);
            if (_dragMode == DragMode.None)
            {
                _hoverMode = HitTest(pos);
                Root.Cursor = HitTestCursor(_hoverMode);
                UpdateHintText();
                return;
            }
            if (_dragMode == DragMode.Draw)
                _selection = Normalize(_dragStart, pos);
            else if (_dragMode == DragMode.Move)
                _selection = new Rect(pos.X - _moveOffset.X, pos.Y - _moveOffset.Y, _selection.Width, _selection.Height);
            else
                _selection = ApplyResize(_dragMode, _selectionAtDragStart, _dragStart, pos);

            UpdateVisuals();
        }

        private void Root_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(Root);
            if (_dragMode == DragMode.Draw)
                _selection = Normalize(_dragStart, pos);
            _dragMode = DragMode.None;
            Root.ReleaseMouseCapture();
            UpdateVisuals();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Confirm();
            else if (e.Key == Key.Escape) Cancel();
        }

        private void Confirm()
        {
            if (_selection.Width < 4 || _selection.Height < 4) { Cancel(); return; }
            var result = new Rect(
                _selection.X + SystemParameters.VirtualScreenLeft,
                _selection.Y + SystemParameters.VirtualScreenTop,
                _selection.Width,
                _selection.Height);
            _tcs.TrySetResult(result);
            Close();
        }

        private void Cancel()
        {
            _tcs.TrySetResult(null);
            Close();
        }

        private void UpdateVisuals()
        {
            var outer = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
            var inner = new RectangleGeometry(_selection);
            DarkOverlay.Data = new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner);

            Canvas.SetLeft(SelectionBorder, _selection.Left);
            Canvas.SetTop(SelectionBorder, _selection.Top);
            SelectionBorder.Width = Math.Max(0, _selection.Width);
            SelectionBorder.Height = Math.Max(0, _selection.Height);

            SizeLabel.Text = $"{(int)_selection.Width} × {(int)_selection.Height}";
            SizeLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(SizeLabel, _selection.Left + 4);
            Canvas.SetTop(SizeLabel, _selection.Top + 4);

            PositionHandles();
            PositionHint();
            UpdateHintText();
        }

        private void PositionHandles()
        {
            var pts = HandlePoints(_selection);
            for (int i = 0; i < 8; i++)
            {
                Canvas.SetLeft(_handles[i], pts[i].X - HandleSize / 2);
                Canvas.SetTop(_handles[i], pts[i].Y - HandleSize / 2);
            }
        }

        private void PositionHint()
        {
            if (_selection.IsEmpty) return;
            HintLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var lh = HintLabel.DesiredSize.Height;
            var lw = HintLabel.DesiredSize.Width;
            const double margin = 8;

            double y;
            if (_selection.Top - lh - margin >= 0)
                y = _selection.Top - lh - margin;       // выше рамки
            else if (_selection.Bottom + lh + margin <= ActualHeight)
                y = _selection.Bottom + margin;          // ниже рамки
            else
                y = _selection.Top + margin;             // поверх верхнего края

            double x = _selection.Left + (_selection.Width - lw) / 2;
            x = Math.Max(4, Math.Min(x, ActualWidth - lw - 4));

            Canvas.SetLeft(HintLabel, x);
            Canvas.SetTop(HintLabel, y);
        }

        private void UpdateHintText()
        {
            HintLabel.Text = _dragMode switch
            {
                DragMode.Draw                            => "Отпустите — зафиксировать  •  Esc — отмена",
                DragMode.Move                            => "Отпустите — готово  •  Esc — отмена",
                var m when m != DragMode.None            => "Отпустите — готово  •  Esc — отмена",
                _ => _hoverMode switch
                {
                    DragMode.Move                        => "ЛКМ — двигать  •  Enter — подтвердить  •  Esc — отмена",
                    var m when m != DragMode.None        => "ЛКМ — изменить размер  •  Enter — подтвердить  •  Esc — отмена",
                    _                                    => "Перетащите — выбрать область  •  Esc — отмена"
                }
            };
        }

        // Handle order: NW, N, NE, E, SE, S, SW, W
        private static Point[] HandlePoints(Rect r) =>
        [
            r.TopLeft,
            new(r.Left + r.Width / 2, r.Top),
            r.TopRight,
            new(r.Right, r.Top + r.Height / 2),
            r.BottomRight,
            new(r.Left + r.Width / 2, r.Bottom),
            r.BottomLeft,
            new(r.Left, r.Top + r.Height / 2),
        ];

        private DragMode HitTest(Point p)
        {
            if (_selection.IsEmpty) return DragMode.Draw;
            var pts = HandlePoints(_selection);
            DragMode[] modes = [DragMode.ResizeNW, DragMode.ResizeN, DragMode.ResizeNE,
                                 DragMode.ResizeE, DragMode.ResizeSE, DragMode.ResizeS,
                                 DragMode.ResizeSW, DragMode.ResizeW];
            for (int i = 0; i < 8; i++)
                if (Math.Abs(p.X - pts[i].X) <= HandleHit && Math.Abs(p.Y - pts[i].Y) <= HandleHit)
                    return modes[i];
            if (_selection.Contains(p)) return DragMode.Move;
            return DragMode.Draw;
        }

        private static Cursor HitTestCursor(DragMode mode) => mode switch
        {
            DragMode.ResizeNW or DragMode.ResizeSE => Cursors.SizeNWSE,
            DragMode.ResizeNE or DragMode.ResizeSW => Cursors.SizeNESW,
            DragMode.ResizeN  or DragMode.ResizeS  => Cursors.SizeNS,
            DragMode.ResizeW  or DragMode.ResizeE  => Cursors.SizeWE,
            DragMode.Move => Cursors.SizeAll,
            _ => Cursors.Cross
        };

        private static Rect ApplyResize(DragMode mode, Rect orig, Point start, Point cur)
        {
            double dx = cur.X - start.X;
            double dy = cur.Y - start.Y;
            double l = orig.Left, t = orig.Top, r = orig.Right, b = orig.Bottom;
            switch (mode)
            {
                case DragMode.ResizeNW: l += dx; t += dy; break;
                case DragMode.ResizeN:             t += dy; break;
                case DragMode.ResizeNE: r += dx; t += dy; break;
                case DragMode.ResizeE:  r += dx;           break;
                case DragMode.ResizeSE: r += dx; b += dy;  break;
                case DragMode.ResizeS:             b += dy; break;
                case DragMode.ResizeSW: l += dx; b += dy;  break;
                case DragMode.ResizeW:  l += dx;           break;
            }
            return Normalize(new Point(l, t), new Point(r, b));
        }

        private static Rect Normalize(Point a, Point b) =>
            new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
                Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
