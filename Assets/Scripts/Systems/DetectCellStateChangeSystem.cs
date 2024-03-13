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
        private double _lastUpdateTime;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<Cell>();
            state.RequireForUpdate<IsAlive>();
            
            _flipCellStateLookup = state.GetComponentLookup<FlipCellState>();
            _lastUpdateTime = 0.0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsed = SystemAPI.Time.ElapsedTime;
            if ((elapsed - _lastUpdateTime) < 0.5)
            {
                return;
            }

            _lastUpdateTime = elapsed;
            
            _flipCellStateLookup.Update(ref state);
            var gridConfigEntity = SystemAPI.GetSingletonEntity<GridConfig>();//this config should be system?
            var gridConfig = SystemAPI.GetComponent<GridConfig>(gridConfigEntity);
            var query = SystemAPI.QueryBuilder().WithAll<Cell, IsAlive>().Build();
            var entityCount = query.CalculateEntityCount();
            // var componentDataArray = query.ToComponentDataArray<Cell>(Allocator.Temp);
            // var sortByGridIndex = componentDataArray.SortJob(new CellIndexComparer());
            // var sortHandle = sortByGridIndex.Schedule(state.Dependency);
            // sortHandle.Complete();
            // populate grid and 
            NativeParallelHashMap<int, bool> gridIndexToEntity = new NativeParallelHashMap<int, bool>(entityCount, state.WorldUpdateAllocator);
            NativeParallelHashMap<int, bool>.ParallelWriter gridParallelWriter = gridIndexToEntity.AsParallelWriter();
            
            var gridIndexToEntityJob = new MapGridIndexToEntityJob()
            {
                GridIndexToEntity = gridParallelWriter
            };

            var mappingJobHandle = gridIndexToEntityJob.ScheduleParallel(query, state.Dependency);

            var detectCellStateChangeJob = new DetectCellStateChangeJob()
            {
                FlipCellStateLookup = _flipCellStateLookup,
                GridIndexToEntity = gridIndexToEntity,
                GridConfig = gridConfig
            };

            state.Dependency = detectCellStateChangeJob.ScheduleParallel(query, mappingJobHandle);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public partial struct MapGridIndexToEntityJob : IJobEntity
        {
            public NativeParallelHashMap<int, bool>.ParallelWriter GridIndexToEntity;//todo as possible optimization set hash map once and keep it for whole duration?
            
            public void Execute(in Entity entity, in Cell cell, in IsAlive isAlive)
            {
                if (GridIndexToEntity.TryAdd(cell.GridIndex, isAlive.Value) == false)
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