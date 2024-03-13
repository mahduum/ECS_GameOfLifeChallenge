using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Data
{
    //todo make more of them with varying sizes, try 8x8
    public struct CachedGridSection64 : ISharedComponentData//it may be a list itself??? 49 bytes for 7x7 booleans, 81 for
    {
        public FixedList64Bytes<bool> StatusList;
        public int2 Offset;
    }

    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct Cell : IComponentData
    {
        public int GridIndex;
    }
    
    public struct CellIndexComparer : IComparer<Cell>
    {
        public int Compare(Cell x, Cell y)
        {
            return x.GridIndex.CompareTo(y.GridIndex);
        }
    }

    public struct IsAlive : IComponentData
    {
        public bool Value;
    }
    
    public struct FlipCellState : IComponentData, IEnableableComponent
    {
        
    }
}