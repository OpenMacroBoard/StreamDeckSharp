using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenMacroBoard.VirtualBoard
{
    /// <summary>
    /// A control used to draw a virtual LCD marco board
    /// </summary>
    public partial class VirtualBoardControl : Control
    {
        private VirtualBoardViewModel model;
        private VirtualBoardLayout layout;
        private readonly BitmapImage glassImage;
        private readonly Brush backgroundColor;

        /// <summary>
        /// Create a <see cref="VirtualBoardControl"/> instance.
        /// </summary>
        public VirtualBoardControl()
        {
            InitializeComponent();
            var asm = Assembly.GetExecutingAssembly();

            using (var resStream = asm.GetManifestResourceStream("OpenMacroBoard.VirtualBoard.glassKey.png"))
            {
                glassImage = new BitmapImage();
                glassImage.BeginInit();
                glassImage.CacheOption = BitmapCacheOption.OnLoad;
                glassImage.StreamSource = resStream;
                glassImage.EndInit();
            }

            byte gray = 20;
            backgroundColor = new SolidColorBrush(Color.FromArgb(255, gray, gray, gray));
        }

        /// <summary>
        /// Renders the LCD macro board
        /// </summary>
        /// <param name="dc"></param>
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(backgroundColor, null, new Rect(0, 0, ActualWidth, ActualHeight));

            UpdateModelProperty();

            if (model == null)
                return;

            UpdateLayoutInfo();
            DrawBoard(dc);
        }

        private void UpdateLayoutInfo()
        {
            layout = new VirtualBoardLayout(model.Keys, ActualWidth, ActualHeight);
        }

        private void UpdateModelProperty()
        {
            var newModel = DataContext as VirtualBoardViewModel;
            if (ReferenceEquals(newModel, model))
                return;

            if (!(model is null))
                model.PropertyChanged -= Model_PropertyChanged;

            if (!(newModel is null))
                newModel.PropertyChanged += Model_PropertyChanged;

            model = newModel;
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VirtualBoardViewModel.KeyImages))
            {
                InvalidateVisual();
            }
        }

        private void DrawBoard(DrawingContext dc)
        {
            dc.PushTransform(new ScaleTransform(0.9, 0.9, ActualWidth / 2, ActualHeight / 2));

            int cnt = 0;
            foreach (var ki in layout.KeyPositions)
            {
                var transform = new TransformGroup();
                transform.Children.Add(new ScaleTransform(ki.Width, ki.Height, 0, 0));
                transform.Children.Add(new TranslateTransform(ki.Left, ki.Top));

                dc.PushTransform(transform);

                DrawButton(dc, cnt);
                cnt++;

                dc.Pop();
            }

            dc.Pop();
        }

        private void DrawButton(DrawingContext dc, int keyId)
        {
            dc.DrawImage(model.KeyImages[keyId], new Rect(0, 0, 1, 1));
            const double offset = 0.071;
            dc.DrawImage(glassImage, new Rect(-offset, -offset, 1 + 2 * offset, 1 + 2 * offset));
        }

        private int GetKeyId(Point point)
        {
            for (int i = 0; i < layout.KeyPositions.Count; i++)
            {
                var r = layout.KeyPositions[i];

                if (point.X < r.Left) continue;
                if (point.X > r.Right) continue;
                if (point.Y < r.Top) continue;
                if (point.Y > r.Bottom) continue;

                return i;
            }

            return -1;
        }

        /// <summary>
        /// Processes mouse down events
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            var p = GetKeyId(pos);

            if (p >= 0)
                model.SendKeyState(p, true);
        }

        /// <summary>
        /// Processes mouse up events
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            var p = GetKeyId(pos);

            if (p >= 0)
                model.SendKeyState(p, false);
        }
    }
}
