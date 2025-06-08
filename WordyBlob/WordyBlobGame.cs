using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using System.Text;

namespace WordyBlob
{
    enum EInBorder
    {
        Unknown,
        Left,
        Top,
        Right,
        Bottom
    }

    public class WordyBlobGame
    {
        Random _Rnd = new Random();
        RectF _frameRect;
        bool _bRunning = true;
        int _iFramesPerAnimationFrame = 1;
        public static int _iFrameNum = 0, _iAnimatedFrameNum = 0;
        int _iMaxTime = 60, _iCurrTime = 0; // Seconds
        // Background
        private Microsoft.Maui.Graphics.IImage? _BackgroundImage;
        int _iScore = 0, _iTopScore = 0;

        // Moving letter tiles
        List<TileMoving> _TilesMoving = new List<TileMoving>();
        // Which tile is currently being dragged around the border
        int _iTileBeingDragged = -1;
        EInBorder _eCurrBorder = EInBorder.Unknown;

        // Other moving letter tiles
        const int _iMaxAnimatingTileFrame = 5;

        // Grid of letter tiles - the eponymous blob
        bool _bBlobCollapsing = false; // Indicates that the collapsing animation is running so don't accept any user input.
        const int _iNumTileTypes = 26;
        private Microsoft.Maui.Graphics.IImage?[] _TileImages; // One for each letter
        const int _iTileGridWid = 9;
        const int _iTileGridHei = 11;
        GridCell[,] _gridCells, _gridCellsCollapsed, _gridCellsPrev;
        Pos2 _posGridTouch = new Pos2();

        // Tiles in the bag
        const int _iNumTilesTotal = 98;
        char[] _cTilesInBag = new char[_iNumTilesTotal];
        //                                              A  B  C  D  E   F  G  H  I  J  K  L  M  N  O  P  Q  R  S  T  U  V  W  X  Y  Z
        static readonly int[] _iLettersDistribution = { 9, 2, 2, 4, 12, 2, 3, 2, 9, 1, 1, 4, 2, 6, 8, 2, 1, 6, 4, 6, 4, 2, 2, 1, 2, 1 };
        int _iNextTileInBag = 0;

        bool _bUserBorder = false;
        float _fTileOverallScale = 0.15f; // Determines the size of the whole game, as a fraction of the screen width
        float _fBorderWid = 64.0f;
        float _fTileSizeBig = 64.0f;
        float _fTileSizeGrid = 48.0f;
        public const float _fTileStrideRel = 0.9375f;

        // Timing frames for debug purposes
        static public int _iFrameTime;
        int _iFrameTimeTotal, _iAnimatedFrameTime;
        const int _iAnimatedFrameTimeFrameSpan = 20;
        Vect2 _vTouchDownPos, _vTouchMovePos, _vTouchUpPos, _vTapPos;

        // Dictionary
        TrieDictionary _trieDict;

        public WordyBlobGame()
        {
            MainPage._Game = this;
            CreateShuffledLetterTileBag();
            DrawFuncs.Initialize();
            _trieDict = new TrieDictionary();
            _gridCells = new GridCell[_iTileGridWid, _iTileGridHei];
            _gridCellsCollapsed = new GridCell[_iTileGridWid, _iTileGridHei];
            _gridCellsPrev = new GridCell[_iTileGridWid, _iTileGridHei];
            _TileImages = new Microsoft.Maui.Graphics.IImage[_iNumTileTypes];
            Initialize().Wait();
        }

        private void CreateShuffledLetterTileBag()
        {
            List<char> letters = new List<char>();
            for(int i = 0; i < 26; i++)
            {
                for(int j = 0; j < _iLettersDistribution[i]; j++)
                {
                    letters.Add((char)(i + 'A'));
                }
            }
            for (int i = 0; i < _iNumTilesTotal; i++)
            {
                int j = _Rnd.Next(_iNumTilesTotal - i);
                _cTilesInBag[i] = letters[j];
                letters.RemoveAt(j);
            }
            _iNextTileInBag = 0;
        }

        private char NextTileFromBag()
        {
            _iNextTileInBag++;
            if (_iNextTileInBag >= _iNumTilesTotal)
                _iNextTileInBag = 0;
            return _cTilesInBag[_iNextTileInBag];
        }

        private async Task Initialize()
        {
            _BackgroundImage = await LoadImageFromRes("background.png");
            ClearGrid();
            CreateABlob(4);
            /*
            _gridCells[_iTileGridWid / 2, _iTileGridHei / 2] = new GridCell(NextTileFromBag(), false);
            for (int yTile = 2; yTile < _iTileGridHei - 2; yTile++)
            {
                for (int xTile = 1; xTile < _iTileGridWid - 1; xTile++)
                {
                    int n = _Rnd.Next(30);
                    _gridCells[xTile, yTile] = new GridCell((char)(n < 26 ? (n + 'A') : 0), false);
                }
            }
            */


            FindEdgesOfBlob();

            for (int i = 0; i < _iNumTileTypes; i++)
            {
                _TileImages[i] = await LoadImageFromRes("tile_" + (char)((char)i + 'a') + ".png");
            }

            LoadSettingsFromFile();
        }

