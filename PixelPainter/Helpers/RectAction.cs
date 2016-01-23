using System.Windows.Media;
using PixelPainter.Model;

namespace PixelPainter.Helpers
{
    class RectAction
    {
        public RectItem Rect { get; set; }
        public SolidColorBrush OriginalColor { get; set; }
        public SolidColorBrush AdaptedColor { get; set; }
        public long ActionIndex { get; set; }

        public RectAction(RectItem rect, SolidColorBrush originalColor, SolidColorBrush adaptedColor, long actionIndex)
        {
            Rect = rect;
            OriginalColor = originalColor;
            AdaptedColor = adaptedColor;
            ActionIndex = actionIndex;
        }
    }
}
