using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(DetectCellStateChangeSystem))]
    public partial struct FlipCellStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<FlipCellState>();
            state.RequireForUpdate<IsAlive>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //NOTE: for 1M linear took about 5-8 ms
            // float3 aliveOffset = new float3(0, 1f, 0);
            // foreach (var (isAlive, localToWorld, entity) in SystemAPI.Query<RefRW<IsAlive>, RefRW<LocalToWorld>>().WithAll<FlipCellState>().WithEntityAccess())
            // {
            //     float3 newPosition = localToWorld.ValueRO.Position +
            //                          (isAlive.ValueRO.Value ? -aliveOffset : aliveOffset);//if was dead at the start leave it
            //     
            //     isAlive.ValueRW.Value = !isAlive.ValueRO.Value;//change appearence
            //     
            //     localToWorld.ValueRW = new LocalToWorld()
            //     {
            //         Value = float4x4.TRS(newPosition, quaternion.identity, new float3(1f, 1f, 1f))
            //     };
            //     
            //     state.EntityManager.SetComponentEnabled<FlipCellState>(entity, false);
            // }
            
            //NOTE: parallelized for 1M 0.05 ms
            var query = SystemAPI.QueryBuilder().WithAllRW<IsAlive, LocalToWorld>().WithAll<FlipCellState>().Build();
            
            var flipStateJob = new FilipStateJob()
            {
            };
            
            state.Dependency = flipStateJob.ScheduleParallel(query, state.Dependency);
        }
        
        [BurstCompile]
        [WithAll(typeof(FlipCellState))]
        private partial struct FilipStateJob : IJobEntity
        {
            public void Execute(ref IsAlive isAlive,
                ref LocalToWorld localToWorld)
            {
                float3 aliveOffset = new float3(0, 1f, 0);
                
                float3 newPosition = localToWorld.Position +
                                     (isAlive.Value ? -aliveOffset : aliveOffset);//if was dead at the start leave it
                
                isAlive.Value = !isAlive.Value;
                
                localToWorld = new LocalToWorld()
                {
                    Value = float4x4.TRS(newPosition, quaternion.identity, new float3(1f, 1f, 1f))
                };
            }
        }
    }
}