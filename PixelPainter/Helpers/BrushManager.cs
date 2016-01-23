using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace PixelPainter.Helpers
{
    static class BrushManager
    {
        private static List<SolidColorBrush> _brushes = new List<SolidColorBrush>();

        public static SolidColorBrush GetBrush(Color color)
        {
            if (!ContainsColor(color))
            {
                SolidColorBrush newBrush = new SolidColorBrush(color);
                _brushes.Add(newBrush);
                return newBrush;
            }
            else
            {
                return GetColor(color);
            }
        }

        private static Boolean ContainsColor(Color color)
        {
            foreach (var brush in _brushes)
            {
                if (brush.Color == color)
                    return true;
            }
            return false;
        }

        private static SolidColorBrush GetColor(Color color)
        {
            foreach (var brush in _brushes)
            {
                if (brush.Color == color)
                    return brush;
            }
            return null;
        }
    }
}
