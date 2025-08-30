using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.NPC
{
    // Debug: érzékelési rádiusz kirajzolása (zöld = nincs szomszéd, piros = van)
    // Play közben a Game nézetben látszik (Debug.DrawLine).
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct NpcDebugDrawSystem : ISystem
    {
        [BurstCompile]
        static int CellKey(float x, float z, float cell)
        {
            int2 c = (int2)math.floor(new float2(x, z) / cell);
            return unchecked((int)math.hash(new int2(c.x * 73856093, c.y * 19349663)));
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NpcSpatialHashConfig>();
            state.RequireForUpdate<NpcTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // gyors kilépés: csak Play-ben rajzoljunk
            if (!Application.isPlaying) return;

            float cell = SystemAPI.GetSingleton<NpcSpatialHashConfig>().CellSize;

            var q = SystemAPI.QueryBuilder().WithAll<NpcTag, LocalTransform, NpcAvoidanceSettings>().Build();
            var ents = q.ToEntityArray(Allocator.Temp);
            var poses = q.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var avs = q.ToComponentDataArray<NpcAvoidanceSettings>(Allocator.Temp);
            int n = ents.Length;
            if (n == 0) { ents.Dispose(); poses.Dispose(); avs.Dispose(); return; }

            // spatial hash
            var grid = new NativeParallelMultiHashMap<int, int>(n, Allocator.Temp);
            for (int i = 0; i < n; i++)
            {
                float3 p = poses[i].Position;
                grid.Add(CellKey(p.x, p.z, cell), i);
            }

            // minden NPC: nézzük van-e szomszéd, és rajzoljunk kört
            for (int i = 0; i < n; i++)
            {
                float3 pos = poses[i].Position;
                float r = avs[i].DetectRadius;
                bool sensed = false;

                int2 c = (int2)math.floor(new float2(pos.x, pos.z) / cell);
                for (int dz = -1; dz <= 1 && !sensed; dz++)
                    for (int dx = -1; dx <= 1 && !sensed; dx++)
                    {
                        int key = CellKey(pos.x + dx * cell, pos.z + dz * cell, cell);
                        NativeParallelMultiHashMapIterator<int> it;
                        int idx;
                        if (grid.TryGetFirstValue(key, out idx, out it))
                        {
                            do
                            {
                                if (idx == i) continue;
                                float3 op = poses[idx].Position;
                                float d2 = math.lengthsq(new float3(op.x - pos.x, 0, op.z - pos.z));
                                if (d2 < r * r) { sensed = true; break; }
                            }
                            while (grid.TryGetNextValue(out idx, ref it));
                        }
                    }

                DrawCircleXZ(pos, r, sensed ? Color.red : Color.green, 32);
                // opcionális kis „fej” jel
                Debug.DrawLine((Vector3)pos, (Vector3)(pos + new float3(0, 0.5f, 0)), Color.gray, 0, false);
            }

            grid.Dispose();
            ents.Dispose();
            poses.Dispose();
            avs.Dispose();
        }

        static void DrawCircleXZ(float3 center, float radius, Color color, int segments)
        {
            if (radius <= 0f || segments < 6) return;
            float3 prev = center + new float3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float t = (i / (float)segments) * math.PI * 2f;
                float3 cur = center + new float3(math.cos(t) * radius, 0, math.sin(t) * radius);
                Debug.DrawLine((Vector3)prev, (Vector3)cur, color, 0, false);
                prev = cur;
            }
        }
    }
}
