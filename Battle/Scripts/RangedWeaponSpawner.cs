using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;

public class RangedWeaponSpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public static Entity convertedEntity;
    public Mesh colliderMesh;
    public float colliderRadius;
    public float cannonBallColliderRadius;
    public float projectileFiringInterval;
    public float projectileSpeed;
    public float projectileLifeTime;
    public GameObject projectilePrefab;
    public float3 projectileSpawnPosition;
    public int numberOfCannons;

    private EntityManager em;
    private BlobAssetStore bas;
    private BlobAssetReference<Unity.Physics.Collider> col;
    private BlobAssetReference<Unity.Physics.Collider> cannonBallCol;
    private Entity projectileEntity;
    private float3 position;

    // Start is called before the first frame update
    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        bas = new BlobAssetStore();
        GameObjectConversionSettings gocs = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, bas);
        projectileEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(projectilePrefab, gocs);
        convertedEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefabToSpawn, gocs);
        if (colliderMesh != null)
        {
            col = CreateSphereCollider(colliderMesh, colliderRadius);
            cannonBallCol = CreateSphereCollider(colliderMesh, cannonBallColliderRadius);
        }
        em.AddComponent<RangedWeaponParentData>(convertedEntity);
        em.SetComponentData(convertedEntity, new Translation { Value = transform.position });
        em.SetComponentData(convertedEntity, new RangedWeaponParentData
        {
            colliderCast = col,
            cannonBall = projectileEntity,
            firingInterval = projectileFiringInterval,
            cannonBallColliderCast = cannonBallCol
        });
        for (int i = 0; i < numberOfCannons; i++)
        {
            em.Instantiate(convertedEntity);
            position = (float3)transform.position + (new float3(10f * i, 0, 0));
            em.SetComponentData(convertedEntity, new Translation { Value = position });
        }
    }

    private BlobAssetReference<Unity.Physics.Collider> CreateSphereCollider(UnityEngine.Mesh mesh, float colRadius)
    {
        Bounds bounds = mesh.bounds;
        CollisionFilter filter = new CollisionFilter()
        {
            BelongsTo = 1u << 10,
            CollidesWith = 1u << 11
        };

        return Unity.Physics.SphereCollider.Create(new SphereGeometry
        {
            Center = bounds.center,
            Radius = colRadius,
        },
        filter);
    }

    private void OnDestroy()
    {
        bas.Dispose();
    }
}