        // Create a circular blob of tiles.
        private void CreateABlob(int radius)
        {
            int xCentre = _iTileGridWid / 2;
            int yCentre = _iTileGridHei / 2;
            int count = 0;
            for (int yr = -radius; yr <= radius; yr++)
            {
                int xStart = (int)Math.Sqrt((double)(radius * radius - yr * yr));
                for(int xr = -xStart; xr <= xStart; xr++)
                {
                    int x = xCentre + xr;
                    int y = yCentre + yr;
                    if (x >= 0 && y >= 0 && x < _iTileGridWid && y < _iTileGridHei)
                    {
                        _gridCells[x, y] = new GridCell(NextTileFromBag(), false);
                        count++;
                        if (count == 26)
                            count = 0;
                    }
                }
            }
        }

        // For debug purposes, create a square ring of tiles so we can see if it collapses correctly.
        private void CreateARing(int radius)
        {
            int dx, dy;
            int xCentre = _iTileGridWid / 2;
            int yCentre = _iTileGridHei / 2;
            dx = dy = -radius;
            int count = 0;
            do
            {
                int x = xCentre + dx;
                int y = yCentre + dy;
                _gridCells[x, y] = new GridCell((char)(count + 'A'), false);
                count++;
            }
            while (IterateRoundASquareClockwise(radius, ref dx, ref dy));
        }

        static public async Task<Microsoft.Maui.Graphics.IImage?> LoadImageFromRes(string filename)
        {
            Microsoft.Maui.Graphics.IImage? img = null;
            try
            {
                // Load the image stream from resources
                bool bExists = await FileSystem.Current.AppPackageFileExistsAsync(filename);
                if (bExists)
                {
                    using Stream stream = await FileSystem.Current.OpenAppPackageFileAsync(filename);
                    // Convert stream to IImage
                    img = PlatformImage.FromStream(stream);
                    stream.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
            }
            return img;
        }

        private int IntFromFileStream(StreamReader sr, int iDefaultValue)
        {
            string? s = sr.ReadLine();
            if (s == null)
                return iDefaultValue;
            int iResult;
            if (int.TryParse(s, out iResult))
                return iResult;
            return iDefaultValue;
        }

        private void LoadSettingsFromFile()
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.dat");
            if (!File.Exists(filePath))
                return;
            using (StreamReader sr = new StreamReader(filePath))
            {
                //_iSomeData = IntFromFileStream(sr, _iSomeData);
                sr.Close();
            }
        }

        private void SaveSettingsToFile()
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.dat");
            using (StreamWriter sw = new StreamWriter(filePath, append: false))
            {
                //sw.WriteLine(_iSomeData.ToString());
                sw.Flush();
                sw.Close();
            }
        }

        public int Score
        {
            get
            {
                return _iScore;
            }
        }

        public int TopScore
        {
            get
            {
                return _iTopScore;
            }
        }

        public int TheTime
        {
            get
            {
                return _iCurrTime;
            }
            set
            {
                _iCurrTime = value;
            }
        }

        public bool IsRunning
        {
            get { return _bRunning; }
            set
            {
                _bRunning = value;
            }
        }
        public int AnimationFrame
        {
            get
            {
                return _iAnimatedFrameNum;
            }
        }
        public void DrawCurrentFrame(ICanvas canvas, RectF frameRect)
        {
            _frameRect = frameRect;
            _fBorderWid = _frameRect.Width * _fTileOverallScale;
            _fTileSizeBig = _fBorderWid;
            _fTileSizeGrid = _fTileSizeBig * 0.75f;
            if (_TilesMoving.Count < 1)
            {
                _iTileBeingDragged = 0;
                _TilesMoving.Add(new TileMoving(NextTileFromBag(), _fTileSizeBig, true, new Vect2(_frameRect.Width * 0.5f, _fTileSizeBig * 0.5f)));
            }

            canvas.DrawImage(_BackgroundImage, 0, 0, _frameRect.Width, _frameRect.Height);
            DrawBackgroundToGrid(canvas);
            DrawPotentialDropLocationsInTilesGrid(canvas);
            DrawTouchLocationInTilesGrid(canvas);
            DrawGridOfTiles(canvas, _frameRect);
            ShowWordsInGrid(canvas);

            foreach (TileMoving tile in _TilesMoving)
            {
                tile.Draw(canvas);
            }


            if (!_bRunning)
            {
                //canvas.FillColor = Color.FromRgba(192, 208, 255, 128);
                //canvas.FillRectangle(frameRect);
            }

            if (MainPage._bShowDebugInfo)
            {
                canvas.FontSize = 20;
                canvas.FontColor = Colors.White;
                canvas.DrawString(_iAnimatedFrameTime.ToString(), 0, 20, HorizontalAlignment.Left);
                canvas.DrawString("Down: " + _vTouchDownPos.X.ToString() + "," + _vTouchDownPos.Y.ToString(), 0, 50, HorizontalAlignment.Left);
                canvas.DrawString("Move: " + _vTouchMovePos.X.ToString() + "," + _vTouchMovePos.Y.ToString(), 0, 100, HorizontalAlignment.Left);
                canvas.DrawString("Up: " + _vTouchUpPos.X.ToString() + "," + _vTouchUpPos.Y.ToString(), 0, 150, HorizontalAlignment.Left);
                canvas.DrawString("Tap: " + _vTapPos.X.ToString() + "," + _vTapPos.Y.ToString(), 0, 200, HorizontalAlignment.Left);
            }
        }

