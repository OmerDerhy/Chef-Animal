using UnityEngine;
using System.Collections;

namespace MergesChef.Characters
{
    /// <summary>
    /// Singleton that handles spawning customers.
    /// Manages an array of different customer prefabs to provide visual variety.
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [Header("Spawning Settings")]
        [Tooltip("Drag all the different animal customer prefabs here!")]
        public GameObject[] customerPrefabs;
        
        [Tooltip("The center location where customers will appear when they enter.")]
        public Transform spawnPoint;
        
        [Tooltip("How wide the spawn area is along the X axis. Customers will spawn randomly along this line.")]
        public float spawnWidth = 8f;
        
        [Tooltip("How many seconds to wait after a customer leaves before spawning a new one.")]
        public float respawnDelay = 2f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private Map.Map mapManager;
        private int activeCustomerCount = 0;

        private void Start()
        {
            mapManager = FindObjectOfType<Map.Map>();
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                if (mapManager != null)
                {
                    int targetCustomers = mapManager.GetTotalChairs();
                    if (activeCustomerCount < targetCustomers)
                    {
                        SpawnRandomCustomer();
                        // Short delay before spawning the next one if we are far behind
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }
                }
                yield return new WaitForSeconds(respawnDelay);
            }
        }

        /// <summary>
        /// Called by a Customer script right before it destroys itself.
        /// </summary>
        public void OnCustomerLeft()
        {
            activeCustomerCount--;
            if (activeCustomerCount < 0) activeCustomerCount = 0;
        }

        private void SpawnRandomCustomer()
        {
            if (customerPrefabs == null || customerPrefabs.Length == 0)
            {
                Debug.LogWarning("[CustomerManager] No customer prefabs assigned! Cannot spawn.");
                return;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;

            // Randomize along the X axis so they don't all come from the exact same pixel
            float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
            position.x += randomX;

            // Pick a random prefab from the array
            int randomIndex = Random.Range(0, customerPrefabs.Length);
            GameObject chosenPrefab = customerPrefabs[randomIndex];

            Instantiate(chosenPrefab, position, Quaternion.identity);
            activeCustomerCount++;
            Debug.Log($"[CustomerManager] Spawned a new customer: {chosenPrefab.name}");
        }

        // Draws a green line in the Editor so you can see exactly where the customers will spawn from!
        private void OnDrawGizmosSelected()
        {
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Vector3 left = spawnPoint.position - new Vector3(spawnWidth / 2f, 0, 0);
                Vector3 right = spawnPoint.position + new Vector3(spawnWidth / 2f, 0, 0);
                Gizmos.DrawLine(left, right);
                
                // Draw little spheres at the edges
                Gizmos.DrawSphere(left, 0.2f);
                Gizmos.DrawSphere(right, 0.2f);
            }
        }
    }
}
