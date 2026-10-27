using System.Collections.Generic;
using UnityEngine;

namespace MergesChef.Characters
{
    /// <summary>
    /// Spawns whichever staff the player owns (persisted via PlayerData) as soon as the scene loads.
    /// Mirrors MapInitializer: destroys any Staff left hand-placed in the Editor first so they can't
    /// duplicate the saved roster, then either respawns the saved roster or - on the very first launch,
    /// when there's no saved data yet - grants one starter staff and saves it.
    /// </summary>
    public class StaffManager : MonoBehaviour
    {
        public static StaffManager Instance { get; private set; }

        [Header("Staff Catalog")]
        [Tooltip("Drag every hireable staff prefab here. Each prefab's own name is what gets saved.")]
        public GameObject[] staffPrefabs;

        [Tooltip("Which entry from Staff Prefabs is granted for free on the player's very first launch.")]
        public GameObject starterStaffPrefab;

        [Tooltip("Where staff appear when spawned. Defaults to the world origin if left empty.")]
        public Transform spawnPoint;

        private readonly List<Staff> spawnedStaff = new List<Staff>();

        /// <summary>The staff currently spawned in the world - read this rather than hardcoding a species
        /// anywhere else (HUD portraits, etc.), so everything stays in sync with what the player owns.</summary>
        public IReadOnlyList<Staff> SpawnedStaff => spawnedStaff;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Clean up any manually placed testing staff in the scene so they don't duplicate the saved roster!
            var oldStaff = FindObjectsOfType<Staff>();
            foreach (var s in oldStaff) Destroy(s.gameObject);

            string saved = PlayerData.Instance != null ? PlayerData.Instance.LoadStaffData() : "";

            if (!string.IsNullOrEmpty(saved))
            {
                foreach (string prefabName in saved.Split(','))
                {
                    SpawnStaffByName(prefabName);
                }
            }
            else if (starterStaffPrefab != null)
            {
                // First-ever launch: grant and save exactly one starter staff.
                SpawnStaff(starterStaffPrefab);
                SaveOwnedStaff();
            }
        }

        private Vector3 SpawnPosition()
        {
            return spawnPoint != null ? spawnPoint.position : Vector3.zero;
        }

        private void SpawnStaffByName(string prefabName)
        {
            foreach (var prefab in staffPrefabs)
            {
                if (prefab != null && prefab.name == prefabName)
                {
                    SpawnStaff(prefab);
                    return;
                }
            }
            Debug.LogWarning($"[StaffManager] No staff prefab named '{prefabName}' in the catalog - skipping.");
        }

        private void SpawnStaff(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, SpawnPosition(), Quaternion.identity);
            Staff staff = instance.GetComponent<Staff>();
            if (staff != null) spawnedStaff.Add(staff);
        }

        private void SaveOwnedStaff()
        {
            if (PlayerData.Instance == null) return;

            var names = new List<string>();
            foreach (var s in spawnedStaff)
            {
                if (s != null) names.Add(s.gameObject.name.Replace("(Clone)", ""));
            }
            PlayerData.Instance.SaveStaffData(string.Join(",", names));
        }

        /// <summary>Call this from a future "hire staff" purchase flow to add and persist one more.</summary>
        public void HireStaff(GameObject prefab)
        {
            SpawnStaff(prefab);
            SaveOwnedStaff();
        }
    }
}
