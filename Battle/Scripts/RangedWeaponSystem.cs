using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

public class RangedWeaponSystem : SystemBase
{
    public BuildPhysicsWorld bpw;
    private EndSimulationEntityCommandBufferSystem es_ecb;

    protected override void OnCreate()
    {
        es_ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        bpw = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
    }
    protected override void OnUpdate()
    {
        PhysicsWorld pw = bpw.PhysicsWorld;
        float deltaTime = Time.DeltaTime;
        var parallelECB = es_ecb.CreateCommandBuffer().ToConcurrent();
        unsafe
        {
            Entities.ForEach((Entity e, int entityInQueryIndex, ref RangedWeaponParentData rwpd, ref Translation trans, ref Rotation rot, in LocalToWorld ltw) =>
            {
                rwpd.elapsedTime += deltaTime;
                float3 directionToShoot = float3.zero;
                ColliderDistanceInput colliderDistanceInput = new ColliderDistanceInput
                {
                    Collider = (Unity.Physics.Collider*)(rwpd.colliderCast.GetUnsafePtr()),
                    Transform = new RigidTransform(rot.Value, trans.Value),
                    MaxDistance = 0.25f
                };
                if (pw.CalculateDistance(colliderDistanceInput, out DistanceHit hit))
                {
                    rwpd.currentTargetDistance = math.length(hit.Position - trans.Value);
                    directionToShoot = math.normalize(hit.Position - trans.Value);
                    rot.Value = math.slerp(rot.Value, quaternion.LookRotation(directionToShoot, math.up()), deltaTime * 5);
                    if (rwpd.currentTargetDistance >= 1)
                    {
                        float height = rwpd.currentTargetDistance / 4;
                        float denom = math.sqrt((2 * height) / 9.8f);
                        rwpd.initialVelocity = new float3(0,
                                                math.sqrt(2 * 9.8f * height),
                                                rwpd.currentTargetDistance / (2 * denom));
                        rwpd.initialVelocity = math.mul(quaternion.LookRotation(directionToShoot, math.up()), rwpd.initialVelocity);
                        if (rwpd.elapsedTime >= rwpd.firingInterval)
                        {
                            rwpd.elapsedTime = 0;
                            Entity defEntity = parallelECB.Instantiate(entityInQueryIndex, rwpd.cannonBall);
                            float3 spawnPosition = math.transform(ltw.Value, new float3(0, 5.5f, 0));
                            parallelECB.SetComponent<Translation>(entityInQueryIndex, defEntity, new Translation { Value = spawnPosition });
                            parallelECB.AddComponent<CannonBallTag>(entityInQueryIndex, defEntity);
                            parallelECB.SetComponent<CannonBallTag>(entityInQueryIndex, defEntity, new CannonBallTag
                            {
                                initialVelocity = rwpd.initialVelocity,
                                cannonBallColliderCast = rwpd.cannonBallColliderCast
                            });
                        }
                    }
                }
            }).ScheduleParallel();
        }

        Entities.ForEach((ref CannonComponentData ccd, ref Rotation rot, ref Translation trans, in LocalToWorld ltw, in Parent p) =>
        {
            if (HasComponent<RangedWeaponParentData>(p.Value))
            {
                RangedWeaponParentData rwpd = GetComponent<RangedWeaponParentData>(p.Value);
                if (rwpd.currentTargetDistance >= 1)
                {
                    float3 localUpDirection = math.transform(math.inverse(ltw.Value), ltw.Up);
                    float angle = math.acos(math.dot(localUpDirection, rwpd.initialVelocity) / (math.length(localUpDirection) * (math.length(rwpd.initialVelocity))));
                    rot.Value = math.slerp(rot.Value, quaternion.Euler(angle, 0, 0), deltaTime * 5);
                }
            }
        }).ScheduleParallel();

        Entities.ForEach((ref CannonBallTag cbt, ref PhysicsVelocity pv) =>
        {
            if (!cbt.impulseAdded)
            {
                cbt.impulseAdded = true;
                pv.Linear = cbt.initialVelocity;
                //Unity.Physics.Extensions.ComponentExtensions.ApplyImpulse(ref pv, pm, trans, rot, cbt.initialVelocity, cbt.point);
            }
        }).ScheduleParallel();

        unsafe
        {
            Entities.ForEach((Entity e, int entityInQueryIndex, ref CannonBallTag cbt, ref Translation trans, ref Rotation rot, ref PhysicsVelocity pv) =>
            {
                NativeList<DistanceHit> allEnemyHits = new NativeList<DistanceHit>(Allocator.Temp);
                ColliderDistanceInput colliderDistanceInput = new ColliderDistanceInput
                {
                    Collider = (Unity.Physics.Collider*)(cbt.cannonBallColliderCast.GetUnsafePtr()),
                    Transform = new RigidTransform(rot.Value, trans.Value),
                    MaxDistance = 0.25f
                };
                pw.CalculateDistance(colliderDistanceInput, ref allEnemyHits);
                if (allEnemyHits.Length > 0)
                {
                    for (int i = 0; i < allEnemyHits.Length; i++)
                    {
                        parallelECB.DestroyEntity(entityInQueryIndex, allEnemyHits[i].Entity);
                    }
                }
                allEnemyHits.Dispose();
            }).ScheduleParallel();
        }
        es_ecb.AddJobHandleForProducer(Dependency);
    }
}
public struct CannonBallTag : IComponentData
{
    public float3 initialVelocity;
    public bool impulseAdded;
    public BlobAssetReference<Collider> cannonBallColliderCast;
}
