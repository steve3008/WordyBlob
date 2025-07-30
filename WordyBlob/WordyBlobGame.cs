using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using System.Text;
//using Plugin.Maui.Audio;

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

    enum BlobMode
    {
        Blob,       // Tiles fall towards the centre of the screen
        Down        // Tiles fall towards the bottom of the screen
    }

    public class WordyBlobGame
    {
        public static readonly Random _Rnd = new Random();
        RectF _frameRect;
        bool _bRunning = true;
        const int _iThrobbingPeriod = 40;
        const int _iFramesPerAnimationFrame = 1;
        public static int _iFrameNum = 0, _iAnimatedFrameNum = 0;
        int _iCurrTime = 0; // Seconds
        // Background
        private Microsoft.Maui.Graphics.IImage? _BackgroundImage;
        int _iScore = 0, _iTopScore = 0;
        int _iTargetScore = 400;
        int _iTargetScoreNear;
        int _iFrameWhenTargetWasReached = -1;

        // Moving letter tiles
        List<TileMoving> _TilesMoving = new List<TileMoving>();
        // Which tile is currently being dragged around the border
        int _iTileBeingDragged = -1;
        EInBorder _eCurrBorder = EInBorder.Unknown;
        Vect2 _vecDraggedTileStartPos; // Starting position of the tile being dragged.

        // Other moving letter tiles
        const int _iMaxAnimatingTileFrame = 5;

        // Grid of letter tiles - the eponymous blob
        BlobMode _eBlobMode = BlobMode.Blob; // Defines the direction in which the letter tiles fall.
        const int _iGridRadius = 4; // When in blob mode, size of the grid.
        const int _iBlobRadius = 2; // Initial size of the blob of tiles.
        int _iShowingFoundWords = 0; // Non-zero indicates that found words are being shown so don't accept any user input.
        const int _iShowingFoundWordsDelay = 75;
        int _iTimeLeftToDropTile = 0; // Amount of time before a tile is dropped automatically.
        const int _iTileDropTimeLimitMax = 250; // Time after which a tile is added randomly, at the start of the game.
        const int _iTileDropTimeLimitMin = 75;
        const int _iTileDropTimeLimitStep = 5;
        int _iTileDropTimeLimit = _iTileDropTimeLimitMax; // Time after which a tile is added randomly. Gradually reduces through the game.
        Pos2 _posRndTileDropPos; // Randomly chosen place where a tile is dropped automatically after _iTileDropTimeLimit frames.
        const int _iMinWordLen = 4; // Minimum length of word so it can be removed.
        const int _iWordLenToScore50 = 7; // Minimum word length to score a 50 point bonus.
        bool _bBlobCollapsing = false; // Indicates that the collapsing animation is running so don't accept any user input.
        const int _iNumTileTypes = 26;
        private Microsoft.Maui.Graphics.IImage?[] _TileImages; // One for each letter
        const int _iTileGridWid = 9;
        const int _iTileGridHei = 9;
        GridCell[,] _gridCells, _gridCellsCollapsed, _gridCellsPrev;
        Pos2 _posGridTouch = new Pos2();

        // Colours used to hilight words of various lengths.
        Color[] _colWordHilites = { new Color(0, 64, 255), new Color(0, 0, 192), new Color(0, 192, 255), new Color(0, 255, 128), new Color(0, 0, 96), new Color(0, 0, 0) };

        // Hints
        const int _iTotalNumHints = 3;
        int _iNumHintsLeft = _iTotalNumHints;
        bool _bShowHint = false;
        Pos2 _posGridHint = new Pos2(-1, -1);
        private Microsoft.Maui.Graphics.IImage? _HintImage = null;
        private Microsoft.Maui.Graphics.IImage? _HintImageGrey = null;

        // Wordy hints
        private Microsoft.Maui.Graphics.IImage? _WordyHintImage = null;
        int _iFrameOfPrevTouch = -1; // Frame at which the most recent user interaction happened. -1 if none.
        int _iFrameOfPrevAddTile = -1; // Frame at which the most recent user addition of a tile happened. -1 if none.
        const int _iFramesOfNoActionBeforeWordyHint = 250; // Num frames to wait, with user doing nothing, before giving them a hint.
        const int _iFramesToShowWordyHint = 125; // Num frames to show the hint.

        // Intro screens
        const int _iNumIntroImages = 7;
        private Microsoft.Maui.Graphics.IImage?[] _IntroImages = new Microsoft.Maui.Graphics.IImage[_iNumIntroImages];
        int _iCurrIntroScreen = 0;

        // Other images
        private Microsoft.Maui.Graphics.IImage? _Bonus2ScoreCellImage = null;
        private Microsoft.Maui.Graphics.IImage? _Bonus3ScoreCellImage = null;
        private Microsoft.Maui.Graphics.IImage? _TargetReachedImage = null;
        private Microsoft.Maui.Graphics.IImage? _PausedImage = null;

        // Progress bar
        RectF _rectProgressBar, _rectProgressBarInner;

        char _cBestNextLetter = (char)0; // Suggested next letter.
        char _cPrevBestNextLetter = (char)0; // The letter which was suggested last time there was a successful hint. To avoid repeats.
        char _cPrevPrevBestNextLetter = (char)0; // The letter which was suggested the time before that. Same reason.
        char _cPrevPrevPrevBestNextLetter = (char)0; // And so on.

        // Flying scores
        const int _iNumScoreFlyTypes = 12;
        private Microsoft.Maui.Graphics.IImage?[] _ScoreFlyImages; // One for each score from 1 to 10 and 20 and 50.
        List<ScoreFly> _ScoreFlies = new List<ScoreFly>();

        // Tiles in the bag
        const int _iNumTilesTotal = 98;
        char[] _cTilesInBag = new char[_iNumTilesTotal];
        //                                              A  B  C  D  E   F  G  H  I  J  K  L  M  N  O  P  Q  R  S  T  U  V  W  X  Y  Z
        static readonly int[] _iLettersDistribution = { 9, 2, 2, 4, 12, 2, 3, 2, 9, 1, 1, 4, 2, 6, 8, 2, 1, 6, 4, 6, 4, 2, 2, 1, 2, 1 };
        static readonly int[] _iLettersScores       = { 1, 3, 3, 2, 1,  4, 2, 4, 1, 8, 5, 1, 3, 1, 1, 3,10, 1, 1, 1, 1, 4, 4, 8, 4,10 };
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

        // Audio
        //const int _iNumAudioPlayers = 1;
        //IAudioPlayer[] _AudioPlayers;

        public WordyBlobGame()
        {
            MainPage._Game = this;
            DrawFuncs.Initialize();
            _trieDict = new TrieDictionary();
            //int iPos = 0;
            //int iMaxLen = _trieDict.LongestWordInString("XGEEKX", ref iPos);// Test
            _gridCells = new GridCell[_iTileGridWid, _iTileGridHei];
            _gridCellsCollapsed = new GridCell[_iTileGridWid, _iTileGridHei];
            _gridCellsPrev = new GridCell[_iTileGridWid, _iTileGridHei];
            _TileImages = new Microsoft.Maui.Graphics.IImage[_iNumTileTypes];
            _ScoreFlyImages = new Microsoft.Maui.Graphics.IImage[_iNumScoreFlyTypes];
            LoadResources().Wait();
            //SetupAudioAsync().Wait();
            StartGame();
        }

        /*
        public async Task SetupAudioAsync()
        {
            _AudioPlayers = new IAudioPlayer[_iNumAudioPlayers];
            _AudioPlayers[0] = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("sound_touch.mp3"));
        }
        */
        public void StartGame()
        {
            _iFrameNum = 0;
            _iAnimatedFrameNum = 0;
            _iCurrTime = 0;
            _iScore = 0;
            _iTargetScore = 400;
            _iTargetScoreNear = _iTargetScore * 9 / 10;
            _iFrameWhenTargetWasReached = -1;
            _ScoreFlies.Clear();
            _TilesMoving.Clear();
            _iTileBeingDragged = -1;
            _eBlobMode = BlobMode.Blob;
            _iShowingFoundWords = 0;
            _iTimeLeftToDropTile = 0;
            _iTileDropTimeLimit = _iTileDropTimeLimitMax;
            _bBlobCollapsing = false;
            _iNumHintsLeft = _iTotalNumHints;
            _bShowHint = false;
            _posGridHint = new Pos2(-1, -1);
            _iFrameOfPrevTouch = -1;
            _iFrameOfPrevAddTile = -1;
            _cBestNextLetter = (char)0;
            _cPrevBestNextLetter = (char)0;
            _cPrevPrevBestNextLetter = (char)0;
            _cPrevPrevPrevBestNextLetter = (char)0;
            CreateShuffledLetterTileBag();
            Initialize();
            if (MainPage._This != null)
            {
                MainPage._This.UpdateTheClockNextFrame();
                MainPage._This.UpdateTheScoreNextFrame();
            }
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

        private char BestNextLetterOrNextTileFromBag()
        {
            FindBestMissingLetter();
            _bShowHint = false;
            if(MainPage._This != null)
                MainPage._This.EnableHintButton(_iNumHintsLeft > 0 && _posGridHint._bDefined);
            if (_cBestNextLetter > 0)
                return _cBestNextLetter;
            return NextTileFromBag();
        }

        private async Task LoadResources()
        {
            _BackgroundImage = await LoadImageFromRes("background.png");
            for (int i = 0; i < _iNumTileTypes; i++)
            {
                _TileImages[i] = await LoadImageFromRes("tile_" + (char)((char)i + 'a') + ".png");
            }
            for (int i = 0; i < _iNumScoreFlyTypes; i++)
            {
                _ScoreFlyImages[i] = await LoadImageFromRes("score_fly_" + (i+1).ToString() + ".png");
            }
            _HintImage = await LoadImageFromRes("bulb.png");
            _HintImageGrey = await LoadImageFromRes("bulb_grey.png");
            _Bonus2ScoreCellImage = await LoadImageFromRes("bonus2_score_cell.png");
            _Bonus3ScoreCellImage = await LoadImageFromRes("bonus3_score_cell.png");
            _TargetReachedImage = await LoadImageFromRes("target_reached.png");
            _WordyHintImage = await LoadImageFromRes("wordy_hint_1.png");
            _PausedImage = await LoadImageFromRes("pause.png");
            for(int i = 0; i < _iNumIntroImages; i++)
                _IntroImages[i] = await LoadImageFromRes("intro_" + (i+1).ToString() + ".jpg");
        }

        private void Initialize()
        {
            if (_eBlobMode == BlobMode.Blob)
            {
                ClearGrid(EGridCellType.Invalid);
                CreateABlob(_iGridRadius, false);
                CreateSpecialCellsForBlob(_iGridRadius);
                CreateABlob(_iBlobRadius, true);
            }
            else
            {
                ClearGrid(EGridCellType.Plain);
                CreateABlob(_iBlobRadius, true);
            }
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
            LoadSettingsFromFile();
        }

        // Create a circular blob of tiles or valid cells.
        private void CreateABlob(int radius, bool bTiles)
        {
            int xCentre = _iTileGridWid / 2;
            int yCentre = _iTileGridHei / 2;
            for (int yr = -radius; yr <= radius; yr++)
            {
                int xStart = (int)Math.Sqrt((double)(radius * radius - yr * yr));
                for(int xr = -xStart; xr <= xStart; xr++)
                {
                    int x = xCentre + xr;
                    int y = yCentre + yr;
                    if (x >= 0 && y >= 0 && x < _iTileGridWid && y < _iTileGridHei)
                    {
                        if (bTiles)
                        {
                            _gridCells[x, y] = new GridCell(NextTileFromBag(), false, EGridCellType.Plain);
                        }
                        else
                        {
                            _gridCells[x, y]._eType = EGridCellType.Plain;
                        }
                    }
                }
            }
        }

        // Add some "bonus score" cells.
        private void CreateSpecialCellsForBlob(int radius)
        {
            int xCentre = _iTileGridWid / 2;
            int yCentre = _iTileGridHei / 2;
            int r2 = radius - 2;
            _gridCells[xCentre - r2, yCentre - r2]._eType = EGridCellType.BonusX2;
            _gridCells[xCentre + r2, yCentre - r2]._eType = EGridCellType.BonusX2;
            _gridCells[xCentre + r2, yCentre + r2]._eType = EGridCellType.BonusX2;
            _gridCells[xCentre - r2, yCentre + r2]._eType = EGridCellType.BonusX2;
            _gridCells[xCentre, yCentre - radius]._eType = EGridCellType.BonusX3;
            _gridCells[xCentre, yCentre + radius]._eType = EGridCellType.BonusX3;
            _gridCells[xCentre - radius, yCentre]._eType = EGridCellType.BonusX3;
            _gridCells[xCentre + radius, yCentre]._eType = EGridCellType.BonusX3;
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
                _gridCells[x, y] = new GridCell((char)(count + 'A'), false, EGridCellType.Plain);
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
                _iTopScore = IntFromFileStream(sr, _iTopScore);
                sr.Close();
            }
        }

        private void SaveSettingsToFile()
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.dat");
            using (StreamWriter sw = new StreamWriter(filePath, append: false))
            {
                sw.WriteLine(_iTopScore.ToString());
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
            set
            {
                if (_iScore != value)
                {
                    _iScore = value;
                    UpdateProgressBar();
                }
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
                _vecDraggedTileStartPos = new Vect2(_frameRect.Width * 0.5f, _fTileSizeBig * 0.5f);
                _TilesMoving.Add(new TileMoving(BestNextLetterOrNextTileFromBag(), _fTileSizeBig, true, _vecDraggedTileStartPos));

                _rectProgressBar = new RectF(_frameRect.Left + _frameRect.Width * 0.1f, _frameRect.Bottom - _frameRect.Height * 0.08f,
                                             _frameRect.Width * 0.8f, _frameRect.Height * 0.04f);
            }

            if (_iCurrIntroScreen >= 0 && _IntroImages[_iCurrIntroScreen] != null)
            {
                canvas.DrawImage(_IntroImages[_iCurrIntroScreen], 0, 0, _frameRect.Width, _frameRect.Height);
                return;
            }

            canvas.DrawImage(_BackgroundImage, 0, 0, _frameRect.Width, _frameRect.Height);
            if (_eBlobMode == BlobMode.Blob)
            {
                DrawBackgroundGridRectangles(canvas);
            }
            else
            {
                DrawBackgroundGridLines(canvas);
            }
            DrawPotentialDropLocationsInTilesGrid(canvas);
            DrawTouchLocationInTilesGrid(canvas);
            DrawGridOfTiles(canvas, _frameRect);
            ShowWordsInGrid(canvas);
            ShowHintInGrid(canvas);
            DrawHintImages(canvas);
            DrawProgressBar(canvas);

            foreach (TileMoving tile in _TilesMoving)
            {
                tile.Draw(canvas);
            }

            foreach (ScoreFly score in _ScoreFlies)
            {
                score.Draw(canvas);
            }

            // If the game has been going for a while but the user hasn't added a tile, maybe they're not sure what to do,
            // so give them a wordy hint.
            if(_iFrameOfPrevAddTile == -1 && _iFrameNum >= _iFramesOfNoActionBeforeWordyHint && 
                _iFrameNum < _iFramesOfNoActionBeforeWordyHint + _iFramesToShowWordyHint)
            {
                DrawImageOriginalSizeAt(canvas, _WordyHintImage, 60.0f, 310.0f);
            }

            // If we're paused.
            if (!_bRunning)
            {
                canvas.FillColor = Color.FromRgba(192, 208, 255, 192);
                canvas.FillRectangle(frameRect);
                DrawImageOriginalSizeAt(canvas, _PausedImage, 250.0f, 840.0f);
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

        private void DrawImageOriginalSizeAt(ICanvas canvas, Microsoft.Maui.Graphics.IImage? img, float x, float y)
        {
            if (img == null)
                return;
            float wid = img.Width * _frameRect.Width / 1080.0f;
            float hei = img.Height * _frameRect.Height / 1980.0f;
            float xScreen = x * _frameRect.Width / 1080.0f;
            float yScreen = y * _frameRect.Height / 1980.0f;
            canvas.DrawImage(img, xScreen, yScreen, wid, hei);
        }

        // Draw the grid as a set of rectangles and fill in the background to the grid.
        // Used when some cells are not valid so the grid is not necessarily rectangular.
        private void DrawBackgroundGridRectangles(ICanvas canvas)
        {
            SizeF szReduce = new SizeF(-1);
            EGridCellType eCellType;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    if (_gridCells[x, y]._eType != EGridCellType.Invalid)
                    {
                        RectF rect = ScreenRectFromGridPos(new Pos2(x, y));
                        canvas.FillColor = Colors.White;
                        canvas.FillRectangle(rect);
                        eCellType = _gridCells[x, y]._eType;
                        if (eCellType == EGridCellType.Plain)
                        {
                            canvas.StrokeColor = new Color(0, 0, 0, 32);
                            canvas.DrawRectangle(rect.Inflate(szReduce));
                        }
                        else if(eCellType == EGridCellType.BonusX2 && _Bonus2ScoreCellImage != null)
                        {
                            canvas.DrawImage(_Bonus2ScoreCellImage, rect.X, rect.Y, rect.Width, rect.Height);
                        }
                        else if (eCellType == EGridCellType.BonusX3 && _Bonus3ScoreCellImage != null)
                        {
                            canvas.DrawImage(_Bonus3ScoreCellImage, rect.X, rect.Y, rect.Width, rect.Height);
                        }
                        // Draw a solid outline around the outer edge of the valid cells.
                        canvas.StrokeColor = new Color(0, 0, 0, 255);
                        if (x == 0 || _gridCells[x-1, y]._eType == EGridCellType.Invalid)
                            canvas.DrawLine(rect.Left, rect.Top, rect.Left, rect.Bottom);
                        if (y == 0 || _gridCells[x, y - 1]._eType == EGridCellType.Invalid)
                            canvas.DrawLine(rect.Left, rect.Top, rect.Right, rect.Top);
                        if (x == _iTileGridWid - 1 || _gridCells[x + 1, y]._eType == EGridCellType.Invalid)
                            canvas.DrawLine(rect.Right, rect.Top, rect.Right, rect.Bottom);
                        if (y == _iTileGridHei - 1 || _gridCells[x, y + 1]._eType == EGridCellType.Invalid)
                            canvas.DrawLine(rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                    }
                }
            }
            // Highlight the centre cell.
            if (_iTileGridWid % 2 != 0 && _iTileGridHei % 2 != 0)
            {
                int xCentre = _iTileGridWid / 2;
                int yCentre = _iTileGridHei / 2;
                canvas.StrokeColor = new Color(0, 0, 0, 255);
                canvas.DrawRectangle(ScreenRectFromGridPos(new Pos2(xCentre, yCentre)));
            }
        }

        // Highlight the cell where the hint says the next tile should be placed.
        private void ShowHintInGrid(ICanvas canvas)
        {
            if (_bShowHint && _posGridHint._bDefined && _posGridHint.X >= 0 && _posGridHint.Y >= 0 && _posGridHint.X < _iTileGridWid && _posGridHint.Y < _iTileGridHei)
            {
                canvas.StrokeColor = new Color(255, 255, 0);
                canvas.StrokeSize = _fTileSizeGrid * 0.1f;
                RectF rectHintCell = ScreenRectFromGridPos(_posGridHint);
                canvas.DrawRectangle(rectHintCell);
                canvas.StrokeSize = 1.0f;
                if (_HintImage != null)
                {
                    float heiHintImage = rectHintCell.Height * 0.8f;
                    float widHintImage = heiHintImage * _HintImage.Width / _HintImage.Height;
                    canvas.DrawImage(_HintImage, rectHintCell.Left + (rectHintCell.Width - widHintImage) * 0.5f, 
                                                 rectHintCell.Top + (rectHintCell.Height - heiHintImage) * 0.5f, 
                                                 widHintImage, heiHintImage);
                }
            }
        }

        // Draw the grid lines and fill in the background to the grid.
        // Used when all cells are valid so the grid is rectangular.
        private void DrawBackgroundGridLines(ICanvas canvas)
        {
            if (_bUserBorder)
            {
                float b2 = _fBorderWid * 2.0f;
                canvas.FillColor = new Color(208, 255, 192);
                canvas.FillRectangle(_fBorderWid, _fBorderWid, _frameRect.Width - b2, _frameRect.Height - b2);
                canvas.DrawRectangle(_fBorderWid, _fBorderWid, _frameRect.Width - b2, _frameRect.Height - b2);
            }

            float fSpace = _fTileSizeGrid * _fTileStrideRel; // Size of each cell
            float xGridLeft = (_frameRect.Width - (float)_iTileGridWid * fSpace) * 0.5f;
            float xGridRight = xGridLeft + (float)_iTileGridWid * fSpace;
            float yGridTop = (_frameRect.Height - (float)_iTileGridHei * fSpace) * 0.5f;
            float yGridBottom = yGridTop + (float)_iTileGridHei * fSpace;
            float xGrid = xGridLeft;
            int xCentre = _iTileGridWid / 2;
            // Draw vertical lines.
            for (int x = 0; x <= _iTileGridWid; x++)
            {
                canvas.StrokeColor = (x == xCentre && _iTileGridWid % 2 == 0) ? new Color(0, 0, 0, 255) : new Color(0, 0, 0, 32);
                canvas.DrawLine(xGrid, yGridTop, xGrid, yGridBottom);
                xGrid += fSpace;
            }
            float yGrid = yGridTop;
            int yCentre = _iTileGridHei / 2;
            // Draw horizontal lines.
            for (int y = 0; y <= _iTileGridHei; y++)
            {
                canvas.StrokeColor = (y == yCentre && _iTileGridHei % 2 == 0) ? new Color(0, 0, 0, 255) : new Color(0, 0, 0, 32);
                canvas.DrawLine(xGridLeft, yGrid, xGridRight, yGrid);
                yGrid += fSpace;
            }
            // Highlight the centre cell.
            if(_iTileGridWid % 2 != 0 && _iTileGridHei % 2 != 0)
            {
                canvas.StrokeColor = new Color(0, 0, 0, 255);
                canvas.DrawRectangle(ScreenRectFromGridPos(new Pos2(xCentre, yCentre)));
            }
            // Highlight the cell where the hint says the next tile should be placed.
            if(_bShowHint && _posGridHint._bDefined && _posGridHint.X >= 0 && _posGridHint.Y >= 0 && _posGridHint.X < _iTileGridWid && _posGridHint.Y < _iTileGridHei)
            {
                canvas.StrokeColor = new Color(255, 255, 0);
                canvas.StrokeSize = _fTileSizeGrid * 0.1f;
                canvas.DrawRectangle(ScreenRectFromGridPos(_posGridHint));
                canvas.StrokeSize = 1.0f;
            }
        }

        // Show all the words that are currently selected in the grid.
        private void ShowWordsInGrid(ICanvas canvas)
        {
            int iLen;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    iLen = _gridCells[x, y]._iInWordOfLen - _iMinWordLen;
                    if ( iLen >= 0 && iLen < 6)
                    {
                        canvas.StrokeColor = _colWordHilites[iLen];
                        canvas.StrokeSize = _fTileSizeGrid * (0.1f + (float)iLen * 0.01f);
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
            Color colEdge = DrawFuncs.ColorInterpolatedSinusoidal(new Color(0, 128, 0, 64), new Color(0, 64, 192), 
                                                                  _iAnimatedFrameNum % _iThrobbingPeriod, _iThrobbingPeriod);
            canvas.StrokeSize = _fTileSizeGrid * 0.08f;
            float fCornerRadius = _fTileSizeGrid * 4.0f * (1.0f - _fTileStrideRel);
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    if(_gridCells[x, y]._bOnEdgeOfBlob)
                    {
                        RectF rect = ScreenRectFromGridPos(new Pos2(x, y));
                        canvas.StrokeColor = (x == _posRndTileDropPos.X && y == _posRndTileDropPos.Y) ? new Color(0, 255, 0) : colEdge;
                        canvas.DrawRoundedRectangle(rect, fCornerRadius);
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

        private void DrawHintImages(ICanvas canvas)
        {
            if (_HintImage != null && _HintImageGrey != null)
            {
                float wid = _frameRect.Width * 0.08f;
                float hei = wid * _HintImage.Height / _HintImage.Width;
                float x = _frameRect.Width * 0.9f;
                float xStep = wid * 1.1f;
                float y = _frameRect.Height * 0.012f;
                for (int i = 0; i < _iTotalNumHints; i++)
                {
                    canvas.DrawImage(i < _iNumHintsLeft ? _HintImage : _HintImageGrey, x, y, wid, hei);
                    x -= xStep;
                }
            }
        }

        private void DrawProgressBar(ICanvas canvas)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(_rectProgressBar);
            canvas.StrokeSize = _rectProgressBar.Height * 0.05f;
            canvas.StrokeColor = Colors.DarkGreen;
            canvas.DrawRectangle(_rectProgressBar);
            if (Score >= _iTargetScore)
            {
                canvas.FillColor = Colors.Yellow;
                if (_iFrameWhenTargetWasReached == -1)
                    _iFrameWhenTargetWasReached = _iAnimatedFrameNum;
            }
            else if (Score < _iTargetScoreNear)
            {
                canvas.FillColor = Colors.Green;
            }
            else
            {
                int i = _iThrobbingPeriod / 2;
                canvas.FillColor = DrawFuncs.ColorInterpolatedSinusoidal(Colors.Green, Colors.Yellow, _iAnimatedFrameNum % i, i);
            }
            canvas.FillRectangle(_rectProgressBarInner);
            if (Score >= _iTargetScore && _iAnimatedFrameNum < _iFrameWhenTargetWasReached + 100 && _TargetReachedImage != null && _iAnimatedFrameNum % 20 < 10)
            {
                canvas.DrawImage(_TargetReachedImage, _rectProgressBar.Left, _rectProgressBar.Top,
                                 _rectProgressBar.Width, _rectProgressBar.Width * _TargetReachedImage.Height / _TargetReachedImage.Width);
            }
            canvas.StrokeSize = 1.0f;
        }
        private void UpdateProgressBar()
        {
            float fBorder = _rectProgressBar.Height * 0.1f;
            _rectProgressBarInner = _rectProgressBar.Inflate(-fBorder, -fBorder);
            if (Score < _iTargetScore)
            {
                _rectProgressBarInner.Width *= (float)Score / (float)_iTargetScore;
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
            return  posGrid.X >= 0 && posGrid.Y >= 0 && posGrid.X < _iTileGridWid && posGrid.Y < _iTileGridHei 
                    && _gridCells[posGrid.X, posGrid.Y]._eType != EGridCellType.Invalid;
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

        public Microsoft.Maui.Graphics.IImage? ScoreFlyImageForScore(int s)
        {
            // Special scores.
            if (s == 20) s = 11;
            if (s == 50) s = 12;
            // Get image.
            if (s < 1 || s > _iNumScoreFlyTypes)
                return null;
            return _ScoreFlyImages[s - 1];
        }

        // Randomly pick one of the positions that's labelled as an edge.
        private Pos2 FindARandomEdge()
        {
            int count = 0;
            Pos2 pos = Pos2.NULL;
            do
            {
                pos = new Pos2(_Rnd.Next(_iTileGridWid), _Rnd.Next(_iTileGridHei));
                count++;
            }
            while (count < 1000 && !_gridCells[pos.X, pos.Y]._bOnEdgeOfBlob);
            return pos;
        }

        // Mark all grid cells around the edge of the blob as potential tile drop locations.
        // Done in such a way that it keeps its blob shape, with no overhangs.
        // Returns the number of potential drop locations found.
        private int FindEdgesOfBlob()
        {
            int iNumFound = 0;
            int xCentre = _iTileGridWid / 2;
            int yCentre = _iTileGridHei / 2;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    /*
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
                    */
                    // If the centre cell is empty it will always be marked as available to be dropped into.
                    if ((x == xCentre && y == yCentre) && _gridCells[x, y]._char == 0)
                    {
                        _gridCells[x, y]._bOnEdgeOfBlob = true;
                    }
                    else // Otherwise we check if this cell is empty but it has at least 1 neighbour with a tile in it.
                    {
                        _gridCells[x, y]._bOnEdgeOfBlob = ((x > 0 && _gridCells[x - 1, y]._char != 0) ||
                                                           (y > 0 && _gridCells[x, y - 1]._char != 0) ||
                                                           (x < _iTileGridWid - 1 && _gridCells[x + 1, y]._char != 0) ||
                                                           (y < _iTileGridHei - 1 && _gridCells[x, y + 1]._char != 0)) &&
                                                           _gridCells[x, y]._char == 0 && _gridCells[x, y]._eType != EGridCellType.Invalid;
                    }
                    if(_gridCells[x, y]._bOnEdgeOfBlob)
                        iNumFound++;
                }
            }
            return iNumFound;
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
            if (!_bRunning || _bShowHint || _iCurrIntroScreen >= 0)
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

            int iCurrTime = 60 - _iTimeLeftToDropTile * 60 / _iTileDropTimeLimit;
            if(iCurrTime != _iCurrTime)
            {
                _iCurrTime = iCurrTime;
                MainPage._This.UpdateTheClockNextFrame();
            }

            _iAnimatedFrameNum++;
        }

        private void IncrementFrame()
        {
            foreach (TileMoving tile in _TilesMoving)
            {
                tile.Increment();
            }

            // If there are any ScoreFlies, fly them towards the scoreboard.
            int num = _ScoreFlies.Count;
            for (int i = 0; i < num; i++)
            {
                ScoreFly score = _ScoreFlies[i];
                score.Increment();
                if(score.CanDelete)
                {
                    if (MainPage._This != null)
                    {
                        Score += score.Score;
                        MainPage._This.UpdateTheScoreNextFrame();
                    }
                    _ScoreFlies.RemoveAt(i);
                    i--;
                    num--;
                }
            }

            // If we're highlighting words that are about to be removed.
            if (_iShowingFoundWords > 0)
            {
                _iShowingFoundWords--;
                // If we've finished highlighting words that are about to be removed, remove them.
                if (_iShowingFoundWords == 0)
                {
                    RemoveAllFoundWords();
                    CollapseBlob();
                }
            }
            // If we've got more than 1 animating tile, we must be running a tile animation.
            else if (_bBlobCollapsing && _TilesMoving.Count > 1)
            {
                // If that tile animation has just finished, flag that collapsing has finished.
                if (_TilesMoving[1].CanDeleteNow)
                {
                    _bBlobCollapsing = false;
                    _TilesMoving.RemoveRange(1, _TilesMoving.Count - 1);
                    CopyGrid(_gridCellsCollapsed, _gridCells);
                    FindEdgesOfBlob();
                    CollapseBlob();
                    if(!_bBlobCollapsing)
                        CheckForWordsInWholeGrid();
                }
            }
            else
            {
                // If we're about to start dropping a tile, create the moving tile that will fly towards the target cell.
                if (_iTimeLeftToDropTile == 0 && _iTileBeingDragged == 0)
                {
                    _posRndTileDropPos = FindARandomEdge();
                    _TilesMoving[_iTileBeingDragged] = new TileMoving(BestNextLetterOrNextTileFromBag(), _fTileSizeBig, true, _vecDraggedTileStartPos);
                }
                _iTimeLeftToDropTile++;
                // If we're less than 3/4 of the way through the time taken to drop a tile, leave the tile-to-be-dropped in its start position.
                // It will start moving when the time reaches iTileDropStartMovingTime.
                int iTileDropStartMovingTime = _iTileDropTimeLimit * 3 / 4;
                if (_iTimeLeftToDropTile < iTileDropStartMovingTime && _iTileBeingDragged == 0)
                {
                    _TilesMoving[_iTileBeingDragged].PosCentre = _vecDraggedTileStartPos;
                }
                // Otherwise, we've reached the start moving time but not the time limit, so the moving tile (the tile-to-be-dropped) position
                // is interpolated between its start position and the intended cell position.
                else if (_iTimeLeftToDropTile < _iTileDropTimeLimit)
                {
                    if (_iTileBeingDragged == 0 && _posRndTileDropPos._bDefined)
                    {
                        Vect2 vRndTileDropPos = ScreenPosFromGridPos(_posRndTileDropPos);
                        int tend = _iTileDropTimeLimit - iTileDropStartMovingTime; // Length of the tile moving period.
                        int t = _iTimeLeftToDropTile - iTileDropStartMovingTime; // Current time within the tile moving period.
                        // Sinusoidal interpolation (using 90 to 270 degrees of an inverted and scaled sine wave):
                        t = 128 - DrawFuncs._SinesInt[90 + t * 180 / tend];
                        _TilesMoving[_iTileBeingDragged].PosCentre = _vecDraggedTileStartPos + (t * (vRndTileDropPos - _vecDraggedTileStartPos) / 256);
                    }
                }
                // Otherwise, we've reached the time limit, so the tile should have reached the intended cell position. So we'll place the tile
                // in the intended cell position.
                else
                {
                    if (_posRndTileDropPos._bDefined)
                    {
                        DropATileAt(_posRndTileDropPos);
                    }
                }
            }
            _iFrameNum++;
        }


        // Remove all tiles from the grid, and set their validity.

        private void ClearGrid(EGridCellType eType)
        {
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    _gridCells[x, y].RemoveTile(eType);
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
            if (_iShowingFoundWords > 0)
                return;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    _gridCells[x, y]._iInWordOfLen = 0;
                }
            }
        }

        // Use the dictionary to look for valid horizontal or vertical words in the whole grid with the first and last letter missing,
        // where the missing letter is in the edge region.
        private void FindBestMissingLetter()
        {
            _posGridHint = Pos2.NULL;
            _cPrevPrevPrevBestNextLetter = _cPrevPrevBestNextLetter;
            _cPrevPrevBestNextLetter = _cPrevBestNextLetter;
            _cPrevBestNextLetter = _cBestNextLetter;
            _cBestNextLetter = (char)0;
            char cNext = (char)0;
            char cPrev = (char)0;
            StringBuilder s = new StringBuilder();

            // Check for horizontal words.
            int x, x1, x2, y, xStart, yStart;
            for (y = 0; y < _iTileGridHei; y++)
            {
                for (x1 = 0; x1 < _iTileGridWid - _iMinWordLen + 2; x1++)
                {
                    // Starting at the first letter tile in each row of cells.
                    if (_gridCells[x1, y]._char != 0)
                    {
                        xStart = x1;
                        // Find the last letter tile in that row and point x2 to the next cell.
                        for (x2 = x1 + 1; x2 < _iTileGridWid && _gridCells[x2, y]._char != 0; x2++) ;
                        // See if the cell after that last letter tile is on the edge.
                        if(x2 < _iTileGridWid && _gridCells[x2, y]._bOnEdgeOfBlob)
                        {
                            // If it is, gradually shorten the string and look for words that begin with that string.
                            while ((x2 - x1) >= _iMinWordLen - 1)
                            {
                                s.Clear();
                                // Get the string for that contiguous horizontal run of tiles.
                                for (x = x1; x < x2; x++)
                                    s.Append(_gridCells[x, y]._char);
                                cNext = _trieDict.FindRandomWordWithPrefixAndOneMoreLetterAtEnd(s.ToString());
                                if (cNext > 0)
                                {
                                    if (cNext != _cPrevBestNextLetter && cNext != _cPrevPrevBestNextLetter && cNext != _cPrevPrevPrevBestNextLetter)
                                    {
                                        _posGridHint = new Pos2(x2, y);
                                        _cBestNextLetter = cNext;
                                        return;
                                    }
                                }
                                x1++;
                            }
                        }
                        // See if the cell before the first letter tile is on the edge.
                        x1 = xStart;
                        if(x1 > 0 && (x2 - x1) >= (_iMinWordLen - 1) && _gridCells[x1 - 1, y]._bOnEdgeOfBlob)
                        {
                            s.Clear();
                            // Get the string for that contiguous horizontal run of tiles.
                            for (x = x1; x < x2; x++)
                                s.Append(_gridCells[x, y]._char);
                            cPrev = _trieDict.FindRandomWordWithSuffixAndOneMoreLetterAtStart(s.ToString());
                            if (cPrev > 0)
                            {
                                if (cPrev != _cPrevBestNextLetter && cNext != _cPrevPrevBestNextLetter && cNext != _cPrevPrevPrevBestNextLetter)
                                {
                                    _posGridHint = new Pos2(x1 - 1, y);
                                    _cBestNextLetter = cPrev;
                                    return;
                                }
                            }
                        }
                        // Move on beyond that string.
                        x1 = x2;
                    }
                }
            }


            // Check for vertical words.
            int y1, y2;
            for (x = 0; x < _iTileGridWid; x++)
            {
                for (y1 = 0; y1 < _iTileGridHei - _iMinWordLen + 2; y1++)
                {
                    // Starting at the first letter tile in each column of cells.
                    if (_gridCells[x, y1]._char != 0)
                    {
                        yStart = y1;
                        // Find the last letter tile in that column and point y2 to the next cell.
                        for (y2 = y1 + 1; y2 < _iTileGridHei && _gridCells[x, y2]._char != 0; y2++) ;
                        // See if the cell after that last letter tile is on the edge.
                        if (y2 < _iTileGridHei && _gridCells[x, y2]._bOnEdgeOfBlob)
                        {
                            // If it is, gradually shorten the string and look for words that begin with that string.
                            while ((y2 - y1) >= _iMinWordLen - 1)
                            {
                                s.Clear();
                                // Get the string for that contiguous vertical run of tiles.
                                for (y = y1; y < y2; y++)
                                    s.Append(_gridCells[x, y]._char);
                                cNext = _trieDict.FindRandomWordWithPrefixAndOneMoreLetterAtEnd(s.ToString());
                                if (cNext > 0)
                                {
                                    if (cNext != _cPrevBestNextLetter && cNext != _cPrevPrevBestNextLetter && cNext != _cPrevPrevPrevBestNextLetter)
                                    {
                                        _posGridHint = new Pos2(x, y2);
                                        _cBestNextLetter = cNext;
                                        return;
                                    }
                                }
                                y1++;
                            }
                        }
                        // See if the cell before the first letter tile is on the edge.
                        y1 = yStart;
                        if (y1 > 0 && (y2 - y1) >= (_iMinWordLen - 1) && _gridCells[x, y1 - 1]._bOnEdgeOfBlob)
                        {
                            s.Clear();
                            // Get the string for that contiguous vertical run of tiles.
                            for (y = y1; y < y2; y++)
                                s.Append(_gridCells[x, y]._char);
                            cPrev = _trieDict.FindRandomWordWithSuffixAndOneMoreLetterAtStart(s.ToString());
                            if (cPrev > 0)
                            {
                                if (cPrev != _cPrevBestNextLetter && cNext != _cPrevPrevBestNextLetter && cNext != _cPrevPrevPrevBestNextLetter)
                                {
                                    _posGridHint = new Pos2(x, y1 - 1);
                                    _cBestNextLetter = cPrev;
                                    return;
                                }
                            }
                        }
                        // Move on beyond that string.
                        y1 = y2;
                    }
                }
            }
        }

        // Use the dictionary to look for valid words in the whole grid.
        // Returns the number of words found.
        private int CheckForWordsInWholeGrid()
        {
            ClearWords();
            int iWordNum = 0, iPos = 0, iLen;
            StringBuilder s = new StringBuilder();

            // Check for horizontal words.
            int x, x1, x2, y, start, end;
            for (y = 0; y < _iTileGridHei; y++)
            {
                for (x1 = 0; x1 < _iTileGridWid - 1; x1++)
                {
                    // Starting at the first letter tile in each row of cells.
                    if(_gridCells[x1, y]._char != 0)
                    {
                        // Find the last tile in that row.
                        for (x2 = x1 + 1; x2 < _iTileGridWid && _gridCells[x2, y]._char != 0; x2++);
                        s.Clear();
                        // Get the string for that contiguous horizontal run of tiles.
                        for (x = x1; x < x2; x++)
                            s.Append(_gridCells[x, y]._char);
                        // If there are any words in that string, mark the longest string as having been found.
                        iLen = _trieDict.LongestWordInString(s.ToString(), ref iPos);
                        if (iLen >= _iMinWordLen)
                        {
                            //string word = s.ToString().Substring(iPos, iLen);
                            iWordNum++;
                            start = x1 + iPos;
                            end = start + iLen;
                            for (x = start; x < end; x++)
                            {
                                if (_gridCells[x, y]._iInWordOfLen < iLen)
                                    _gridCells[x, y]._iInWordOfLen = iLen;
                            }
                        }
                        // Move on beyond that string.
                        x1 = x2 - 1;
                    }
                }
            }

            // Check for vertical words.
            int y1, y2;
            for (x = 0; x < _iTileGridWid; x++)
            {
                for (y1 = 0; y1 < _iTileGridHei - 1; y1++)
                {
                    // Starting at the first tile in each column of cells.
                    if (_gridCells[x, y1]._char != 0)
                    {
                        // Find the last tile in that column.
                        for (y2 = y1 + 1; y2 < _iTileGridHei && _gridCells[x, y2]._char != 0; y2++);
                        s.Clear();
                        // Get the string for that contiguous vertical run of tiles.
                        for (y = y1; y < y2; y++)
                            s.Append(_gridCells[x, y]._char);
                        // If there are any words in that string, mark the longest string as having been found.
                        iLen = _trieDict.LongestWordInString(s.ToString(), ref iPos);
                        if (iLen >= _iMinWordLen)
                        {
                            //string word = s.ToString().Substring(iPos, iLen);
                            iWordNum++;
                            start = y1 + iPos;
                            end = start + iLen;
                            for (y = start; y < end; y++)
                            {
                                if(_gridCells[x, y]._iInWordOfLen < iLen)
                                    _gridCells[x, y]._iInWordOfLen = iLen;
                            }
                        }
                        // Move on beyond that string.
                        y1 = y2 - 1;
                    }
                }
            }

            if (iWordNum > 0)
                _iShowingFoundWords = _iShowingFoundWordsDelay;
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
                    if (x2 - x1 > _iMinWordLen)
                    {
                        StringBuilder s = new StringBuilder();
                        for (int x = x1; x <= x2; x++)
                            s.Append(_gridCells[x, posGridCentre.Y]._char);
                        if(_trieDict.SearchForWord(s.ToString()))
                        {
                            iWordNum++;
                            for (int x = x1; x <= x2; x++)
                                _gridCells[x, posGridCentre.Y]._iInWordOfLen = iWordNum;
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
                                _gridCells[posGridCentre.X, y]._iInWordOfLen = iWordNum;
                        }
                    }

                }
            }
            return iWordNum;
        }

        // After calling CheckForWords(), remove all the words that were found.
        private void RemoveAllFoundWords()
        {
            bool bFound7LetterWord = false;
            int iWordLen;
            int iNumTilesLeft = 0;
            int iDelay = 1;
            Vect2 vEndPosAtScoreboard = new Vect2(_frameRect.Width * 165.0 / 1080.0, 0);
            double fRndVar = _frameRect.Width * 0.06;
            for (int y = 0; y < _iTileGridHei; y++)
            {
                for (int x = 0; x < _iTileGridWid; x++)
                {
                    if (_gridCells[x, y]._char >= 'A')
                    {
                        iNumTilesLeft++;
                        iWordLen = _gridCells[x, y]._iInWordOfLen;
                        if (iWordLen != 0)
                        {
                            // Start a ScoreFly from this position.
                            int score = _iLettersScores[_gridCells[x, y]._char - 'A'];
                            int iScoreMultiple = _gridCells[x, y].ScoreMultiple;
                            for (int i = 0; i < iScoreMultiple; i++)
                            {
                                _ScoreFlies.Add(new ScoreFly(score, ScreenPosFromGridPos(new Pos2(x, y)).RandomVariationAroundThisPoint(fRndVar),
                                                vEndPosAtScoreboard, iDelay));
                                iDelay++;
                            }
                            _gridCells[x, y].RemoveTile(EGridCellType.SameAsBefore);
                            iNumTilesLeft--;

                            // If this is the first letter in a 7 or more letter word, give a 50 point bonus.
                            if (iWordLen >= _iWordLenToScore50 && !bFound7LetterWord)
                            {
                                _ScoreFlies.Add(new ScoreFly(50, ScreenPosFromGridPos(new Pos2(x, y)).RandomVariationAroundThisPoint(fRndVar),
                                                vEndPosAtScoreboard, 0));
                                bFound7LetterWord = true;
                            }
                        }
                    }
                }
            }

            // If the end result of this is no tiles left, give a 20 point bonus.
            if(iNumTilesLeft <= 0)
            {
                _ScoreFlies.Add(new ScoreFly(20, ScreenPosFromGridPos(new Pos2(_iTileGridWid / 2, _iTileGridHei / 2)).RandomVariationAroundThisPoint(fRndVar),
                                vEndPosAtScoreboard, 0));
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
                            _gridCellsCollapsed[xCentre + x1, y]._char = _gridCellsCollapsed[x, y]._char;
                            _gridCells[x, y].RemoveTile(EGridCellType.SameAsBefore);
                            _gridCellsCollapsed[x, y].RemoveTile(EGridCellType.SameAsBefore);
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
                            _gridCellsCollapsed[x, yCentre + y1]._char = _gridCellsCollapsed[x, y]._char;
                            _gridCells[x, y].RemoveTile(EGridCellType.SameAsBefore);
                            _gridCellsCollapsed[x, y].RemoveTile(EGridCellType.SameAsBefore);
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

        public void ShowOrHideHelp()
        {
            if(_iCurrIntroScreen <= 0)
                _iCurrIntroScreen = 1;
            else
                _iCurrIntroScreen = -1;
        }
        public void OnTapped(PointF pntTouched)
        {
            if (_iCurrIntroScreen == 0)
            {
                _iCurrIntroScreen = -1;
                return;
            }
            else if (_iCurrIntroScreen > 0)
            {
                _iCurrIntroScreen++;
                if (_iCurrIntroScreen == _iNumIntroImages)
                    _iCurrIntroScreen = -1;
                return;
            }
            _iFrameOfPrevTouch = _iFrameNum;
            if (!_bRunning)
                IsRunning = true;
            if (_bShowHint)
                _bShowHint = false;
            if (_bBlobCollapsing)
                return;
            if (_TilesMoving.Count < 1)
                return;
            _vTapPos = pntTouched;
            if (_iTileBeingDragged >= 0)
            {
                _posGridTouch = GridPosFromScreenPosOrNull(_vTapPos);
                // If we've tapped on a valid cell...
                if (_posGridTouch._bDefined)
                {
                    if (_iShowingFoundWords > 0)
                    {
                        if (TileAt(_posGridTouch)._char != 0 && TileAt(_posGridTouch)._iInWordOfLen != 0)
                        {
                            // If we've tapped on a cell with a tile in it and it's already being shown, skip to the end of the showing period to save time,
                            // so the found words will be removed.
                            _iShowingFoundWords = 1;
                            return;
                        }
                    }
                    if (TileAt(_posGridTouch)._bOnEdgeOfBlob && _iShowingFoundWords == 0 &&
                        _iTimeLeftToDropTile > 3 && _iTimeLeftToDropTile < _iTileDropTimeLimit)
                    {
                        // Otherwise, if we've tapped on an empty cell at the edge of the blob, and we're not showing found words, and the tile is not just
                        // about to drop into its randomly chosen position or just created a new drop tile, put a tile there.
                        DropATileAt(_posGridTouch);
                        _iFrameOfPrevAddTile = _iFrameNum;
                        return;
                    }
                }
                if (_iShowingFoundWords > 0)
                {
                    // Otherwise, cancel showing found words and don't remove the found words.
                    _iShowingFoundWords = 0;
                }
            }
        }

        private void DropATileAt(Pos2 pos)
        {
            if (_iTileBeingDragged < 0)
                return;
            _iTimeLeftToDropTile = 0;
            SetTileCharAt(pos, _TilesMoving[_iTileBeingDragged].Letter);
            _TilesMoving[_iTileBeingDragged].Visible = false;
            int iNumEdgesFound = FindEdgesOfBlob();
            int iNumWordsFound = CheckForWordsInWholeGrid();
            if(iNumEdgesFound == 0 && iNumWordsFound == 0)
            {
                if (MainPage._This != null)
                {
                    FinishAllScoreFlies();
                    if(_iScore > _iTopScore)
                    {
                        _iTopScore = _iScore;
                        MainPage._This.UpdateTopScoreNextFrame();
                        SaveSettingsToFile();
                    }
                    MainPage._This.GameOver();
                    return;
                }
            }

            // Gradually reduce the time before a tile is dropped automatically, making the game harder and harder.
            if (_iTileDropTimeLimit > _iTileDropTimeLimitMin)
            {
                _iTileDropTimeLimit -= _iTileDropTimeLimitStep;
            }
        }

        // If there are any ScoreFlies, add their scores to the score and remove them.
        private void FinishAllScoreFlies()
        {
            if (MainPage._This != null)
            {
                foreach (ScoreFly score in _ScoreFlies)
                {
                    Score += score.Score;
                    MainPage._This.UpdateTheScoreNextFrame();
                }
            }
            _ScoreFlies.Clear();
        }
        public void OnTouchDown(PointF pntTouched)
        {
            if (_iCurrIntroScreen >= 0)
                return;
            _iFrameOfPrevTouch = _iFrameNum;
            if (_bShowHint)
                _bShowHint = false;
            if (_bBlobCollapsing || _iShowingFoundWords > 0 || !_bRunning)
                return;
            //if(_AudioPlayers != null && _AudioPlayers[0] != null)
            //    _AudioPlayers[0].Play();
            _vTouchDownPos = _vTouchMovePos = pntTouched;
            if(_bUserBorder)
                MoveTileToNearestPositionInBorderTo(_vTouchDownPos);
            _posGridTouch = GridPosFromScreenPosOrNull(_vTouchDownPos);
            if (_posGridTouch._bDefined && TileAt(_posGridTouch)._char != 0)
            {
                CheckForWordsInWholeGrid();
                //CheckForWordsCentredOn(_posGridTouch);
            }
        }

        public void OnTouchMove(PointF pntTouched)
        {
            if (_iCurrIntroScreen >= 0)
                return;
            _iFrameOfPrevTouch = _iFrameNum;
            if (_bBlobCollapsing || _iShowingFoundWords > 0 || !_bRunning)
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

        public bool OnTouchHintButton()
        {
            if (!IsRunning || _bShowHint || _iCurrIntroScreen >= 0)
                return false;
            _bShowHint = _iNumHintsLeft > 0 && _posGridHint._bDefined;
            if (_bShowHint)
                _iNumHintsLeft--;
            return _bShowHint;
        }

        public void OnTouchClock()
        {
            if (_iCurrIntroScreen >= 0)
                return;
            IsRunning = !IsRunning;
        }

        private void MoveTileToNearestPositionInBorderTo(Vect2 pos)
        {
            if (_iTileBeingDragged >= 0 && _iTileBeingDragged < _TilesMoving.Count)
            {
                Vect2 posTileCentre = _TilesMoving[_iTileBeingDragged].PosCentre;
                _TilesMoving[_iTileBeingDragged].PosCentre = NearestPositionInBorderTo(pos, posTileCentre, _TilesMoving[_iTileBeingDragged].Size, out _eCurrBorder);
                _TilesMoving[_iTileBeingDragged].Visible = true;
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