        // Draw the grid lines and fill in the background to the grid.
        private void DrawBackgroundToGrid(ICanvas canvas)
        {
            if (_bUserBorder)
            {
                float b2 = _fBorderWid * 2.0f;
                canvas.FillColor = new Color(208, 255, 192);
                canvas.FillRectangle(_fBorderWid, _fBorderWid, _frameRect.Width - b2, _frameRect.Height - b2);
                canvas.DrawRectangle(_fBorderWid, _fBorderWid, _frameRect.Width - b2, _frameRect.Height - b2);
            }

            float fSpace = _fTileSizeGrid * _fTileStrideRel;
            float xGridLeft = (_frameRect.Width - (float)_iTileGridWid * fSpace) * 0.5f;
            float xGridRight = xGridLeft + (float)_iTileGridWid * fSpace;
            float yGridTop = (_frameRect.Height - (float)_iTileGridHei * fSpace) * 0.5f;
            float yGridBottom = yGridTop + (float)_iTileGridHei * fSpace;
            float xGrid = xGridLeft;
            int xCentre = _iTileGridWid / 2;
            for (int x = 0; x <= _iTileGridWid; x++)
            {
                canvas.StrokeColor = (x == xCentre && _iTileGridWid % 2 == 0) ? new Color(0, 0, 0, 255) : new Color(0, 0, 0, 32);
                canvas.DrawLine(xGrid, yGridTop, xGrid, yGridBottom);
                xGrid += fSpace;
            }
            float yGrid = yGridTop;
            int yCentre = _iTileGridHei / 2;
            for (int y = 0; y <= _iTileGridHei; y++)
            {
                canvas.StrokeColor = (y == yCentre && _iTileGridHei % 2 == 0) ? new Color(0, 0, 0, 255) : new Color(0, 0, 0, 32);
                canvas.DrawLine(xGridLeft, yGrid, xGridRight, yGrid);
                yGrid += fSpace;
            }
            if(_iTileGridWid % 2 != 0 && _iTileGridHei % 2 != 0)
            {
                canvas.StrokeColor = new Color(0, 0, 0, 255);
                canvas.DrawRectangle(ScreenRectFromGridPos(new Pos2(xCentre, yCentre)));
            }
        }

