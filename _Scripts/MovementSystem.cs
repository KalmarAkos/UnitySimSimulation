using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // Kamera síkbeli tengelyei (playernél kamera-relatív mozgáshoz)
        float3 camF = new float3(0, 0, 1);
        float3 camR = new float3(1, 0, 0);
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0; r.Normalize();
            camF = new float3(f.x, f.y, f.z);
            camR = new float3(r.x, r.y, r.z);
        }

        foreach (var (lt, vel, ms, gs, jumpData, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<Velocity>,
                                 RefRO<MoveSettings>, RefRW<GroundState>, RefRO<PlayerJumpData>>()
                          .WithEntityAccess())
        {
            float3 pos = lt.ValueRO.Position;
            float3 v = vel.ValueRO.Value;

            // --- Globális NaN/Inf szanálás a biztonság kedvéért
            if (!math.all(math.isfinite(pos))) pos = float3.zero;
            if (!math.all(math.isfinite(v))) v = float3.zero;

            // 1) Bemenet / AI → kívánt irány + ugrás kérés
            GetDesired(ref state, entity, camR, camF,
                       out float3 desiredDirWorld,
                       out byte jumpRequest);

            bool hasInput = math.lengthsq(desiredDirWorld) > 1e-6f;
            bool grounded = gs.ValueRO.IsGrounded == 1;
            float footHitY = gs.ValueRO.LastHitY;

            // 2) Planáris (XZ) sebesség frissítése – külön ág nincs/van inputra
            float3 planar = new float3(v.x, 0, v.z);
            planar = UpdatePlanarVelocity(planar, desiredDirWorld, ms.ValueRO, grounded, dt, hasInput);

            // 3) Ugrás + gravitáció (Y)
            float yVel = UpdateVerticalVelocity(v.y, jumpRequest, grounded, jumpData.ValueRO, dt);

            // 4) Integráció
            float3 total = new float3(planar.x, yVel, planar.z);
            pos += total * dt;

            // 5) Talaj clamp (ha grounded)
            if (grounded && pos.y < footHitY + gs.ValueRO.FootOffset)
            {
                pos.y = footHitY + gs.ValueRO.FootOffset;
                if (yVel < 0) yVel = 0;
            }

            // 6) Forgatás mozgás irányába (SAFE – sose adunk 0/NaN vektort)
            float planarLen2 = math.lengthsq(planar);
            if (planarLen2 > 1e-6f && math.all(math.isfinite(planar)))
            {
                float3 fwd = planar / math.sqrt(planarLen2);      // kézi normalize
                if (!math.all(math.isfinite(fwd)) || math.lengthsq(fwd) < 0.5f)
                    fwd = new float3(0, 0, 1);                    // fallback

                var target = quaternion.LookRotationSafe(fwd, math.up());
                lt.ValueRW.Rotation = math.slerp(lt.ValueRO.Rotation, target, ms.ValueRO.RotateSpeed * dt);
            }

            // 7) Visszaírás (még egy szűrés)
            if (!math.all(math.isfinite(pos))) pos = float3.zero;
            if (!math.all(math.isfinite(planar))) planar = float3.zero;
            if (!math.isfinite(yVel)) yVel = 0;

            lt.ValueRW.Position = pos;
            vel.ValueRW.Value = new float3(planar.x, yVel, planar.z);
        }
    }

    // ----------------- Helpers -----------------

    // Itt NEM hívunk SystemAPI-t, hanem a state.EntityManager-t → nincs SGSG0002.
    private static void GetDesired(ref SystemState state, Entity e, float3 camR, float3 camF,
                                   out float3 desiredDirWorld, out byte jumpRequest)
    {
        var em = state.EntityManager;

        desiredDirWorld = float3.zero;
        jumpRequest = 0;

        if (em.HasComponent<PlayerTag>(e))
        {
            var input = em.GetComponentData<ControlInput>(e);
            bool camRel = (input.CameraRelative == 1);

            if (camRel)
                desiredDirWorld = math.normalize(camR * input.Move.x + camF * input.Move.y);
            else
                desiredDirWorld = math.normalize(new float3(input.Move.x, 0, input.Move.y));

            if (!math.any(desiredDirWorld)) desiredDirWorld = float3.zero;
            jumpRequest = input.Jump;
        }
        else if (em.HasComponent<NpcTag>(e))
        {
            var ai = em.GetComponentData<DesiredMove>(e);
            desiredDirWorld = math.lengthsq(ai.WorldDir) > 1e-6f ? math.normalize(ai.WorldDir) : float3.zero;
            jumpRequest = ai.Jump;
        }
    }

    private static float3 UpdatePlanarVelocity(float3 planar,
                                               float3 desiredDirWorld,
                                               MoveSettings ms,
                                               bool grounded,
                                               float dt,
                                               bool hasInput)
    {
        float3 desiredVel = desiredDirWorld * ms.MaxSpeed;

        float accel = grounded ? ms.Accel : ms.Accel * ms.AirControl;
        float decel = grounded ? ms.Decel : ms.Decel * ms.AirControl;

        // NINCS INPUT → decellel húzzuk nullára (nem csúszik)
        if (!hasInput)
        {
            float3 delta = -planar;
            float dLen = math.length(delta);
            if (dLen <= 1e-5f) return float3.zero;

            float maxDelta = decel * dt;
            return (dLen > maxDelta) ? (planar + (delta / dLen) * maxDelta) : float3.zero;
        }

        // VAN INPUT → gyorsulás, ellentétes irányban először decel
        {
            float3 delta = desiredVel - planar;
            float dLen = math.length(delta);
            float maxStep = ((math.lengthsq(desiredVel) > 0 && math.dot(planar, desiredVel) < 0)
                              ? decel : accel) * dt;

            if (dLen > 1e-5f)
                return (dLen > maxStep) ? (planar + (delta / dLen) * maxStep) : desiredVel;

            return planar;
        }
    }

    private static float UpdateVerticalVelocity(float yVel,
                                                byte jumpRequest,
                                                bool grounded,
                                                PlayerJumpData jumpData,
                                                float dt)
    {
        if (grounded && yVel < 0) yVel = -2f; // talajhoz tapadás
        if (grounded && jumpRequest == 1)
            yVel = math.sqrt(jumpData.JumpForce * -2f * jumpData.Gravity);

        yVel += jumpData.Gravity * dt;
        return yVel;
    }
}
