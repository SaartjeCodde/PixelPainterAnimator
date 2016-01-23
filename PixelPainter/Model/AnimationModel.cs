using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using PixelPainter.Helpers;
using System.Windows.Media;
using System.Drawing;
using System.Windows;
using RectConverter = PixelPainter.Helpers.RectConverter;

namespace PixelPainter.Model
{
    class AnimationModel : INotifyPropertyChanged
    {
        private List<ObservableCollection<RectItem>> AnimationFrames { get; set; }

        private int _currentFrameIndex;

        public int CurrentFrameIndex
        {
            get
            {
                return _currentFrameIndex;
            }
            set
            {
                if (value > AnimationFrames.Count - 1)
                {
                    _currentFrameIndex = 0;
                }
                else if (value < 0)
                {
                    _currentFrameIndex = AnimationFrames.Count - 1;
                }
                else
                {
                    _currentFrameIndex = value;
                }
                OnPropertyChanged("CurrentFrameIndex");
            }
        }

        public void SetCurrentFrameIndex(int sliderValue)
        {
            _currentFrameIndex = sliderValue;
        }
        public void SetCurrentFrame(ObservableCollection<RectItem> rects)
        {
            AnimationFrames.RemoveAt(CurrentFrameIndex);
            AnimationFrames.Insert(CurrentFrameIndex,rects);
        }

        public AnimationModel(ObservableCollection<RectItem> rectItems)
        {
            AnimationFrames = new List<ObservableCollection<RectItem>>();
            AnimationFrames.Add(rectItems);
        }

        public void AddAnimationFrame(int frameIndex, ObservableCollection<RectItem> rectItems)
        {
            if (frameIndex == AnimationFrames.Count)
            {
                AnimationFrames.Add(rectItems);
            }
            else
            {
                AnimationFrames.Insert(frameIndex, rectItems);
            }
            CurrentFrameIndex = frameIndex;
        }

        public void RemoveAnimationFrame()
        {
            if (AnimationFrames.Count > 1)
            {
                AnimationFrames.RemoveAt(CurrentFrameIndex);
                CurrentFrameIndex--;
            }
        }

        public ObservableCollection<RectItem> GetAnimationFrame(int frameIndex)
        {
            if (frameIndex < 0)
            {
                CurrentFrameIndex = 0;
            }
            else if (frameIndex > AnimationFrames.Count - 1)
            {
                CurrentFrameIndex = AnimationFrames.Count() - 1;
            }
            else
            {
                CurrentFrameIndex = frameIndex;
            }
            return AnimationFrames.ElementAt(CurrentFrameIndex);
        }

        public ObservableCollection<RectItem> GetCurrentAnimationFrame()
        {
            return AnimationFrames.ElementAt(CurrentFrameIndex);
        }

        public Boolean IsLastFrame()
        {
            return CurrentFrameIndex == AnimationFrames.Count - 1;
        }

        public int GetFrameCount()
        {
            return AnimationFrames.Count;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Save(int canvasHeight, int canvasWidth)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            var result = saveFile.ShowDialog();
            if (result == true)
            {
                List<object> dataObjects = new List<object>();
                var frameCount = AnimationFrames.Count;
                dataObjects.Add(canvasHeight);
                dataObjects.Add(canvasWidth);
                dataObjects.Add(CurrentFrameIndex);
                dataObjects.Add(frameCount);

                foreach (var rectList in AnimationFrames.ToArray())
                {
                    foreach (var rectItem in rectList.ToArray())
                    {
                        dataObjects.Add(rectItem.X);
                        dataObjects.Add(rectItem.Y);
                        dataObjects.Add(rectItem.PixelColor.Color.ToString());
                    }
                }
                Serialization.WriteToBinaryFile(saveFile.FileName + ".bin", dataObjects.ToArray());
            }
        }

        public int[] Load()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select animation file";
            openFile.Filter = "BIN File (.bin) | *.bin";
            var result = openFile.ShowDialog();
            int canvasHeight = 0;
            int canvasWidth = 0;

            if (result == true)
            {
                try
                {
                    object[] data = Serialization.ReadFromBinaryFile<object[]>(openFile.FileName);
                    canvasHeight = int.Parse(data[0].ToString());
                    canvasWidth = int.Parse(data[1].ToString());
                    int currentFrameIndex = int.Parse(data[2].ToString());
                    int frameCount = int.Parse(data[3].ToString());

                    AnimationFrames.Clear();

                    for (int i = 0; i < frameCount; i++)
                    {
                        ObservableCollection<RectItem> RectItems = new ObservableCollection<RectItem>();
                        int startIndex = canvasHeight * canvasWidth * 3;
                        for (int j = i * startIndex; j < startIndex * (i + 1); j += 3)
                        {
                            string colorInfo = data[j + 6].ToString();

                            RectItems.Add(new RectItem
                            {
                                X = int.Parse(data[j + 4].ToString()),
                                Y = int.Parse(data[j + 5].ToString()),
                                Height = 32,
                                Width = 32,
                                PixelColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(colorInfo))
                            });
                        }
                        AnimationFrames.Add(RectItems);
                    }
                }
                catch
                {
                    MessageBox.Show("Incorrect filetype!");
                }
            }
            return new[] {canvasWidth, canvasHeight};
        }

        public void SaveSpritesheet(int canvasWidth, int canvasHeight)
        {

            SaveFileDialog saveFile = new SaveFileDialog();
            var result = saveFile.ShowDialog();
            if (result == true)
            {
                List<Bitmap> sprites = new List<Bitmap>(AnimationFrames.Count);
                foreach (var frame in AnimationFrames)
                {
                    Bitmap sprite = RectConverter.RectToBitmap(frame.ToList(), canvasWidth, canvasHeight);
                    sprites.Add(sprite);
                }
                RectConverter.CombineBitmaps(sprites).Save(saveFile.FileName + ".png");
            }
        }

        public bool IsRightSize(int canvasWidth, int canvasHeight)
        {
            int rectCount = AnimationFrames.ElementAt(CurrentFrameIndex).Count;

            if (rectCount == canvasWidth*canvasHeight)
                return true;
            else return false;

        }
    }
}
