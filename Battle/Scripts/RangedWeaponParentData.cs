using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;

public struct RangedWeaponParentData : IComponentData
{
    public BlobAssetReference<Collider> colliderCast;
    public BlobAssetReference<Collider> cannonBallColliderCast;
    public float currentTargetDistance;
    public Entity cannonBall;
    public float firingInterval;
    public float3 initialVelocity;
    public float elapsedTime;
}
