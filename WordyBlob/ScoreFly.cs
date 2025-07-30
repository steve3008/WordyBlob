using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordyBlob
{
    public class ScoreFly
    {
        Vect2 _PosCentreStart, _PosCentreEnd, _Pos;
        int _iPos, _iPosMax, _iDelayBeforeStart;
        int _iScore;
        float _fSize = 48.0f;
        Microsoft.Maui.Graphics.IImage? _Image = null;

        public ScoreFly(int iScore, Vect2 posCentreStart, Vect2 posCentreEnd, int iDelayBeforeStart)
        {
            _iScore = iScore;
            PosCentre = _PosCentreStart = posCentreStart;
            _PosCentreEnd = posCentreEnd;
            _iPos = 0;
            _iPosMax = 30 + WordyBlobGame._Rnd.Next(20);
            _iDelayBeforeStart = iDelayBeforeStart;
            _Image = MainPage._Game?.ScoreFlyImageForScore(_iScore);
        }

        public void Draw(ICanvas canvas)
        {
            if (_Image != null && _iDelayBeforeStart == 0)
                canvas.DrawImage(_Image, (float)_Pos.X, (float)_Pos.Y, _fSize, _fSize);
        }

        public Vect2 PosCentre
        {
            get
            {
                float d = _fSize * 0.5f;
                return new Vect2(_Pos.X + d, _Pos.Y + d);
            }
            set
            {
                float d = _fSize * 0.5f;
                _Pos = new Vect2(value.X - d, value.Y - d);
            }
        }

        public void Increment()
        {
            if(_iDelayBeforeStart > 0)
            {
                _iDelayBeforeStart--;
            }
            else if (_iPos <= _iPosMax)
            {
                int a = 128 - DrawFuncs._SinesInt[90 + _iPos * 90 / _iPosMax];
                PosCentre = _PosCentreStart + ((_PosCentreEnd - _PosCentreStart) * a / 128);
                _iPos++;
            }
        }

        public bool CanDelete
        {
            get
            {
                return _iPos > _iPosMax;
            }
        }
        public int Score
        {
            get
            {
                return _iScore;
            }
        }

        public Vect2 Pos
        {
            get
            {
                return _Pos;
            }
        }

        public float Size
        {
            get
            {
                return _fSize;
            }
        }
    }
}
