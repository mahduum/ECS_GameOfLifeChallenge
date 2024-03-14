using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Data
{
    public class GridAuthoring : MonoBehaviour
    {
        [Range(0, 100)]
        [SerializeField] private int _aliveOnSpawnProbability = 10;
        [SerializeField] private int _width = 216;
        [SerializeField] private int _height = 216;
        [SerializeField] private GameObject _cellPrefab;
        
        private class GridAuthoringBaker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                var cellEntity = GetEntity(authoring._cellPrefab, TransformUsageFlags.Renderable);
                var gridEntity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(gridEntity, new GridConfig()
                {
                    CellPrefab = cellEntity,
                    GridWidth = authoring._width,
                    GridHeight = authoring._height,
                    PrefabScale = authoring._cellPrefab.transform.localScale,
                    AliveOnSpawnProbability = authoring._aliveOnSpawnProbability
                });
            }
        }
    }

    [Serializable]
    public struct GridConfig : IComponentData
    {
        public int GridWidth;
        public int GridHeight;
        public Entity CellPrefab;
        public float3 PrefabScale;
        public int AliveOnSpawnProbability;
    }
}