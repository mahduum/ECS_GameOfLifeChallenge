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
            state.RequireForUpdate<FlipCellState>();
            state.RequireForUpdate<IsAlive>();
            state.RequireForUpdate<Neighbours>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float3 aliveOffset = new float3(0, 1f, 0);
            foreach (var (isAlive, localToWorld, cell, entity) in SystemAPI.Query<RefRW<IsAlive>, RefRW<LocalToWorld>, RefRO<Cell>>().WithAll<FlipCellState, Cell, Neighbours>().WithEntityAccess())
            {
                float3 newPosition = localToWorld.ValueRO.Position +
                                     (isAlive.ValueRO.Value ? -aliveOffset : aliveOffset);//if was dead at the start leave it
                
                isAlive.ValueRW.Value = !isAlive.ValueRO.Value;//change appearence
                
                localToWorld.ValueRW = new LocalToWorld()
                {
                    Value = float4x4.TRS(newPosition, quaternion.identity, new float3(1f, 1f, 1f))
                };
                
                state.EntityManager.SetComponentEnabled<FlipCellState>(entity, false);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}