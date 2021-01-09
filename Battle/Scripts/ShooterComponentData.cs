using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;

public struct ShooterComponentData : IComponentData
{
    public BlobAssetReference<Collider> colliderCast;
    public float3 projectileSpawnPosition;
    public float firingInterval;
    public Entity projectile;
    public float elapsedTime;
    public float projectileSpeed;
    public float projectileLifeTime;
}
