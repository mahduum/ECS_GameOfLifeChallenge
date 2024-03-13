using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Systems
{
    [DisableAutoCreation]
    public partial struct CachedGridSectionsInitializationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfig>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            // var gridOfLife = SystemAPI.GetSingletonEntity<GridConfig>();
            // var gridConfig = SystemAPI.GetComponent<GridConfig>(gridOfLife);
            // var gridSections = gridConfig.Grid.GetCacheGridSections();
            //
            // //todo try later like in snake
            // // var cachedGridSectionEntityTemplates =
            // //     state.EntityManager.CreateEntity(gridConfig.CellPrefab, gridSections.Count, state.WorldUpdateAllocator);
            //
            // //TODO: bottom is wrong, we must spawn all entities, and then add them to shared, that means that border entities
            // //will be found in more than one shared component, but it may be ok.
            // var entities = state.EntityManager.Instantiate(gridConfig.CellPrefab, gridConfig.Grid.TotalCells, state.WorldUpdateAllocator);
            //
            // for (int i = 0; i < gridSections.Count; i++)
            // {
            //     var section = gridSections[i];
            //     FixedList64Bytes<bool> cellStatusList = new FixedList64Bytes<bool>();//todo make it a bitmap
            //     for (int j = 0, k = 0; j < section.CellStatusList.Count; j++)
            //     {
            //         cellStatusList.Add(section.CellStatusList[j]);
            //         
            //         
            //         //for future updates to avoid setting other cells data, we care about it because of multithreading, entity must have index from grid, so external border is just a lookup.
            //         if (j < gridConfig.Grid.SectionStrideWithOverlap ||
            //             j > gridConfig.Grid.SectionStrideWithOverlap * gridConfig.Grid.SectionStrideWithOverlap -
            //             gridConfig.Grid.SectionStrideWithOverlap ||
            //             j % gridConfig.Grid.SectionStrideWithOverlap == 0 ||
            //             j % gridConfig.Grid.SectionStrideWithOverlap == gridConfig.Grid.SectionStrideWithOverlap - 1)
            //         {
            //             continue;
            //         }
            //         
            //         //get coordinates with offset, first local, then add offset
            //         var localCoords = GridOfLife.GetCoordsFromIndex(j, gridConfig.Grid.SectionStrideWithOverlap,
            //             gridConfig.Grid.SectionStrideWithOverlap);
            //         
            //         var globalCoords = localCoords + section.Offset;
            //
            //         var entity = k + i * gridConfig.Grid.SectionStride * gridConfig.Grid.SectionStride;
            //         k++;
            //         
            //         // state.EntityManager.SetComponentData(entity, new LocalToWorld()
            //         // {
            //         //     Value = float4x4.TRS()
            //         // });
            //         
            //         
            //     }
            //
            //     //set shared:
            //     
            //     var entitiesToSpawn = gridConfig.Grid.SectionStride * gridConfig.Grid.SectionStride;
            //     
            //     for (int k = 0; k < entitiesToSpawn; k++)
            //     {
            //         //spawn only internal entities
            //         var entity = state.EntityManager.Instantiate(gridConfig.CellPrefab);
            //         //var position = section.Offset + GridOfLife.
            //             state.EntityManager.SetComponentData(entity, new LocalToWorld()
            //             {
            //                 //Value = float4x4.TRS()
            //             });
            //     }
            //
            //     var cells = state.EntityManager.Instantiate(gridConfig.CellPrefab, gridConfig.Grid.SectionStride, state.WorldUpdateAllocator);

                //todo this may not need be shared? but initialize shared from it?
                // state.EntityManager.SetSharedComponent(cachedGridSectionEntityTemplates[i], new CachedGridSection64()
                // {
                //     Offset = section.Offset,
                //     StatusList = cellStatusList
                // });
            //}
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        // protected override void OnCreate()
        // {
        //     RequireForUpdate<GridConfig>();
        // }
        //
        // protected override void OnUpdate()
        // {
        //     Enabled = false;
        //     SystemAPI.GetSingletonEntity<GridConfig>();
        //     SystemAPI.ManagedAPI.GetSingletonEntity<GridConfig>();
        // }
    }
}