using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;

public class EntitySpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public static Entity convertedEntity;
    public Mesh colliderMesh;
    public float colliderRadius;
    public float projectileFiringInterval;
    public float projectileSpeed;
    public float projectileLifeTime;
    public GameObject projectilePrefab;
    public int numberOfShooters;

    private EntityManager em;
    private BlobAssetStore bas;
    private BlobAssetReference<Unity.Physics.Collider> col;
    private Entity projectileEntity;
    private EntityQuery projectileQuery;
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
            col = CreateSphereCollider(colliderMesh);
        }
        em.AddComponent<ShooterComponentData>(convertedEntity);
        em.SetComponentData(convertedEntity, new Translation { Value =  transform.position });
        em.SetComponentData(convertedEntity, new ShooterComponentData
        {
            colliderCast = col,
            projectile = projectileEntity,
            firingInterval = projectileFiringInterval,
            projectileSpeed = projectileSpeed,
            projectileLifeTime = projectileLifeTime
        });
        for (int i=0; i<=numberOfShooters; i++)
        {
            em.Instantiate(convertedEntity);
            position = (float3)transform.position + (new float3(1.5f*i, 0, 0));
            em.SetComponentData(convertedEntity, new Translation { Value = position });
        }
    }

    private BlobAssetReference<Unity.Physics.Collider> CreateSphereCollider(UnityEngine.Mesh mesh)
    {
        Bounds bounds = mesh.bounds;
        CollisionFilter filter = new CollisionFilter()
        {
            BelongsTo = 1<<10,
            CollidesWith = 1<<11
        };

        return Unity.Physics.SphereCollider.Create(new SphereGeometry
        {
            Center = bounds.center,
            Radius = colliderRadius,
        },
        filter);
    }

    private void OnDestroy()
    {
        bas.Dispose();
    }
}
