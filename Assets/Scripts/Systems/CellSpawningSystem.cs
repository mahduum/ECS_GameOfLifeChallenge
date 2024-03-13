using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Utils;
using Random = Unity.Mathematics.Random;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    //[UpdateBefore(typeof(DetectCellStateChangeSystem))]
    public partial struct CellSpawningSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfig>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            ComponentLookup<LocalToWorld> localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
            ComponentLookup<Cell> cellComponentLookup = SystemAPI.GetComponentLookup<Cell>();
            ComponentLookup<IsAlive> isAliveComponentLookup = SystemAPI.GetComponentLookup<IsAlive>();
            
            var gridOfLife = SystemAPI.GetSingletonEntity<GridConfig>();
            var gridConfig = SystemAPI.GetComponent<GridConfig>(gridOfLife);
            var gridLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(gridOfLife);
            //NOTE: previously I had a prefab with shared component and all entity non shared components and it was possible to spawn it that way
            //because there was a prefab entity from which instances were being created, so now I need to do this the same way, use base entities of
            //grid sections to Instantiate them with separate data, remember about prefab components for 

            var allCells =
                CollectionHelper.CreateNativeArray<Entity, RewindableAllocator>(gridConfig.GridHeight * gridConfig.GridWidth,
                    ref state.WorldUnmanaged.UpdateAllocator);

            state.EntityManager.Instantiate(gridConfig.CellPrefab, allCells);
            //assign positions, set neighbours, record positions on the grid, and then update neighbours
            var assignPositionsOnGridJob = new AssignPositionsOnGridJob()
            {
                GridConfig = gridConfig,
                Entities = allCells,
                CellFromEntity = cellComponentLookup,
                LocalToWorldFromEntity = localToWorldLookup,
                isAliveFromEntity = isAliveComponentLookup,
                Origin = gridLocalToWorld.Position,
                Scale = gridConfig.PrefabScale,
                Random = new Random((uint)SystemAPI.Time.ElapsedTime + 1)
            };

            state.Dependency = assignPositionsOnGridJob.Schedule(allCells.Length, 6, state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    struct AssignPositionsOnGridJob : IJobParallelFor//todo make it entity job
    {
        [ReadOnly] public GridConfig GridConfig;
        public NativeArray<Entity> Entities;
        
        [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction]
        public ComponentLookup<LocalToWorld> LocalToWorldFromEntity;
        
        [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction]
        public ComponentLookup<Cell> CellFromEntity;
        
        [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction]
        public ComponentLookup<IsAlive> isAliveFromEntity;

        public float3 Origin;
        public float3 Scale;

        public Random Random;
        
        //readwrite native array of grid indices, each index will contain entity
        
        public void Execute(int index)
        {
            var coordinates = GridOfLife.GetCoordsFromIndex(index, GridConfig.GridWidth, GridConfig.GridHeight);
            var entity = Entities[index];
            var isAlive = Random.NextInt(0, 100) < 10;
            var cellPosition = Origin.xyz + new float3(coordinates.x, isAlive ? 1f : 0, coordinates.y) * Scale.xyz;
            //Debug.Log($"Coordinates: {coordinates}, cell position: {cellPosition}");
            var localToWorld = new LocalToWorld()
            {
                Value = float4x4.TRS(cellPosition, quaternion.identity, Scale.xyz)
            };

            LocalToWorldFromEntity[entity] = localToWorld;
            CellFromEntity[entity] = new Cell(){ GridIndex = index };
            isAliveFromEntity[entity] = new IsAlive() { Value = isAlive };
        }
    }
}