        // Show all the words that are currently selected in the grid.
        private void ShowWordsInGrid(ICanvas canvas)
        {
            canvas.StrokeColor = new Color(0, 0, 255);
            canvas.StrokeSize = _fTileSizeGrid * 0.1f;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    if (_gridCells[x, y]._iInWord != 0)
                    {
                        RectF rect = ScreenRectFromGridPos(new Pos2(x, y));
                        canvas.DrawRoundedRectangle(rect, _fTileSizeGrid * 4.0f * (1.0f - _fTileStrideRel));
                    }
                }
            }
            canvas.StrokeColor = new Color(0, 0, 0);
            canvas.StrokeSize = 1.0f;
        }

        // Show the cells all around the blob into which the next tile could be dropped.
        private void DrawPotentialDropLocationsInTilesGrid(ICanvas canvas)
        {
            canvas.StrokeColor = new Color(0, 128, 0, 64);
            canvas.StrokeSize = _fTileSizeGrid * 0.05f;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    if(_gridCells[x, y]._bOnEdgeOfBlob)
                    {
                        RectF rect = ScreenRectFromGridPos(new Pos2(x, y));
                        canvas.DrawRoundedRectangle(rect, _fTileSizeGrid * 4.0f * (1.0f - _fTileStrideRel));
                    }
                }
            }
            canvas.StrokeColor = new Color(0, 0, 0);
            canvas.StrokeSize = 1.0f;
        }

        private void DrawTouchLocationInTilesGrid(ICanvas canvas)
        {
            if (!_posGridTouch._bDefined)
                return;
            RectF rect = ScreenRectFromGridPos(_posGridTouch);
            canvas.StrokeColor = new Color(255, 0, 0);
            canvas.StrokeSize = _fTileSizeGrid * 0.1f;
            canvas.FillRoundedRectangle(rect, _fTileSizeGrid * 4.0f * (1.0f - _fTileStrideRel));
            canvas.DrawRoundedRectangle(rect, _fTileSizeGrid * 4.0f * (1.0f - _fTileStrideRel));
            canvas.StrokeColor = new Color(0, 0, 0);
            canvas.StrokeSize = 1.0f;
        }

        private void DrawGridOfTiles(ICanvas canvas, RectF frameRect)
        {
            float fSpace = _fTileSizeGrid * _fTileStrideRel;
            float fGap2 = (_fTileSizeGrid - fSpace) * 0.5f;
            float xOffset = (_frameRect.Width - (float)_iTileGridWid * fSpace) * 0.5f - fGap2;
            float yOffset = (_frameRect.Height - (float)_iTileGridHei * fSpace) * 0.5f - fGap2;
            for (int yTile = 0; yTile < _iTileGridHei; yTile++)
            {
                for (int xTile = 0; xTile < _iTileGridWid; xTile++)
                {
                    if(_gridCells[xTile, yTile]._char > 1)
                        canvas.DrawImage(TileImageAtPos(xTile, yTile), 
                                                xTile * fSpace + xOffset, 
                                                yTile * fSpace + yOffset, _fTileSizeGrid, _fTileSizeGrid);
                }
            }
        }

        // Given a position in the border, returns the next available position in the tile grid
        // for a tile dropped from that position in the border.
        private Pos2 GridPosFromBorderPos(Vect2 posBorder, EInBorder eWhichBorder)
        {
            Pos2 posGrid = GridPosFromScreenPos(posBorder);
            return GridPosFromGridPos(posGrid, eWhichBorder);
        }
        // Given a position in the grid, returns the next available position in the grid
        // for a tile dropped from that position from the direction given.
        private Pos2 GridPosFromGridPos(Pos2 posGrid, EInBorder eWhichBorder)
        {
            switch (eWhichBorder)
            {
                case EInBorder.Left:
                    // If dropped from the left it falls right first and then up or down.
                    if (posGrid.Y >= 0 && posGrid.Y < _iTileGridHei)
                    {
                        if (_gridCells[0, posGrid.Y]._char == 0)
                        {
                            int iEnd = _iTileGridWid / 2;
                            for (int i = 1; i < iEnd; i++)
                            {
                                if (_gridCells[i, posGrid.Y]._char != 0)
                                    return new Pos2(i - 1, posGrid.Y);
                            }
                        }
                    }
                    return Pos2.NULL;
                case EInBorder.Right:
                    // If dropped from the right it falls left first and then up or down.
                    if (posGrid.Y >= 0 && posGrid.Y < _iTileGridHei)
                    {
                        int iStart = _iTileGridWid / 2;
                        for (int i = iStart; i < _iTileGridWid; i++)
                        {
                            if (_gridCells[i, posGrid.Y]._char == 0)
                                return new Pos2(i, posGrid.Y);
                        }
                    }
                    return Pos2.NULL;
                case EInBorder.Top:
                    // If dropped from the top it falls down first and then up or left or right.
                    if (posGrid.X >= 0 && posGrid.X < _iTileGridWid)
                    {
                        int iStart = _iTileGridHei / 2 - 1;
                        for (int i = iStart; i >= 0; i--)
                        {
                            if (_gridCells[posGrid.X, i]._char == 0)
                                return new Pos2(posGrid.X, i);
                        }
                    }
                    return Pos2.NULL;
                case EInBorder.Bottom:
                    // If dropped from the bottom it falls up first and left or right.
                    if (posGrid.X >= 0 && posGrid.X < _iTileGridWid)
                    {
                        int iStart = _iTileGridHei / 2;
                        for (int i = iStart; i < _iTileGridHei; i++)
                        {
                            if (_gridCells[posGrid.X, i]._char == 0)
                                return new Pos2(posGrid.X, i);
                        }
                    }
                    return Pos2.NULL;
            }
            return Pos2.NULL;
        }

        private GridCell TileAt(Pos2 posGrid)
        {
            return _gridCells[posGrid.X, posGrid.Y];
        }

        private void SetTileCharAt(Pos2 posGrid, char c)
        {
           _gridCells[posGrid.X, posGrid.Y]._char = c;
        }

        // Checks whether the given position is in the grid.
        private bool GridPosIsInGrid(Pos2 posGrid)
        {
            return posGrid.X >= 0 && posGrid.Y >= 0 && posGrid.X < _iTileGridWid && posGrid.Y < _iTileGridHei;
        }

        // Given a position on the screen, returns the position on the tile grid, or null
        // if the position is not in the grid.
        private Pos2 GridPosFromScreenPosOrNull(Vect2 pos)
        {
            Pos2 posGrid = GridPosFromScreenPos(pos);
            return GridPosIsInGrid(posGrid) ? posGrid : Pos2.NULL;
        }

        // Given a position on the screen, returns the position on or off the tile grid.
        private Pos2 GridPosFromScreenPos(Vect2 pos)
        {
            float fSpace = _fTileSizeGrid * _fTileStrideRel;
            float xGridLeft = (_frameRect.Width - (float)_iTileGridWid * fSpace) * 0.5f;
            float yGridTop = (_frameRect.Height - (float)_iTileGridHei * fSpace) * 0.5f;
            int x = (int)((pos.X - xGridLeft) / fSpace);
            int y = (int)((pos.Y - yGridTop) / fSpace);
            return new Pos2(x, y);
        }

        // Given a position in the tile grid, returns the screen position of [the centre of] that position.
        private Vect2 ScreenPosFromGridPos(Pos2 posGrid, bool bCentre = true)
        {
            float fSpace = _fTileSizeGrid * _fTileStrideRel;
            float fSpace2 = fSpace * 0.5f;
            float xGridLeft = (_frameRect.Width - (float)_iTileGridWid * fSpace) * 0.5f;
            float yGridTop = (_frameRect.Height - (float)_iTileGridHei * fSpace) * 0.5f;
            if(bCentre)
                return new Vect2(xGridLeft + ((float)posGrid.X * fSpace) + fSpace2,
                                 yGridTop + ((float)posGrid.Y * fSpace) + fSpace2);
            return new Vect2(xGridLeft + ((float)posGrid.X * fSpace),
                             yGridTop + ((float)posGrid.Y * fSpace));
        }

        // Given a position in the tile grid, returns the bounding rectangle of a tile drawn at that position.
        private RectF ScreenRectFromGridPos(Pos2 posGrid)
        {
            Vect2 posScreen = ScreenPosFromGridPos(posGrid);
            float fSpace = _fTileSizeGrid * _fTileStrideRel;
            float fSpace2 = fSpace * 0.5f;
            return new RectF((float)posScreen.X - fSpace2, (float)posScreen.Y - fSpace2, fSpace, fSpace);
        }

        // Given a position in the grid, returns the image for the tile at that position, or null
        // if there is no tile at that position.
        private Microsoft.Maui.Graphics.IImage? TileImageAtPos(int x, int y)
        {
            char c = _gridCells[x, y]._char;
            if (c == 0)
                return null;
            return TileImageForChar(c);
        }

        public Microsoft.Maui.Graphics.IImage? TileImageForChar(char c)
        {
            if (c < 'A')
                return null;
            return _TileImages[(int)(c - 'A')];
        }

        // Mark all grid cells around the edge of the blob as potential tile drop locations.
        // Done in such a way that it keeps its blob shape, with no overhangs.
        private void FindEdgesOfBlob()
        {
            int xCentre = _iTileGridWid / 2;
            int yCentre = _iTileGridHei / 2;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    _gridCells[x, y]._bOnEdgeOfBlob = false;
                    if ((x == xCentre && y == yCentre) || _gridCells[x, y]._char != 0)
                        continue;
                    if (Math.Abs(x - xCentre) > Math.Abs(y - yCentre))
                    {
                        int support = x + (x < xCentre ? 1 : -1);
                        _gridCells[x, y]._bOnEdgeOfBlob = _gridCells[support, y]._char != 0;
                    }
                    else
                    {
                        int support = y + (y < yCentre ? 1 : -1);
                        _gridCells[x, y]._bOnEdgeOfBlob = _gridCells[x, support]._char != 0;
                    }
                    //bOnEdge = ((x > 0 && _gridCells[x - 1, y]._char != 0) ||
                    //           (y > 0 && _gridCells[x, y - 1]._char != 0) ||
                    //           (x < _iTileGridWid - 1 && _gridCells[x + 1, y]._char != 0) ||
                    //           (y < _iTileGridHei - 1 && _gridCells[x, y + 1]._char != 0)) &&
                    //           _gridCells[x, y]._char == 0;
                }
            }
        }

        // Add an item to the list of animating tiles.
        private void AddAnAnimatingTile(char c, Pos2 from, Pos2 to)
        {
            float fEdge = 0.5f * _fTileSizeGrid * (1.0f - _fTileStrideRel);
            Vect2 vFrom = ScreenPosFromGridPos(from, false) - fEdge;
            Vect2 vTo = ScreenPosFromGridPos(to, false) - fEdge;
            TileMoving tile = new TileMoving(c, _fTileSizeGrid, vFrom, vTo, to, _iMaxAnimatingTileFrame, true);
            _TilesMoving.Add(tile);
        }

        public void IncrementAnimationFrame()
        {
            if (!_bRunning)
                return;

            for (int i = 0; i < _iFramesPerAnimationFrame; i++)
            {
                IncrementFrame();
            }

            if (MainPage._bShowDebugInfo)
            {
                _iFrameTimeTotal += _iFrameTime;
                if (_iAnimatedFrameNum % _iAnimatedFrameTimeFrameSpan == 0)
                {
                    _iAnimatedFrameTime = _iFrameTimeTotal / _iAnimatedFrameTimeFrameSpan;
                    _iFrameTimeTotal = 0;
                }
            }
            _iAnimatedFrameNum++;
            if (_iAnimatedFrameNum % MainPage._iFramesPerSecond == 0)
            {
                _iCurrTime--;
                if (_iCurrTime < 0)
                {
                    //IsRunning = false;
                    return;
                }
                if (MainPage._This != null)
                {
                    MainPage._This.UpdateTheClockNextFrame();
                    MainPage._This.UpdateTheDigitalTimeNextFrame();
                }
            }
        }

        private void IncrementFrame()
        {
            foreach (TileMoving tile in _TilesMoving)
            {
                tile.Increment();
            }

            // If we've got more than 1 animating tile, we must be running a tile animation.
            if(_bBlobCollapsing && _TilesMoving.Count > 1)
            {
                // If that tile animation has just finished, flag that collapsing has finished.
                if (_TilesMoving[1].CanDeleteNow)
                {
                    _bBlobCollapsing = false;
                    _TilesMoving.RemoveRange(1, _TilesMoving.Count - 1);
                    CopyGrid(_gridCellsCollapsed, _gridCells);
                    FindEdgesOfBlob();
                    CollapseBlob();
                }
            }
            _iFrameNum++;
        }


        // Remove all tiles from the grid.
        private void ClearGrid()
        {
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    _gridCells[x, y].RemoveTile();
                }
            }
        }

        // Copy the grid.
        private void CopyGrid(GridCell[,] from, GridCell[,] to)
        {
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    to[x,y] = from[x, y];
                }
            }
        }

        // Reset found words.
        private void ClearWords()
        {
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    _gridCells[x, y]._iInWord = 0;
                }
            }
        }

        // Use the dictionary to look for valid words in the whole grid.
        private int CheckForWordsInWholeGrid()
        {
            ClearWords();
            int iWordNum = 0;

            // Check for horizontal words.
            int x, x1, x2, iLen = 0;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (x1 = 0; x1 < _iTileGridWid - 1; x1++)
                {
                    if(_gridCells[x1, y]._char != 0)
                    {
                        for (x2 = x1 + 1; x2 < _iTileGridWid && _gridCells[x2, y]._char != 0; x2++);
                        StringBuilder s = new StringBuilder();
                        for (x = x1; x < x2; x++)
                            s.Append(_gridCells[x, y]._char);
                        if (_trieDict.LongestWordInString(s.ToString(), ref iLen) > 1)
                        {
                            // TODO
                        }
                    }
                }
            }

            return iWordNum;
        }


        // Use the dictionary to look for valid words centred on the given tile.
        // The given cell is assumed to contain a tile.
        private int CheckForWordsCentredOn(Pos2 posGridCentre)
        {
            ClearWords();
            int iWordNum = 0;
            if (_gridCells[posGridCentre.X, posGridCentre.Y]._char == 0)
                return 0;

            // Check horizontally.
            // Find the maximum horizontal extents of the row.
            int xStart = posGridCentre.X;
            int xEnd = xStart;
            while (xStart >= 0 && _gridCells[xStart, posGridCentre.Y]._char != 0)
                xStart--;
            xStart++;
            while (xEnd < _iTileGridWid && _gridCells[xEnd, posGridCentre.Y]._char != 0)
                xEnd++;
            xEnd--;

            for(int x1 = xStart; x1 <= posGridCentre.X; x1++)
            {
                for (int x2 = posGridCentre.X; x2 <= xEnd; x2++)
                {
                    if (x2 - x1 > 0)
                    {
                        StringBuilder s = new StringBuilder();
                        for (int x = x1; x <= x2; x++)
                            s.Append(_gridCells[x, posGridCentre.Y]._char);
                        if(_trieDict.SearchForWord(s.ToString()))
                        {
                            iWordNum++;
                            for (int x = x1; x <= x2; x++)
                                _gridCells[x, posGridCentre.Y]._iInWord = iWordNum;
                        }
                    }
                }
            }

            // Check vertically.
            // Find the maximum vertical extents of the column.
            int yStart = posGridCentre.Y;
            int yEnd = yStart;
            while (yStart >= 0 && _gridCells[posGridCentre.X, yStart]._char != 0)
                yStart--;
            yStart++;
            while (yEnd < _iTileGridHei && _gridCells[posGridCentre.X, yEnd]._char != 0)
                yEnd++;
            yEnd--;

            for (int y1 = yStart; y1 <= posGridCentre.Y; y1++)
            {
                for (int y2 = posGridCentre.Y; y2 <= yEnd; y2++)
                {
                    if (y2 - y1 > 0)
                    {
                        StringBuilder s = new StringBuilder();
                        for (int y = y1; y <= y2; y++)
                        {
                            s.Append(_gridCells[posGridCentre.X, y]._char);
                        }
                        if (_trieDict.SearchForWord(s.ToString()))
                        {
                            iWordNum++;
                            for (int y = y1; y <= y2; y++)
                                _gridCells[posGridCentre.X, y]._iInWord = iWordNum;
                        }
                    }

                }
            }
            return iWordNum;
        }

        // After calling CheckForWords(), remove all the words that were found.
        private void RemoveAllFoundWords()
        {
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    if(_gridCells[x, y]._iInWord != 0)
                    {
                        _gridCells[x, y].RemoveTile();
                    }
                }
            }
        }

        // Remove all spaces in the blob by collapsing tiles towards the centre according to the following algorithm:
        // In concentric rings starting with the 8 tiles that surround the centre tile:
        //   For each tile in the ring:
        //     do:
        //       If the vertical distance to the centre is greater than the horizintal distance and the next closest cell vertically is empty:
        //         Move vertically one cell towards the centre.
        //       Else if the next closest cell horizontally is empty:
        //         Move horizontally one cell towards the centre.
        //     until there is a filled cell either horizontally or vertically "below" this tile.
        private void CollapseBlob()
        {
            _bBlobCollapsing = false;
            CopyGrid(_gridCells, _gridCellsCollapsed);
            int xCentre = _iTileGridWid / 2;
            int yCentre = _iTileGridHei / 2;
            int iMax = Math.Max(xCentre, yCentre);
            int i, dx, dy, x, y;
            // For each concentric ring
            for (i = 1; i <= iMax; i++)
            {
                dx = dy = -i;
                // For each cell around that ring
                do
                {
                    x = xCentre + dx;
                    y = yCentre + dy;
                    if (x < 0 || y < 0 || x >= _iTileGridWid || y >= _iTileGridHei)
                        continue;
                    char ch = _gridCells[x, y]._char;
                    // We're moving tiles inwards from this ring, so only consider cells containing tiles.
                    if (ch == 0)
                        continue;
                    // If the x distance from the centre is greater than the y distance from the centre, the tile will 
                    // fall horizontally, otherwise it will fall vertically.
                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        // If the tile is left of centre, consider falling to the right, otherwise left.
                        int x1 = dx + (dx < 0 ? 1 : -1);
                        // Fall if the cell we're considering falling into is empty.
                        if (_gridCellsCollapsed[xCentre + x1, y]._char == 0)
                        {
                            // In the collapsed copy of the grid, move the tile. Also remove it from the live copy.
                            _gridCellsCollapsed[xCentre + x1, y] = _gridCellsCollapsed[x, y];
                            _gridCells[x, y].RemoveTile();
                            _gridCellsCollapsed[x, y].RemoveTile();
                            // Create a moving tile, going from the old to the new position.
                            AddAnAnimatingTile(ch, new Pos2(x, y), new Pos2(xCentre + x1, y));
                            _bBlobCollapsing = true;
                        }
                    }
                    else
                    {
                        // If the tile is above the centre, consider falling down, otherwise up.
                        int y1 = dy + (dy < 0 ? 1 : -1);
                        // Fall if the cell we're considering falling into is empty.
                        if (_gridCellsCollapsed[x, yCentre + y1]._char == 0)
                        {
                            // In the collapsed copy of the grid, move the tile. Also remove it from the live copy.
                            _gridCellsCollapsed[x, yCentre + y1] = _gridCellsCollapsed[x, y];
                            _gridCells[x, y].RemoveTile();
                            _gridCellsCollapsed[x, y].RemoveTile();
                            // Create a moving tile, going from the old to the new position.
                            AddAnAnimatingTile(ch, new Pos2(x, y), new Pos2(x, yCentre + y1));
                            _bBlobCollapsing = true;
                        }
                    }
                }
                while (IterateRoundASquareClockwise(i, ref dx, ref dy));
            }
            // If none of the above resulted in animations of tiles collapsing, we can finish the collapse process right
            // here. If it did result in those animations then the process will finish in the Increment() method when the
            // moving tiles have reached their intended destination.
            if (!_bBlobCollapsing)
            {
                CopyGrid(_gridCellsCollapsed, _gridCells);
                FindEdgesOfBlob();
            }
        }

        // For a square ring of radius i, iterate x and y around it clockwise,
        // assuming x,y starts at -i,-i (top left corner).
        // Returns false when it gets back to the start point.
        private bool IterateRoundASquareClockwise(int i, ref int x, ref int y)
        {
            if (y == -i)
            {
                if (x == i)
                    y++;
                else
                    x++;
                return true;
            }
            if(x == i)
            {
                if (y == i)
                    x--;
                else
                    y++;
                return true;
            }
            if(y == i)
            {
                if (x == -i)
                    y--;
                else
                    x--;
                return true;
            }
            if (x == -i)
            {
                y--;
                if (y == -i)
                    return false;
            }
            return true;
        }

        public void OnTapped(PointF pntTouched)
        {
            if (_bBlobCollapsing)
                return;
            if (_TilesMoving.Count < 1)
                return;
            _vTapPos = pntTouched;
            if (_iTileBeingDragged >= 0)
            {
                _posGridTouch = GridPosFromScreenPosOrNull(_vTapPos);
                if (_posGridTouch._bDefined)
                {
                    if (TileAt(_posGridTouch)._bOnEdgeOfBlob)
                    {
                        // If we've tapped on an empty cell at the edge of the blob, put a tile there.
                        SetTileCharAt(_posGridTouch, _TilesMoving[_iTileBeingDragged].Letter);
                        _TilesMoving[_iTileBeingDragged] = new TileMoving(NextTileFromBag(), _fTileSizeBig, true,
                                                              new Vect2(_frameRect.Width * 0.5f, _fTileSizeBig * 0.5f));
                        FindEdgesOfBlob();
                        // TODO: Animate dropping tile onto blob.
                    }
                    else if(TileAt(_posGridTouch)._char != 0 && TileAt(_posGridTouch)._iInWord != 0)
                    {
                        // Otherwise, if we've tapped on a cell with a tile in it, remove words centred on that position.
                        RemoveAllFoundWords();
                        CollapseBlob();
                    }
                }
            }
        }

        public void OnTouchDown(PointF pntTouched)
        {
            if (_bBlobCollapsing)
                return;
            _vTouchDownPos = _vTouchMovePos = pntTouched;
            if(_bUserBorder)
                MoveTileToNearestPositionInBorderTo(_vTouchDownPos);
            _posGridTouch = GridPosFromScreenPosOrNull(_vTouchDownPos);
            if (_posGridTouch._bDefined && TileAt(_posGridTouch)._char != 0)
            {
                CheckForWordsCentredOn(_posGridTouch);
            }
        }

        public void OnTouchMove(PointF pntTouched)
        {
            if (_bBlobCollapsing)
                return;
            Vect2 vTouchMove = (Vect2)pntTouched - _vTouchMovePos;
            _vTouchMovePos = pntTouched;
            if (_bUserBorder)
                MoveTileToNearestPositionInBorderTo(_vTouchMovePos);
            _posGridTouch = GridPosFromScreenPosOrNull(_vTouchMovePos);
        }

        public void OnTouchUp(PointF pntTouched)
        {
            _vTouchUpPos = pntTouched;
            _posGridTouch._bDefined = false;
            ClearWords();
        }

        public void OnTouchClock()
        {

        }

        private void MoveTileToNearestPositionInBorderTo(Vect2 pos)
        {
            if (_iTileBeingDragged >= 0 && _iTileBeingDragged < _TilesMoving.Count)
            {
                Vect2 posTileCentre = _TilesMoving[_iTileBeingDragged].PosCentre;
                _TilesMoving[_iTileBeingDragged].PosCentre = NearestPositionInBorderTo(pos, posTileCentre, _TilesMoving[_iTileBeingDragged].Size, out _eCurrBorder);
                //_posGridNextDrop = GridPosFromBorderPos(_Tiles[_iTileBeingDragged].PosCentre, _eCurrBorder);
            }
        }

        // Given a tile position on the game space (usually from a touch), returns the most sensible
        // corresponding position around the edge of that space for the centre of a tile of the 
        // given size to be placed.
        // Takes into account the existing position of the tile.
        private Vect2 NearestPositionInBorderTo(Vect2 pos, Vect2 posCurr, float fTileSize, out EInBorder eWhichBorder)
        {
            eWhichBorder = EInBorder.Unknown;
            float f2 = fTileSize * 0.5f;
            float w = _frameRect.Width;
            float h = _frameRect.Height;
            double x = Math.Clamp(pos.X, f2, w - f2);
            double y = Math.Clamp(pos.Y, f2, h - f2);

            // Touching within the border, so ignore current position
            // Left border
            if (x < _fBorderWid)
            {
                eWhichBorder = EInBorder.Left;
                return new Vect2(f2, y);
            }
            // Right border
            if (x > w - _fBorderWid)
            {
                eWhichBorder = EInBorder.Right;
                return new Vect2(w - f2, y);
            }
            // Top border
            if (y < _fBorderWid)
            {
                eWhichBorder = EInBorder.Top;
                return new Vect2(x, f2);
            }
            // Bottom border
            if (y > h - _fBorderWid)
            {
                eWhichBorder = EInBorder.Bottom;
                return new Vect2(x, h - f2);
            }
            // Touching in the middle, not in the border, so take account of current position
            // Left border
            if (posCurr.X < _fBorderWid)
            {
                eWhichBorder = EInBorder.Left;
                return new Vect2(f2, y);
            }
            // Right border
            if (posCurr.X > w - _fBorderWid)
            {
                eWhichBorder = EInBorder.Right;
                return new Vect2(w - f2, y);
            }
            // Top border
            if (posCurr.Y < _fBorderWid)
            {
                eWhichBorder = EInBorder.Top;
                return new Vect2(x, f2);
            }
            // Bottom border
            //if (posCurr.Y > h - _fBorderWid)
            //{
            eWhichBorder = EInBorder.Bottom;
            return new Vect2(x, h - f2);
            //}
        }

        private Vect2 SnapToGrid(Vect2 pos, float fTileSize)
        {
            Vect2 posSnap = pos;
            float f2 = fTileSize * 0.5f;
            if (pos.X < f2)
                posSnap.X = f2;
            if (pos.X > _frameRect.Width - f2)
                posSnap.X = _frameRect.Width - f2;
            if (pos.Y < f2)
                posSnap.Y = f2;
            if (pos.Y > _frameRect.Height - f2)
                posSnap.Y = _frameRect.Height - f2;
            posSnap.X = pos.X - _frameRect.Width * 0.5f;
            return posSnap;
        }
    }
}
