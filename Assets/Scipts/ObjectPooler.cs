using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    // Kelas ini menyimpan data untuk satu jenis pool
    [System.Serializable]
    public class Pool
    {
        public string tag; // Tag untuk prefab ini (misal: "NPC" atau "Bullet")
        public GameObject prefab;
        public int size; // Jumlah yang akan dibuat di awal
    }

    // Singleton Pattern
    public static ObjectPooler Instance;
    void Awake()
    {
        Instance = this;
    }

    public List<Pool> pools; // Daftar semua pool yang kita kelola
    
    // Dictionary untuk menyimpan semua antrian (Queue) objek
    // Key: string (tag), Value: Antrian GameObject
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // Buat semua objek untuk setiap pool
        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectQueue = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false); // Sembunyikan
                objectQueue.Enqueue(obj); // Masukkan ke antrian
            }

            poolDictionary.Add(pool.tag, objectQueue);
        }
    }

    // Fungsi untuk mengambil objek dari pool
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool dengan tag " + tag + " tidak ada.");
            return null;
        }

        // Cek apakah pool masih punya objek
        if (poolDictionary[tag].Count == 0)
        {
            // Opsi: Anda bisa menambah pool di sini jika habis
            // Untuk game ini, kita biarkan saja (berarti semua NPC sedang dipakai)
            return null; 
        }

        // Ambil objek dari antrian
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true); // Aktifkan!
        
        // Panggil OnObjectSpawn jika ada (berguna untuk reset)
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    // --- FUNGSI BARU ---
    // Fungsi untuk mengembalikan objek ke pool
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool dengan tag " + tag + " tidak ada.");
            return;
        }

        objectToReturn.SetActive(false); // Sembunyikan
        poolDictionary[tag].Enqueue(objectToReturn); // Masukkan kembali ke antrian
    }
}

// Interface (kontrak) opsional untuk mereset objek saat di-spawn
public interface IPooledObject
{
    void OnObjectSpawn();
}

