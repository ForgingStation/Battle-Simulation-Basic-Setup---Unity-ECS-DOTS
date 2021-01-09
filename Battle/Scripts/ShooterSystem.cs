using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Burst;
using Unity.Jobs;

public class ShooterSystem : SystemBase
{
    public BuildPhysicsWorld bpw;
    public StepPhysicsWorld spw;
    private EndSimulationEntityCommandBufferSystem es_ecb;
    private EndSimulationEntityCommandBufferSystem es_ecb_Job;

    protected override void OnCreate()
    {
        es_ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        es_ecb_Job = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        bpw = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        spw = World.DefaultGameObjectInjectionWorld.GetExistingSystem<StepPhysicsWorld>(); 
    }

    protected override void OnUpdate()
    {
        var ecb = es_ecb.CreateCommandBuffer();
        var parallelECB = ecb.ToConcurrent();
        PhysicsWorld pw = bpw.PhysicsWorld;
        float deltaTime = Time.DeltaTime;
        float3 directionToShoot = float3.zero;
        unsafe
        {
            Entities.ForEach((ref ShooterComponentData scd, ref Translation trans, ref Rotation rot, in LocalToWorld ltw) =>
            {
                scd.elapsedTime += deltaTime;
                ColliderDistanceInput colliderDistanceInput = new ColliderDistanceInput
                {
                    Collider = (Unity.Physics.Collider*)(scd.colliderCast.GetUnsafePtr()),
                    Transform = new RigidTransform(rot.Value, trans.Value),
                    MaxDistance = 0.25f
                };
                if (pw.CalculateDistance(colliderDistanceInput, out DistanceHit hit))
                {
                    directionToShoot = math.normalize(hit.Position - trans.Value);
                    rot.Value = math.slerp(rot.Value, quaternion.LookRotation(directionToShoot, math.up()), deltaTime * 25);
                    scd.projectileSpawnPosition = math.transform(ltw.Value, new float3(0, 0.3f, 0.7f));
                    if (scd.elapsedTime >= scd.firingInterval)
                    { 
                        scd.elapsedTime = 0;
                        Entity projectile = ecb.Instantiate(scd.projectile);
                        ecb.SetComponent(projectile, new Translation { Value = scd.projectileSpawnPosition });
                        ecb.SetComponent(projectile, new Rotation { Value = quaternion.LookRotation(ltw.Forward, math.up()) });
                        ecb.AddComponent<ProjectileFired>(projectile);
                        ecb.SetComponent(projectile, new ProjectileFired
                        {
                            elapsedTime = 0,
                            projectileSpeed = scd.projectileSpeed,
                            projectileLifeTime = scd.projectileLifeTime
                        });
                    }
                }
            }).Run();
        }

        Entities.ForEach((Entity e, int entityInQueryIndex, ref ProjectileFired pf, ref Translation trans, in LocalToWorld ltw) =>
        {
            pf.elapsedTime += deltaTime;
            trans.Value += ltw.Forward * pf.projectileSpeed * deltaTime;
            if (pf.elapsedTime > pf.projectileLifeTime)
            {
                parallelECB.DestroyEntity(entityInQueryIndex, e);
            }
        }).ScheduleParallel();

        JobHandle jh = new ProjectileCollisionJob()
        {
            enemyGroup = GetComponentDataFromEntity<EnemyComponentData>(),
            activeProjectileGroup = GetComponentDataFromEntity<ProjectileFired>(),
            ecb = es_ecb_Job.CreateCommandBuffer()
        }.Schedule(spw.Simulation, ref bpw.PhysicsWorld, Dependency);

        es_ecb.AddJobHandleForProducer(Dependency);
        es_ecb_Job.AddJobHandleForProducer(jh);
    }

    [BurstCompile]
    public struct ProjectileCollisionJob : ICollisionEventsJob
    {
        public ComponentDataFromEntity<EnemyComponentData> enemyGroup;
        public ComponentDataFromEntity<ProjectileFired> activeProjectileGroup;
        public EntityCommandBuffer ecb;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.Entities.EntityA;
            Entity entityB = collisionEvent.Entities.EntityB;
            if (entityA!=Entity.Null && entityB!=Entity.Null)
            {
                bool isBodyAProjectile = activeProjectileGroup.Exists(entityA);
                bool isBodyBProjectile = activeProjectileGroup.Exists(entityB);
                bool isBodyAEnemy = enemyGroup.Exists(entityA);
                bool isBodyBEnemy = enemyGroup.Exists(entityB);

                if (isBodyAProjectile && isBodyBEnemy)
                {
                    ecb.DestroyEntity(entityA);
                    ecb.DestroyEntity(entityB);
                }
                if (isBodyBProjectile && isBodyAEnemy)
                {
                    ecb.DestroyEntity(entityB);
                    ecb.DestroyEntity(entityA);
                }
            }
        }
    }
}

public struct ProjectileFired : IComponentData
{
    public float projectileSpeed;
    public float projectileLifeTime;
    public float projectileImpulseForce;
    public float elapsedTime;
}



