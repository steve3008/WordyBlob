using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WordyBlob;

namespace WordyBlob.Drawables;

public class WordyBlobDrawable : IDrawable
{
    RectF _Rect = new RectF(0, 0, 0, 0);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        _Rect = dirtyRect;
        if (MainPage._Game == null)
            MainPage._Game = new WordyBlobGame();
        MainPage._Game.DrawCurrentFrame(canvas, _Rect);
    }

    public int TheTime
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.TheTime; }
    }
    public int Score
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.Score; }
    }
    public int TopScore
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.TopScore; }
    }
    public int AnimationFrame
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.AnimationFrame; }
    }

    public void IncrementAnimationFrame()
    {
        if (MainPage._Game == null)
            return;
        MainPage._Game.IncrementAnimationFrame();
    }

    public bool GameIsRunning
    {
        get
        {
            return MainPage._Game != null && MainPage._Game.IsRunning;
        }
        set
        {
            if (MainPage._Game != null)
                MainPage._Game.IsRunning = value;
        }
    }

    public void OnTapped(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTapped((PointF)pntTouched);
    }

    public void OnTouchDown(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTouchDown((PointF)pntTouched);
    }

    public void OnTouchMove(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTouchMove((PointF)pntTouched);
    }

    public void OnTouchUp(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTouchUp((PointF)pntTouched);
    }

    public void OnTouchClock()
    {
        if (MainPage._Game == null)
            return;
        MainPage._Game.OnTouchClock();
    }
}

