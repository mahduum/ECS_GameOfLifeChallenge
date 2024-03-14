using System;
using Unity.Mathematics;

namespace Utils
{
    public static class GridOfLife
    {
        public const int NeighboursCount = 8;
        
        private static readonly int2[] Offsets =
        {
            new (-1, 1), new (0, 1), new (1, 1),
            new (-1, 0), new (1, 0),
            new (-1, -1), new (0, -1), new (1, -1)
        };

        private static int WrapModulo(int dividend, int modulus)
        {
            return ((dividend % modulus) + modulus) % modulus;
        }
        
        public static int GetNeighbourIndex(int centerIndex, int neighbourNumber, int width, int height)
        {
            var centerCoords = new int2(centerIndex % width, centerIndex / width);

            int2 wrap = new int2(width, height);
            
            var neighbourCoords = centerCoords + Offsets[neighbourNumber];

            if (neighbourCoords.x < 0 || neighbourCoords.x >= wrap.x)
            {
                neighbourCoords.x = WrapModulo(neighbourCoords.x, wrap.x);
            }
            
            if (neighbourCoords.y < 0 || neighbourCoords.y >= wrap.y)
            {
                neighbourCoords.y = WrapModulo(neighbourCoords.y, wrap.y);
            }
            
            return neighbourCoords.y * width + neighbourCoords.x;
        }

        public static int2 GetCoordsFromIndex(int index, int gridWidth, int gridHeight)
        {
            if (index >= gridHeight * gridWidth)
            {
                throw new IndexOutOfRangeException("Cannot convert centerIndex to coordinates!");
            }
            return new int2(index % gridHeight, index / gridWidth);
        }
    }
}