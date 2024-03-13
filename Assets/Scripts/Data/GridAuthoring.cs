using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Utils;

namespace Data
{
    public class GridAuthoring : MonoBehaviour
    {
        [SerializeField] private int _width = 216;
        [SerializeField] private int _height = 216;
        [SerializeField] private GameObject _cellPrefab;//give it a material
        
        private class GridAuthoringBaker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                var cellEntity = GetEntity(authoring._cellPrefab, TransformUsageFlags.Renderable);
                var gridEntity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(gridEntity, new GridConfig()
                {
                    //Grid = new GridOfLife(authoring._width, authoring._height),
                    CellPrefab = cellEntity,
                    GridWidth = authoring._width,
                    GridHeight = authoring._height,
                    PrefabScale = authoring._cellPrefab.transform.localScale
                });

                //Create shared components in systems?
            }
        }
    }

    [Serializable]
    public struct GridConfig : IComponentData
    {
        //public GridOfLife Grid;
        public int GridWidth;
        public int GridHeight;
        public Entity CellPrefab;
        public float3 PrefabScale;
    }
}