using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;

public class EnemySystem : SystemBase
{
    public float3 shooterPosition;

    protected override void OnCreate()
    {
        shooterPosition = float3.zero;
    }
    protected override void OnUpdate()
    {
        Entities.WithoutBurst().ForEach((in ShooterComponentData scd, in Translation trans) =>
        {
            shooterPosition = trans.Value;
        }).Run();

        float3 sh = shooterPosition;
        float deltaTime = Time.DeltaTime;
        Entities.ForEach((ref EnemyComponentData ecd, ref Translation trans, ref Rotation rot) =>
        {
            float3 diff = math.normalize(sh - trans.Value);
            rot.Value = math.slerp(rot.Value, quaternion.LookRotation(diff, math.up()), deltaTime);
            trans.Value += diff * ecd.speed * deltaTime;
        }).ScheduleParallel();
    } 
}
