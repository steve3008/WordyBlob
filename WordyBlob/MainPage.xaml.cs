using WordyBlob.Drawables;

namespace WordyBlob;

public partial class MainPage : ContentPage
{
    public static MainPage? _This;
    public static WordyBlobGame? _Game = null;

    WordyBlobDrawable _WordyBlobDrawable;
    ScoreDrawable _scoreDrawable, _topScoreDrawable;
    ClockDrawable _clockDrawable;
    bool _bUpdateScore = true, _bUpdateTopScore = true, _bUpdateClock = true;
    private IDispatcherTimer? _timerMain;
    public static bool _bShowDebugInfo = false;
    public const int _iFramesPerSecond = 25;
    bool _bKillGameNextFrame = false;

    public MainPage()
    {
        InitializeComponent();
        _This = this;
        if (MainPage._Game == null)
            MainPage._Game = new WordyBlobGame();

        // Touch event handlers
        WordyBlobGraphicsView.TouchStart += (sender, e) =>
        {
            if (sender != null)
                OnTouchStartedMainGame(sender, e);
        };
        WordyBlobGraphicsView.TouchMove += (sender, e) =>
        {
            if (sender != null)
                OnTouchMovedMainGame(sender, e);
        };
        WordyBlobGraphicsView.TouchEnd += (sender, e) =>
        {
            if(sender != null)
                OnTouchEndedMainGame(sender, e);
        };

        // Setup the scoreboard
        _scoreDrawable = new ScoreDrawable(4);
        ScoreGraphicsView.Drawable = _scoreDrawable;
        _topScoreDrawable = new ScoreDrawable(4);
        TopScoreGraphicsView.Drawable = _topScoreDrawable;
        _clockDrawable = new ClockDrawable();
        ClockGraphicsView.Drawable = _clockDrawable;

        // Setup the main game drawable
        _WordyBlobDrawable = new WordyBlobDrawable();
        WordyBlobGraphicsView.Drawable = _WordyBlobDrawable;

        // Setup the frame timer
        _timerMain = Dispatcher.CreateTimer();
        _timerMain.IsRepeating = true;
        _timerMain.Interval = TimeSpan.FromSeconds(1.0 / (double)_iFramesPerSecond);
        _timerMain.Tick += TimerMain_Tick;
        _timerMain?.Start();
    }

    private void TimerMain_Tick(object? sender, EventArgs e)
    {
        long t1 = 0;
        if (_bShowDebugInfo)
            t1 = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

        // Update the main game
        _WordyBlobDrawable.IncrementAnimationFrame();
        WordyBlobGraphicsView.Invalidate();

        // Update the scoreboard
        if (_bUpdateScore)
        {
            int score = _WordyBlobDrawable.Score;
            _scoreDrawable.TheValue = score;
            _bUpdateScore = false;
        }
        if (_bUpdateTopScore)
        {
            _topScoreDrawable.TheValue = _WordyBlobDrawable.TopScore;
            _bUpdateTopScore = false;
        }
        if (_bUpdateClock)
        {
            _clockDrawable.TheTime = _WordyBlobDrawable.TheTime;
            _bUpdateClock = false;
            ClockGraphicsView.Invalidate();
        }

        if (_scoreDrawable.IncrementAnimationFrame())
            ScoreGraphicsView.Invalidate();
        if (_topScoreDrawable.IncrementAnimationFrame())
            TopScoreGraphicsView.Invalidate();

        // Kill game
        if (_bKillGameNextFrame && Application.Current != null)
        {
            Application.Current.Quit();
        }

        if (_bShowDebugInfo)
        {
            long t2 = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            WordyBlobGame._iFrameTime = (int)(t2 - t1);
        }
    }
    public void UpdateTheScoreNextFrame()
    {
        _bUpdateScore = true;
    }
    public void UpdateTopScoreNextFrame()
    {
        _bUpdateTopScore = true;
    }
    public void UpdateTheClockNextFrame()
    {
        _bUpdateClock = true;
    }

    // Helper method to convert Android coordinates to MAUI coordinates
    private Point GetScaledPoint(Vect2 p)
    {
        double density = Microsoft.Maui.Devices.DeviceDisplay.Current.MainDisplayInfo.Density;
        return p / density;
    }

    private void OnTouchStartedMainGame(object sender, TouchableGraphicsView.TouchPointEventArgs e)
    {
        _WordyBlobDrawable.OnTouchDown(GetScaledPoint(e.Pos));
    }
    private void OnTouchMovedMainGame(object sender, TouchableGraphicsView.TouchPointEventArgs e)
    {
        _WordyBlobDrawable.OnTouchMove(GetScaledPoint(e.Pos));
    }
    private void OnTouchEndedMainGame(object sender, TouchableGraphicsView.TouchPointEventArgs e)
    {
        _WordyBlobDrawable.OnTouchUp(GetScaledPoint(e.Pos));
    }

    private void OnTappedMainGame(object sender, TappedEventArgs e)
    {
        Point? pos = e.GetPosition(WordyBlobGraphicsView);

        _WordyBlobDrawable.OnTapped(pos);
    }
    private void OnTouchClock(object sender, TappedEventArgs e)
    {
        _WordyBlobDrawable.OnTouchClock();
    }

    private void OnTouchCurrentScore(object sender, TappedEventArgs e)
    {
    }
    private void OnTouchTopScore(object? sender, TappedEventArgs e)
    {

    }

    private void OnTouchButtonHint(object? sender, TappedEventArgs e)
    {
        if (_WordyBlobDrawable.OnTouchHintButton())
            ScoreBoardButton_Hint.Source = "scbd_button_hint_down.png";
    }

    private void OnTouchScoreboard(object sender, TappedEventArgs e)
    {
        if (MainPage._Game != null)
            MainPage._Game.ShowOrHideHelp();

        Point? pos = e.GetPosition(ScoreboardGrid);
        if (pos == null)
            return;
        int x = (int)(pos.Value.X * 1080.0 / ScoreboardGrid.Width);
    }

    private void OnTouchNextLevelGrid(object sender, TappedEventArgs e)
    {
        NextLevelGrid.IsVisible = false;
        _WordyBlobDrawable.StartGame();
        _WordyBlobDrawable.GameIsRunning = true;
    }

    public void EnableHintButton(bool bEnable)
    {
        ScoreBoardButton_Hint.Source = bEnable ? "scbd_button_hint_up.png" : "scbd_button_hint_grey.png";
    }

    public void GameOver()
    {
        NextLevelGrid.IsVisible = true;
        _WordyBlobDrawable.GameIsRunning = false;
    }
}
