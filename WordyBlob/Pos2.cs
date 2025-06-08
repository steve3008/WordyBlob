namespace WordyBlob
{
    public struct Pos2
    {
        public int X, Y;
        public bool _bDefined = false;

        public Pos2()
        {
            _bDefined = false;
        }

        public Pos2(int x, int y)
        {
            X = x;
            Y = y;
            _bDefined = true;
        }

        public static Pos2 NULL
        {
            get
            {
                return new Pos2();
            }
        }

        /*
        public static bool operator == (Pos2 p1, Pos2 p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static bool operator !=(Pos2 p1, Pos2 p2)
        {
            return !(p1 == p2);
        }
        */
    }
}
