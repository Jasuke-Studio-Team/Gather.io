using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Gunakan singleton agar mudah diakses dari skrip lain
    public static GameManager Instance;

    [Header("Spawner Settings")]
    public List<Transform> spawnPoints; // Daftar semua titik spawn
    public string npcPoolTag = "NPC"; // Tag pool yang akan kita gunakan
    public float spawnInterval = 3.0f; // Seberapa sering memunculkan NPC baru (detik)
    public int maxNeutralNPCs = 50; // Jumlah maksimum NPC netral di peta

    // Daftar untuk melacak NPC yang aktif dan netral
    private List<GameObject> activeNeutralNPCs = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Mulai Coroutine untuk memunculkan NPC
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true) // Loop selamanya
        {
            // Cek dan bersihkan NPC yang sudah tidak netral (sudah direkrut)
            // Kita loop terbalik untuk menghapus dari list dengan aman
            for (int i = activeNeutralNPCs.Count - 1; i >= 0; i--)
            {
                if (activeNeutralNPCs[i] == null || !activeNeutralNPCs[i].activeSelf || activeNeutralNPCs[i].CompareTag("NeutralNPC") == false)
                {
                    activeNeutralNPCs.RemoveAt(i);
                }
            }

            // Jika jumlah NPC netral di peta kurang dari maksimum...
            if (activeNeutralNPCs.Count < maxNeutralNPCs)
            {
                // ...coba spawn yang baru
                SpawnNPC();
            }
            
            // Tunggu sebelum cek lagi
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnNPC()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("Tidak ada spawn point terdaftar di GameManager!");
            return;
        }

        // 1. Pilih titik spawn acak
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        // 2. Minta NPC dari Object Pooler
        GameObject spawnedNPC = ObjectPooler.Instance.SpawnFromPool(npcPoolTag, randomSpawnPoint.position, randomSpawnPoint.rotation);

        // 3. Jika berhasil (pool tidak kosong)
        if (spawnedNPC != null)
        {
            // Pastikan tag-nya NeutralNPC saat spawn
            spawnedNPC.tag = "NeutralNPC"; 
            
            // Tambahkan ke daftar pelacak kita
            activeNeutralNPCs.Add(spawnedNPC);
        }
    }
}
