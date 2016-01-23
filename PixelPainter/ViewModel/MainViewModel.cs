using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using PixelPainter.Helpers;
using Microsoft.Win32;
using PixelPainter.Model;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using RectConverter = PixelPainter.Helpers.RectConverter;

namespace PixelPainter.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ===== FIELDS =====
        private ObservableCollection<RectItem> _rectItems;

        private AnimationModel _animationFrames;

        private int _red;
        private int _green;
        private int _blue;
        private int _selectedColorIndex;
        private int _canvasHeight;
        private int _canvasWidth;
        private int _frameCounter;
        private int _frameCount;
        private int _actionCounter = 0;

        private Color _colorPickerColor;

        private bool _canDraw = false;
        private bool _isAnimating = false;

        private Task _task;

        private CancellationTokenSource _cts;
        
        private Stack<RectAction> _undoStack = new Stack<RectAction>();
        private Stack<RectAction> _redoStack = new Stack<RectAction>();

        private long _currentActionIndex = 0;

        private float _zoomLevel = 1f;

        // ===== PROPERTIES =====
        public ObservableCollection<RectItem> RectItems
        {
            get { return _rectItems; }
            set
            {
                _rectItems = value;
                OnPropertyChanged("RectItems");
            }
        }
        public ObservableCollection<RectItem> Swatches { get; set; }
        public int Red
        {
            get
            {
                return _red;
            }
            set
            {
                _red = value;
                ColorPickerColor = Color.FromArgb(255, System.Convert.ToByte(_red), System.Convert.ToByte(_green), System.Convert.ToByte(_blue));
                OnPropertyChanged("Red");
            }
        }
        public int Green
        {
            get { return _green; }
            set
            {
                _green = value;
                ColorPickerColor = Color.FromArgb(255, System.Convert.ToByte(_red), System.Convert.ToByte(_green), System.Convert.ToByte(_blue));
                OnPropertyChanged("Green");
            }
        }
        public int Blue
        {
            get { return _blue; }
            set
            {
                _blue = value;
                ColorPickerColor = Color.FromArgb(255, System.Convert.ToByte(_red), System.Convert.ToByte(_green), System.Convert.ToByte(_blue));
                OnPropertyChanged("Blue");
            }
        }
        public Color ColorPickerColor
        {
            get { return _colorPickerColor; }
            set
            {
                if (_colorPickerColor != value)
                {
                    _colorPickerColor = value;
                    OnPropertyChanged("ColorPickerColor");
                }
            }
        }
        public int SelectedColorIndex
        {
            get
            {
                return _selectedColorIndex;
            }
            set
            {
                _selectedColorIndex = value;
                OnPropertyChanged("SelectedColorIndex");
                if (_selectedColorIndex != -1)
                {
                    Red = Swatches.ElementAt(_selectedColorIndex).PixelColor.Color.R;
                    Green = Swatches.ElementAt(_selectedColorIndex).PixelColor.Color.G;
                    Blue = Swatches.ElementAt(_selectedColorIndex).PixelColor.Color.B;
                }
            }
        }
        public int CanvasHeight
        {
            get
            {
                return _canvasHeight;
            }
            set
            {
                _canvasHeight = value;
                OnPropertyChanged("CanvasHeight");
            }
        }
        public int CanvasWidth
        {
            get
            {
                return _canvasWidth;
            }
            set
            {
                _canvasWidth = value;
                OnPropertyChanged("CanvasWidth");
            }
        }
        public int CanvasPixelHeight
        {
            get
            {
                return CanvasHeight * 32;
            }
            private set { }
        }
        public int CanvasPixelWidth
        {
            get
            {
                return CanvasWidth * 32;
            }
            private set { }
        }
        public int FrameCounter
        {
            get { return _frameCounter; }
            set
            {
                _frameCounter = value;
                _animationFrames.SetCurrentFrameIndex(_frameCounter);
                RectItems = _animationFrames.GetCurrentAnimationFrame();
                if (!_animationFrames.IsRightSize(CanvasWidth, CanvasHeight))
                {
                    CanvasChange();
                }
                OnPropertyChanged("FrameCounter");
            }
        }
        public int FrameCount
        {
            get { return _frameCount; }
            set
            {
                _frameCount = value - 1;
                OnPropertyChanged("FrameCount");
            }
        }
        public float ZoomLevel
        {
            get
            {
                return _zoomLevel;
            }
            set
            {
                _zoomLevel = value; OnPropertyChanged("ZoomLevel");
            }
        }
        
        // =======================

        public MainViewModel()
        {
            Swatches = new ObservableCollection<RectItem>();
            CanvasHeight = 10;
            CanvasWidth = 10;
            RectItems = new ObservableCollection<RectItem>(new List<RectItem>(CanvasWidth * CanvasHeight + 10));
            _animationFrames = new AnimationModel(RectItems);
            _animationFrames.PropertyChanged += (s, e) => { FrameCounter = _animationFrames.CurrentFrameIndex; };
            InitLayout(CanvasHeight, CanvasWidth);
            _cts = new CancellationTokenSource();
        }

        // ===== RELAY COMMANDS =====
        private RelayCommand<RectItem> _editCommand;
        public RelayCommand<RectItem> EditCommand
        {
            get { return _editCommand ?? (_editCommand = new RelayCommand<RectItem>(EditRect, CanDraw)); }
        }

        private RelayCommand _paintCommand;
        public RelayCommand PaintCommand
        {
            get { return _paintCommand ?? (_paintCommand = new RelayCommand(Paint)); }
        }

        private RelayCommand _eraseCommand;
        public RelayCommand EraseCommand
        {
            get { return _eraseCommand ?? (_eraseCommand = new RelayCommand(EraseRect)); }
        }

        private RelayCommand _mouseDownCommand;
        public RelayCommand MouseDownCommand
        {
            get { return _mouseDownCommand ?? (_mouseDownCommand = new RelayCommand(OnMouseDown)); }
        }

        private RelayCommand _mouseUpCommand;
        public RelayCommand MouseUpCommand
        {
            get { return _mouseUpCommand ?? (_mouseUpCommand = new RelayCommand(OnMouseUp)); }
        }

        private RelayCommand _undoCommand;
        public RelayCommand UndoCommand
        {
            get { return _undoCommand ?? (_undoCommand = new RelayCommand(Undo)); }
        }

        private RelayCommand _redoCommand;
        public RelayCommand RedoCommand
        {
            get { return _redoCommand ?? (_redoCommand = new RelayCommand(Redo)); }
        }

        private RelayCommand _clearCommand;
        public RelayCommand ClearCommand
        {
            get { return _clearCommand ?? (_clearCommand = new RelayCommand(ClearCanvas)); }
        }

        private RelayCommand _smallerCanvasCommand;
        public RelayCommand SmallerCanvasCommand
        {
            get { return _smallerCanvasCommand ?? (_smallerCanvasCommand = new RelayCommand(SmallerCanvas)); }
        }

        private RelayCommand _biggerCanvasCommand;
        public RelayCommand BiggerCanvasCommand
        {
            get { return _biggerCanvasCommand ?? (_biggerCanvasCommand = new RelayCommand(BiggerCanvas)); }
        }

        private RelayCommand _swatchCommand;
        public RelayCommand SwatchCommand
        {
            get { return _swatchCommand ?? (_swatchCommand = new RelayCommand(AddSwatch)); }
        }

        private RelayCommand _removeSwatchCommand;
        public RelayCommand RemoveSwatchCommand
        {
            get { return _removeSwatchCommand ?? (_removeSwatchCommand = new RelayCommand(RemoveSwatch)); }
        }

        private RelayCommand<MouseWheelEventArgs> _zoomCommand;
        public RelayCommand<MouseWheelEventArgs> ZoomCommand
        {
            get { return _zoomCommand ?? (_zoomCommand = new RelayCommand<MouseWheelEventArgs>(ScrollZoom)); }
        }

        private RelayCommand _addNewFrameCommand;
        public RelayCommand AddNewFrameCommand
        {
            get { return _addNewFrameCommand ?? (_addNewFrameCommand = new RelayCommand(AddNewFrame)); }
        }

        private RelayCommand _removeFrameCommand;
        public RelayCommand RemoveFrameCommand
        {
            get { return _removeFrameCommand ?? (_removeFrameCommand = new RelayCommand(RemoveFrame)); }
        }

        private RelayCommand _prevFrameCommand;
        public RelayCommand PrevFrameCommand
        {
            get { return _prevFrameCommand ?? (_prevFrameCommand = new RelayCommand(PrevFrame)); }
        }

        private RelayCommand _nextFrameCommand;
        public RelayCommand NextFrameCommand
        {
            get { return _nextFrameCommand ?? (_nextFrameCommand = new RelayCommand(NextFrame)); }
        }

        private RelayCommand _playCommand;
        public RelayCommand PlayCommand
        {
            get { return _playCommand ?? (_playCommand = new RelayCommand(Play)); }
        }
        
        private RelayCommand _saveSpritesheetCommand;
        public RelayCommand SaveSpritesheetCommand
        {
            get { return _saveSpritesheetCommand ?? (_saveSpritesheetCommand = new RelayCommand(SaveSpritesheet)); }
        }

        private RelayCommand _saveAnimationsCommand;
        public RelayCommand SaveAnimationsCommand
        {
            get { return _saveAnimationsCommand ?? (_saveAnimationsCommand = new RelayCommand(SaveAnimationSheet)); }
        }

        private RelayCommand _loadAnimationsCommand;
        public RelayCommand LoadAnimationsCommand
        {
            get { return _loadAnimationsCommand ?? (_loadAnimationsCommand = new RelayCommand(LoadAnimationSheet)); }
        }

        private RelayCommand _saveCommand;
        public RelayCommand SaveCommand
        {
            get { return _saveCommand ?? (_saveCommand = new RelayCommand(Save)); }
        }

        private RelayCommand _loadCommand;
        public RelayCommand LoadCommand
        {
            get { return _loadCommand ?? (_loadCommand = new RelayCommand(Load)); }
        }

        // ===== METHODS ===== 
        public void EditRect(RectItem rec)
        {
            SolidColorBrush adaptionColor = BrushManager.GetBrush(ColorPickerColor);

            if (!rec.PixelColor.Color.Equals(adaptionColor.Color))
            {
                _undoStack.Push(new RectAction(rec, rec.PixelColor, adaptionColor, _actionCounter > 0 ? _currentActionIndex - 1 : _currentActionIndex++));
                rec.PixelColor = adaptionColor;
                _actionCounter++;
            }
        }

        public void Paint()
        {
            ColorPickerColor = ColorPickerColor = Color.FromArgb(255, System.Convert.ToByte(_red), System.Convert.ToByte(_green), System.Convert.ToByte(_blue));
        }

        public void EraseRect()
        {
            ColorPickerColor = Colors.Transparent;
        }

        void OnMouseDown()
        {
            _canDraw = true;
        }

        void OnMouseUp()
        {
            _actionCounter = 0;
            _canDraw = false;
        }

        public void Undo()
        {
            if (_undoStack.Count != 0)
            {
                long startActionIndex = _undoStack.Peek().ActionIndex;
                long newActionIndex = ++_currentActionIndex;
                while (_undoStack.Count != 0 && _undoStack.Peek().ActionIndex == startActionIndex)
                {
                    RectAction lastAction = _undoStack.Pop();
                    _redoStack.Push(new RectAction(lastAction.Rect, lastAction.OriginalColor, lastAction.AdaptedColor, newActionIndex));

                    var selectedRect = RectItems.FirstOrDefault(re => re.X == lastAction.Rect.X && re.Y == lastAction.Rect.Y);
                    if (selectedRect != null)
                    {
                        selectedRect.PixelColor = lastAction.OriginalColor;
                    }
                }
            }
        }

        public void Redo()
        {
            if (_redoStack.Count != 0)
            {
                long startActionIndex = _redoStack.Peek().ActionIndex;
                long newActionIndex = ++_currentActionIndex;
                while (_redoStack.Count != 0 && _redoStack.Peek().ActionIndex == startActionIndex)
                {
                    RectAction lastAction = _redoStack.Pop();
                    _undoStack.Push(new RectAction(lastAction.Rect, lastAction.OriginalColor, lastAction.AdaptedColor, newActionIndex));

                    var selectedRect = RectItems.FirstOrDefault(re => re.X == lastAction.Rect.X && re.Y == lastAction.Rect.Y);
                    if (selectedRect != null)
                    {
                        selectedRect.PixelColor = lastAction.AdaptedColor;
                    }
                }
            }
        }

        private void ClearCanvas()
        {
            RectItems = new ObservableCollection<RectItem>(new List<RectItem>(CanvasWidth * CanvasHeight + 10));
            InitLayout(CanvasHeight, CanvasWidth);
        }

        private void SmallerCanvas()
        {
            CanvasWidth -= 1;
            CanvasHeight -= 1;
            CanvasChange();
        }

        private void BiggerCanvas()
        {
            CanvasWidth += 1;
            CanvasHeight += 1;
            CanvasChange();
        }

        private void CanvasChange()
        {
            List<RectItem> tempList = new List<RectItem>(RectItems.ToList());
            RectItems = new ObservableCollection<RectItem>(new List<RectItem>(CanvasWidth * CanvasHeight + 10));
            InitLayout(CanvasHeight, CanvasWidth, tempList);
        }

        public void AddSwatch()
        {
            RectItem rect = new RectItem();
            rect.PixelColor = BrushManager.GetBrush(ColorPickerColor);
            if (!SwatchesContainsColor(rect.PixelColor))
            {
                Swatches.Add(rect);
                OrderByColor();
            }
        }

        public void RemoveSwatch()
        {
            if (SelectedColorIndex != -1 && Swatches.Count > 0)
                Swatches.RemoveAt(SelectedColorIndex);
        }

        void ScrollZoom(MouseWheelEventArgs args)
        {
            if (args.Delta > 0)
                _zoomLevel += 0.1f;
            else
                _zoomLevel += -0.1f;

            if (_zoomLevel < 0.5f)
                _zoomLevel = 0.5f;
            else if (_zoomLevel > 4f)
                _zoomLevel = 4f;
            else
                _zoomLevel = _zoomLevel;

            OnPropertyChanged("ZoomLevel");
        }

        private void AddNewFrame()
        {
            RectItems = new ObservableCollection<RectItem>(new List<RectItem>(CanvasWidth * CanvasHeight + 10));
            InitLayout(CanvasHeight, CanvasWidth);
            int newFrameIndex = _animationFrames.CurrentFrameIndex + 1;
            _animationFrames.AddAnimationFrame(newFrameIndex, RectItems);
            FrameCount = _animationFrames.GetFrameCount();
        }

        public void RemoveFrame()
        {
            _animationFrames.RemoveAnimationFrame();
            FrameCount = _animationFrames.GetFrameCount();
        }

        public void PrevFrame()
        {
            _animationFrames.CurrentFrameIndex--;
        }

        public void NextFrame()
        {
            _animationFrames.CurrentFrameIndex++;
        }

        private void Play()
        {
            _isAnimating = !_isAnimating;
            if (_isAnimating)
            {
                _task = Task.Run(async () => {
                    while (true)
                    {
                        Animate();
                        await Task.Delay(500, _cts.Token);
                    }
                }, _cts.Token);
            }
            else
            {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
            }
        }

        private void SaveSpritesheet()
        {
            _animationFrames.SaveSpritesheet(CanvasWidth, CanvasHeight);
        }

        private void SaveAnimationSheet()
        {
            _animationFrames.Save(CanvasHeight, CanvasWidth);
        }

        private void LoadAnimationSheet()
        {
            int[] resolution = _animationFrames.Load();
            CanvasWidth = resolution[0];
            CanvasHeight = resolution[1];
            RectItems = _animationFrames.GetAnimationFrame(0);
            FrameCount = _animationFrames.GetFrameCount();
        }

        private void Save()
        {
            RectConverter.RectToBitmap(RectItems.ToList(), CanvasWidth, CanvasHeight);

            SaveFileDialog saveFile = new SaveFileDialog();
            var result = saveFile.ShowDialog();
            if (result == true)
            {
                List<object> dataObjects = new List<object>();
                var rectCount = RectItems.Count;
                dataObjects.Add(CanvasHeight);
                dataObjects.Add(CanvasWidth);
                dataObjects.Add(rectCount);

                foreach (var rectItem in RectItems.ToArray())
                {
                    dataObjects.Add(rectItem.X);
                    dataObjects.Add(rectItem.Y);
                    dataObjects.Add(rectItem.PixelColor.Color.ToString());
                }
                Serialization.WriteToBinaryFile(saveFile.FileName + ".bin", dataObjects.ToArray());
            }
        }

        private void Load()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select sprite file";
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
                    int rectCount = int.Parse(data[2].ToString());

                    RectItems.Clear();

                    for (int i = 0; i < (canvasHeight * canvasWidth) * 3; i += 3)
                    {
                        string colorInfo = data[i + 5].ToString();

                        RectItems.Add(new RectItem
                        {
                            X = int.Parse(data[i + 3].ToString()),
                            Y = int.Parse(data[i + 4].ToString()),
                            Height = 32,
                            Width = 32,
                            PixelColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(colorInfo))
                        });
                    }
                }
                catch
                {
                    MessageBox.Show("Incorrect filetype! You've opened animation sequence!");
                }
            }
            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
        }

        private void InitLayout(int columns, int rows)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    RectItems.Add(new RectItem
                    {
                        X = column * 32,
                        Y = row * 32,
                        Height = 32,
                        Width = 32,
                        PixelColor = BrushManager.GetBrush(Color.FromArgb(0, 23, 23, 23))
                    });
                }
            }
        }

        private void InitLayout(int columns, int rows, List<RectItem> currentCanvas)
        {
            InitLayout(columns, rows);
            foreach (var rect in currentCanvas)
            {
                var selectedRect = RectItems.FirstOrDefault(re => re.X == rect.X && re.Y == rect.Y);
                if (selectedRect != null)
                {
                    selectedRect.PixelColor = rect.PixelColor;
                }
            }
            _animationFrames.SetCurrentFrame(RectItems);
        }

        private Boolean SwatchesContainsColor(SolidColorBrush swatchColor)
        {
            foreach (var swatch in Swatches)
            {
                if (swatchColor.Color == swatch.PixelColor.Color)
                {
                    return true;
                }
            }
            return false;
        }

        bool CanDraw(RectItem rect)
        {
            return _canDraw;
        }

        private void Animate()
        {
            _animationFrames.CurrentFrameIndex++;
        }

        public void OrderByColor()
        {
            List<RectItem> orderedList = Swatches.OrderBy(swatch => Convert.ToInt32(swatch.PixelColor.Color.R))
                .ThenBy(swatch => Convert.ToInt32(swatch.PixelColor.Color.G))
                .ThenBy(swatch => Convert.ToInt32(swatch.PixelColor.Color.B)).ToList();
            Swatches.Clear();
            foreach (RectItem swatch in orderedList)
            {
                Swatches.Add(swatch);
            }
        }

        // ===================

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
