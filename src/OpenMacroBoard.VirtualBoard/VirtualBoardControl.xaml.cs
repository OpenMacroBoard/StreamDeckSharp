using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// A control used to draw a virtual LCD macro board.
/// </summary>
public partial class VirtualBoardControl : Control
{
    private readonly BitmapImage glassImage;
    private readonly Brush backgroundColor;

    private VirtualBoardViewModel? model;

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

        const byte gray = 20;
        backgroundColor = new SolidColorBrush(Color.FromArgb(255, gray, gray, gray));
    }

    /// <summary>
    /// Renders the LCD macro board
    /// </summary>
    /// <param name="drawingContext"></param>
    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(backgroundColor, null, new Rect(0, 0, ActualWidth, ActualHeight));
        UpdateModelProperty();
        DrawBoard(drawingContext);
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (model == null)
        {
            return;
        }

        var isDown = e.LeftButton == MouseButtonState.Pressed;
        var position = e.GetPosition(this);
        var positionKeyId = GetKeyId(position);

        bool UpdateKeyState(int keyId, bool targetState)
        {
            var keyState = model!.GetKeyState(keyId);

            if (keyState == targetState)
            {
                return false;
            }

            model.SendKeyState(keyId, targetState);
            return true;
        }

        bool needVisualUpdate = false;

        for (var keyId = 0; keyId < model.Keys.Count; keyId++)
        {
            var targetState = positionKeyId == keyId && isDown;
            needVisualUpdate |= UpdateKeyState(keyId, targetState);
        }

        if (needVisualUpdate)
        {
            InvalidateVisual();
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (model == null)
        {
            return;
        }

        var pos = e.GetPosition(this);
        var p = GetKeyId(pos);

        if (p < 0)
        {
            return;
        }

        model.SendKeyState(p, true);
        InvalidateVisual();
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (model == null)
        {
            return;
        }

        var pos = e.GetPosition(this);
        var p = GetKeyId(pos);

        if (p < 0)
        {
            return;
        }

        model.SendKeyState(p, false);
        InvalidateVisual();
    }

    private void UpdateModelProperty()
    {
        var newModel = DataContext as VirtualBoardViewModel;

        if (ReferenceEquals(newModel, model))
        {
            return;
        }

        if (model is not null)
        {
            // Get rid of old model
            model.PropertyChanged -= Model_PropertyChanged;
            model = null;
        }

        if (newModel is not null)
        {
            newModel.PropertyChanged += Model_PropertyChanged;
            model = newModel;

            Width = newModel.Keys.Area.Width + newModel.Keys.GapSize * 2;
            Height = newModel.Keys.Area.Height + newModel.Keys.GapSize * 2;
        }
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VirtualBoardViewModel.KeyImages))
        {
            InvalidateVisual();
        }
    }

    private void DrawBoard(DrawingContext dc)
    {
        if (model == null)
        {
            return;
        }

        for (var keyId = 0; keyId < model.Keys.Count; keyId++)
        {
            var keyRect = model.Keys[keyId];
            var pressed = model.GetKeyState(keyId);

            double scaleFactor = 1.125;
            double glassScaleFactor = pressed ? 1.1 : scaleFactor;

            var imgTargetRect = new Rect(
                keyRect.X + model.Keys.GapSize,
                keyRect.Y + model.Keys.GapSize,
                keyRect.Width,
                keyRect.Height
            );

            var glassSize = new Size(
                imgTargetRect.Width * glassScaleFactor,
                imgTargetRect.Height * glassScaleFactor
            );

            var glassOffset = new Point(
                imgTargetRect.X - (glassSize.Width - imgTargetRect.Width) / 2,
                imgTargetRect.Y - (glassSize.Height - imgTargetRect.Height) / 2
            );

            var glassImageRect = new Rect(glassOffset, glassSize);

            // draw image
            var geo = new RectangleGeometry(imgTargetRect, 10, 10);
            dc.PushClip(geo);
            dc.DrawImage(model!.KeyImages[keyId], imgTargetRect);
            dc.Pop();

            // draw "glass" button overlay
            dc.DrawImage(glassImage, glassImageRect);
        }
    }

    private int GetKeyId(Point point)
    {
        var px = point.X - model!.Keys.GapSize;
        var py = point.Y - model!.Keys.GapSize;

        for (var i = 0; i < model!.Keys.Count; i++)
        {
            var r = model!.Keys[i];

            if (px < r.Left)
            {
                continue;
            }

            if (px > r.Right)
            {
                continue;
            }

            if (py < r.Top)
            {
                continue;
            }

            if (py > r.Bottom)
            {
                continue;
            }

            return i;
        }

        return -1;
    }
}
