using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace PixelPainter.Model
{
    public class RectItem : INotifyPropertyChanged
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        private SolidColorBrush _pixelColor;
     
        public SolidColorBrush PixelColor { get { return _pixelColor;}
            set
            {
                _pixelColor = value;
                OnPropertyChanged("PixelColor");
            } 
        }

        public RectItem()
        {
            this.X = 32;
            this.Y = 32;
            this.Width = 32;
            this.Height = 32;
            this.PixelColor = Brushes.Black;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
