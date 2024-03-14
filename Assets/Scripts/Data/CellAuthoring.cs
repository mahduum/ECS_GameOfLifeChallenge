using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Data
{
    public class CellAuthoring : MonoBehaviour
    {
        private class CellAuthoringBaker : Baker<CellAuthoring>
        {
            public override void Bake(CellAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                
                AddComponent(entity, new Cell());
                AddComponent(entity, new IsAlive());
                AddComponent(entity, new FlipCellState());
                SetComponentEnabled<FlipCellState>(entity, false);
            }
        }
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