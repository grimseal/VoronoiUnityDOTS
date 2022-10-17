using System;

namespace Voronoi
{
    internal struct StructList4<T>
    {
        private T v0;
        private T v1;
        private T v2;
        private T v3;
        public int Length { get; private set; }

        public void Add(T v)
        {
            switch (Length)
            {
                case 0:
                    v0 = v;
                    break;
                case 1:
                    v1 = v;
                    break;
                case 2:
                    v2 = v;
                    break;
                case 3:
                    v3 = v;
                    break;
                default: throw new IndexOutOfRangeException();
            }

            Length++;
        }

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return v0;
                    case 1: return v1;
                    case 2: return v2;
                    case 3: return v3;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public void RemoveAtSwapBack(int index)
        {
            if (Length <= 1)
            {
                Length = 0;
                return;
            }

            switch (index)
            {
                case 0:
                    v0 = this[Length - 1];
                    break;
                case 1:
                    v1 = this[Length - 1];
                    break;
                case 2:
                    v2 = this[Length - 1];
                    break;
                case 3:
                    v3 = this[Length - 1];
                    break;
                default: throw new IndexOutOfRangeException();
            }

            Length--;
        }
    }
}
