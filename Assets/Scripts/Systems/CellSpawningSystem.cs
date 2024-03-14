using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Utils;
using Random = Unity.Mathematics.Random;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
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

            var allCells =
                CollectionHelper.CreateNativeArray<Entity, RewindableAllocator>(gridConfig.GridHeight * gridConfig.GridWidth,
                    ref state.WorldUnmanaged.UpdateAllocator);

            state.EntityManager.Instantiate(gridConfig.CellPrefab, allCells);

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
    }

    [BurstCompile]
    struct AssignPositionsOnGridJob : IJobParallelFor
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
        
        public void Execute(int index)
        {
            var coordinates = GridOfLife.GetCoordsFromIndex(index, GridConfig.GridWidth, GridConfig.GridHeight);
            var entity = Entities[index];
            var isAlive = Random.NextInt(0, 100) < GridConfig.AliveOnSpawnProbability;
            var cellPosition = Origin.xyz + new float3(coordinates.x, isAlive ? 1f : 0, coordinates.y) * Scale.xyz;
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