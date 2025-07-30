using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordyBlob
{
    public enum EGridCellType
    {
        Invalid,
        SameAsBefore,
        Plain,
        BonusX2,
        BonusX3
    }
    public struct GridCell
    {
        public char _char;
        public bool _bOnEdgeOfBlob;
        public int _iInWordOfLen;
        public EGridCellType _eType = EGridCellType.Plain;

        public GridCell(char c, bool bOnEdgeOfBlob, EGridCellType eType)
        {
            _char = c;
            _bOnEdgeOfBlob = bOnEdgeOfBlob;
            _eType = eType;
            _iInWordOfLen = 0;
        }

        public void RemoveTile(EGridCellType eType)
        {
            _char = (char)0;
            _bOnEdgeOfBlob = false;
            _iInWordOfLen = 0;
            if(eType != EGridCellType.SameAsBefore)
                _eType = eType;
        }

        public int ScoreMultiple
        {
            get
            {
                switch(_eType)
                {
                    case EGridCellType.BonusX2:
                        return 2;
                    case EGridCellType.BonusX3:
                        return 3;
                    default:
                        return 1;
                }
            }
        }
    }

    public class TileMoving
    {
        Vect2 _Pos, _Vel;
        bool _bVisible = true;

        Vect2 _posStart, _posEnd;
        bool _bFollowingAPath, _bDeleteAtEndOfPath;
        int _iPathFrame, _iMaxPathFrame;
        Pos2 _posEndInGrid;

        char _Letter = (char)0;
        float _fTileSize = 64.0f, _fTileStride = 60.0f;
        bool _bInBorder = false;
        Microsoft.Maui.Graphics.IImage? _Image = null;

        public TileMoving(char letter, float fTileSize, bool bInBorder, Vect2 posCentre)
        {
            _bVisible = true;
            _Letter = letter;
            _fTileSize = fTileSize;
            _fTileStride = _fTileSize * WordyBlobGame._fTileStrideRel;
            _bInBorder = bInBorder;
            PosCentre = posCentre;
            _posStart = _posEnd = _Pos;
            _iPathFrame = _iMaxPathFrame = 0;
            _bFollowingAPath = _bDeleteAtEndOfPath = false;
            _Vel = 0;
            _Image = MainPage._Game?.TileImageForChar(_Letter);
        }

        // Animation tile
        public TileMoving(char letter, float fTileSize, Vect2 posStart, Vect2 posEnd, Pos2 posEndInGrid, int iMaxPathFrame, bool bDeleteAtEndOfPath)
        {
            _bVisible = true;
            _Letter = letter;
            _fTileSize = fTileSize;
            _fTileStride = _fTileSize * WordyBlobGame._fTileStrideRel;
            _bInBorder = false;
            _posStart = _Pos = posStart;
            _posEnd = posEnd;
            _posEndInGrid = posEndInGrid;
            _iPathFrame = 0;
            _iMaxPathFrame = iMaxPathFrame;
            _bFollowingAPath = true;
            _bDeleteAtEndOfPath = bDeleteAtEndOfPath;
            _Vel = 0;
            _Image = MainPage._Game?.TileImageForChar(_Letter);
        }

        public void FollowPath(Vect2 start, Vect2 end, int iNumFrames)
        {
            _posStart = start;
            _posEnd = end;
            _iPathFrame = 0;
            _iMaxPathFrame = iNumFrames;
            _bFollowingAPath = true;
        }

        public bool FollowingAPath
        {
            get
            {
                return _bFollowingAPath;
            }
        }

        public bool CanDeleteNow
        {
            get
            {
                return !_bFollowingAPath && _bDeleteAtEndOfPath;
            }
        }

        public bool Visible
        {
            get { return _bVisible; }
            set { _bVisible = value; }
        }

        public Pos2 PosEndInGrid
        {
            get
            {
                return _posEndInGrid;
            }
        }

        public void Draw(ICanvas canvas)
        {
            if(_Image != null && _bVisible)
                canvas.DrawImage(_Image, (float)_Pos.X, (float)_Pos.Y, _fTileSize, _fTileSize);
        }

        public void Increment()
        {
            if (_bFollowingAPath)
            {
                _iPathFrame++;
                _Pos = _posStart + ((_posEnd - _posStart) * _iPathFrame / _iMaxPathFrame);
                if (_iPathFrame == _iMaxPathFrame)
                {
                    _bFollowingAPath = false;
                }
            }
            else
            {
                _Pos += _Vel;
            }
        }

        public Vect2 PosCentre
        {
            get
            {
                float d = _fTileSize * 0.5f;
                return new Vect2(_Pos.X + d, _Pos.Y + d);
            }
            set
            {
                float d = _fTileSize * 0.5f;
                _Pos = new Vect2(value.X - d, value.Y - d);
            }
        }

        public char Letter
        {
            get
            {
                return _Letter;
            }
        }

        public Vect2 Pos
        {
            get
            {
                return _Pos;
            }
        }

        public Vect2 Vel
        {
            get
            {
                return _Vel;
            }
            set
            {
                _Vel = value;
            }
        }

        public float Size
        {
            get
            {
                return _fTileSize;
            }
        }

        public bool ContainsPoint(Vect2 pnt)
        {
            return pnt.X >= _Pos.X && pnt.Y >= _Pos.Y && pnt.X < _Pos.X + _fTileSize && pnt.Y < _Pos.Y + _fTileSize;
        }
    }
}
