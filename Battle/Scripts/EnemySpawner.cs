using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class EnemySpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public static Entity convertedEntity;
    public int maxEntitiesToSpawn;
    public float interval;
    public int entitiesPerInterval;
    public float minSpeed;
    public float maxSpeed;

    private int spawnedEntities;
    private float3 position;
    private float elapsedTime;
    private EntityManager em;
    private BlobAssetStore bas;

    // Start is called before the first frame update
    void Start()
    {
        elapsedTime = 0;
        spawnedEntities = 0;
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        bas = new BlobAssetStore();
        GameObjectConversionSettings gocs = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, bas);
        convertedEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefabToSpawn, gocs);
        em.AddComponent<EnemyComponentData>(convertedEntity);
        em.SetComponentData(convertedEntity, new EnemyComponentData
        {
            speed = UnityEngine.Random.Range(minSpeed, maxSpeed),
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (spawnedEntities < maxEntitiesToSpawn)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= interval)
            {
                elapsedTime = 0;
                for (int i = 0; i <= entitiesPerInterval; i++)
                {
                    if (spawnedEntities >= maxEntitiesToSpawn)
                    {
                        break;
                    }
                    position = (float3)transform.position + (new float3(2.5f*i, 0, 0));
                    em.Instantiate(convertedEntity);
                    em.AddComponent<EnemyComponentData>(convertedEntity);
                    em.SetComponentData(convertedEntity, new Translation { Value = position });
                    em.SetComponentData(convertedEntity, new EnemyComponentData
                    {
                        speed = UnityEngine.Random.Range(minSpeed, maxSpeed)
                    });
                    spawnedEntities++;
                }
            }
        }
    }

    private void OnDestroy()
    {
        bas.Dispose();
    }
}
