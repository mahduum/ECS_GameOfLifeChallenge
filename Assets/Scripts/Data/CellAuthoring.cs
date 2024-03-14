using Unity.Entities;
using Unity.Rendering;
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
                AddComponent<Neighbours>(entity);
                SetComponentEnabled<Neighbours>(entity, false);
                //AddComponent(entity, new LocalTransform());
                //AddSharedComponent(entity, new CachedGridSection64());
            }
        }
    }
}