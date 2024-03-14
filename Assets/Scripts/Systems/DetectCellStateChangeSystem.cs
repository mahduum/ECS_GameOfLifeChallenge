using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct DetectCellStateChangeSystem : ISystem
    {
        private ComponentLookup<FlipCellState> _flipCellStateLookup;
        private ComponentLookup<IsAlive> _isAliveLookup;
        private double _lastUpdateTime;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<Cell>();
            state.RequireForUpdate<IsAlive>();
            
            _flipCellStateLookup = state.GetComponentLookup<FlipCellState>();
            _isAliveLookup = state.GetComponentLookup<IsAlive>();
            _lastUpdateTime = 0.0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsed = SystemAPI.Time.ElapsedTime;
            if ((elapsed - _lastUpdateTime) < 0.5)
            {
                //return;
            }

            _lastUpdateTime = elapsed;
            
            _flipCellStateLookup.Update(ref state);
            _isAliveLookup.Update(ref state);
            var gridConfigEntity = SystemAPI.GetSingletonEntity<GridConfig>();//this config should be system?
            var gridConfig = SystemAPI.GetComponent<GridConfig>(gridConfigEntity);

            var detectCellStateChangeDynamicQuery = SystemAPI.QueryBuilder().WithAll<Neighbours>().Build();

            //todo check how firemen are done with dynamic buffer
            var detectCellStateChangeDynamicJob = new DetectCellStateChangeDynamicJob()
            {
                FlipCellStateLookup = _flipCellStateLookup,
                IsAliveLookup = _isAliveLookup,
            };

            state.Dependency = detectCellStateChangeDynamicJob.ScheduleParallel(detectCellStateChangeDynamicQuery, state.Dependency);
            
            var query = SystemAPI.QueryBuilder().WithAll<Cell, IsAlive>().WithDisabled<Neighbours>().Build();
            var entityCount = query.CalculateEntityCount();

            if (entityCount == 0)
            {
                return;
            }
            
            NativeParallelHashMap<int, Entity> gridIndexToEntity = new NativeParallelHashMap<int, Entity>(entityCount, state.WorldUpdateAllocator);
            NativeParallelHashMap<int, Entity>.ParallelWriter gridParallelWriter = gridIndexToEntity.AsParallelWriter();
            
            //first change to entity and do query only once, or simply assign once a component with neighbours to entity
            
            var gridIndexToEntityJob = new MapGridIndexToEntityJob()
            {
                GridIndexToEntity = gridParallelWriter
            };
            
            var mappingJobHandle = gridIndexToEntityJob.ScheduleParallel(query, state.Dependency);

            //
            // var detectCellStateChangeJob = new DetectCellStateChangeJob()
            // {
            //     FlipCellStateLookup = _flipCellStateLookup,
            //     GridIndexToEntity = gridIndexToEntity,
            //     GridConfig = gridConfig
            // };
            //
            // state.Dependency = detectCellStateChangeJob.ScheduleParallel(query, mappingJobHandle);
            
            // Refs on entities solution:
            var setReferencesQuery = SystemAPI.QueryBuilder().WithAll<Cell>().WithDisabledRW<Neighbours>().Build();
            var endSimSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer endSimBuffer = endSimSystem.CreateCommandBuffer(state.WorldUnmanaged);

            var addNeighboursJob = new AddNeighboursJob()
            {
                GridConfig = gridConfig,
                Ecb = endSimBuffer.AsParallelWriter(),
                GridIndexToEntity = gridIndexToEntity,
            };

            state.Dependency = addNeighboursJob.ScheduleParallel(setReferencesQuery, mappingJobHandle);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct DetectCellStateChangeDynamicJob : IJobEntity//todo check how it performs per chunk
        {
            [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public ComponentLookup<FlipCellState> FlipCellStateLookup;
            [ReadOnly]
            public ComponentLookup<IsAlive> IsAliveLookup;
            public void Execute(in Entity entity, in Neighbours neighbours)
            {
                int livingNeighboursCount = 0;
                
                foreach (var neighbour in neighbours.Entities)
                {
                    if (IsAliveLookup[neighbour].Value)
                    {
                        livingNeighboursCount++;
                    }
                }
                
                bool flip = false;
                if (IsAliveLookup[entity].Value)//todo bools are cached so state can be changed here directly?
                {
                    if (livingNeighboursCount is <= 1 or >= 4)
                    {
                        //die
                        flip = true;
                    }
                }
                else if (livingNeighboursCount == 3)
                {
                    //bring back to life
                    flip = true;
                }
                
                if(neighbours.Entities.Length > 0)
                    FlipCellStateLookup.SetComponentEnabled(entity, flip);
            }
        }
        
        [BurstCompile]
        public partial struct AddNeighboursJob : IJobEntity
        {
            [ReadOnly]
            public NativeParallelHashMap<int, Entity> GridIndexToEntity;
            public GridConfig GridConfig;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(in Entity entity, in Cell cell, ref Neighbours neighbours, [EntityIndexInQuery] int sortKey)
            {
                for (int i = 0; i < GridOfLife.NeighboursCount; i++)
                {
                    var neighbourGridIndex = GridOfLife.GetNeighbourIndex(cell.GridIndex, i, GridConfig.GridWidth,
                        GridConfig.GridHeight);

                    if (neighbourGridIndex < 0 || neighbourGridIndex >= GridConfig.GridWidth * GridConfig.GridHeight)
                    {
                        Debug.LogError(
                            $"Index should not be less than 0, inputs: cell grid index: {cell.GridIndex}, neighbour number: {i}, width: {GridConfig.GridWidth}, height: {GridConfig.GridHeight}");
                    }

                    if (GridIndexToEntity.TryGetValue(neighbourGridIndex, out var val))
                    {
                        neighbours.Entities.Add(val);
                    }
                    else
                    {
                        Debug.LogError(
                            $"Item index {neighbourGridIndex} not found, hash map length: {GridIndexToEntity.Count()} ");
                    }
                }
                
                if(neighbours.Entities.Length > 0)
                    Ecb.SetComponentEnabled<Neighbours>(sortKey, entity, true);
            }
        }

        [BurstCompile]
        public partial struct MapGridIndexToEntityJob : IJobEntity
        {
            public NativeParallelHashMap<int, Entity>.ParallelWriter GridIndexToEntity;//todo as possible optimization set hash map once and keep it for whole duration?
            
            public void Execute(in Entity entity, in Cell cell, in IsAlive isAlive)
            {
                if (GridIndexToEntity.TryAdd(cell.GridIndex, entity) == false)
                {
                    Debug.LogError($"Couldn't add key: {cell.GridIndex} with value: {isAlive.Value}");
                }
            }
        }

        //todo separate query for dead -> there is only on condition to bring them back
        //for living -> only one condition to keep them unchanged
        [BurstCompile]
        public partial struct DetectCellStateChangeJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public ComponentLookup<FlipCellState> FlipCellStateLookup;
            
            [ReadOnly]
            public NativeParallelHashMap<int, bool> GridIndexToEntity;
            public GridConfig GridConfig;
            
            public void Execute(in Entity entity, in Cell cell, in IsAlive isAlive)
            {
                int livingNeighboursCount = 0;
                for (int i = 0; i < GridOfLife.NeighboursCount; i++)
                {
                   var neighbourGridIndex = GridOfLife.GetNeighbourIndex(cell.GridIndex, i, GridConfig.GridWidth, GridConfig.GridHeight);

                   if (neighbourGridIndex < 0 || neighbourGridIndex >= GridConfig.GridWidth * GridConfig.GridHeight)
                   {
                       Debug.LogError($"Index should not be less than 0, inputs: cell grid index: {cell.GridIndex}, neighbour number: {i}, width: {GridConfig.GridWidth}, height: {GridConfig.GridHeight}");
                   }
                   
                   if (GridIndexToEntity.TryGetValue(neighbourGridIndex, out var val))
                   {
                       if(val)
                           livingNeighboursCount++;
                   }
                   else
                   {
                       Debug.LogError($"Item index {neighbourGridIndex} not found, hash map length: {GridIndexToEntity.Count()} ");
                   }
                }
                
                //default unchanged
                bool flip = false;
                if (isAlive.Value)//todo bools are cached so state can be changed here directly?
                {
                    if (livingNeighboursCount is <= 1 or >= 4)
                    {
                        //die
                        flip = true;
                    }
                }
                else if (livingNeighboursCount == 3)
                {
                    //bring back to life
                    flip = true;
                }
                
                //alternative
                //default flip
                // bool flip = true;
                // if (isAlive.Value)//todo bools are cached so state can be changed here directly?
                // {
                //     if (livingNeighboursCount is > 1 and < 4)
                //     {
                //         flip = false;
                //     }
                // }
                // else if (livingNeighboursCount != 3)
                // {
                //     flip = false;
                // }

                //todo consider processing only disabled (no lookup) but then another system must disable it
                FlipCellStateLookup.SetComponentEnabled(entity, flip);
            }
        }
    }
}