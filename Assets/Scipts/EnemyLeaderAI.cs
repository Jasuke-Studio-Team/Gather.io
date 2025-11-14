using UnityEngine;
using UnityEngine.AI; // <-- Sangat penting untuk NavMeshAgent
using TMPro; // <-- Diperlukan untuk referensi UI

/*
 * Ini adalah "otak" untuk leader AI musuh.
 * Skrip ini membutuhkan dua komponen lain untuk bekerja:
 * 1. NavMeshAgent: Untuk bergerak di peta.
 * 2. CrowdManager: Untuk mengelola pengikutnya sendiri.
 */
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CrowdManager))]
public class EnemyLeaderAI : MonoBehaviour
{
    // 1. STATE MACHINE (MESIN STATUS)
    // AI akan berada di salah satu status ini.
    private enum AIState
    {
        WanderAndCollect, // Mode: Jalan-jalan acak & mengumpulkan NPC netral
        Hunt              // Mode: Memburu pemain
    }
    
    private AIState currentState; // Menyimpan status AI saat ini

    // 2. REFERENSI KOMPONEN
    // Variabel untuk menyimpan komponen yang akan kita gunakan
    private NavMeshAgent agent;
    private CrowdManager crowdManager;
    private Transform playerTransform; // Untuk melacak posisi pemain

    // 3. PENGATURAN AI (Bisa diubah di Inspector)
    [Header("AI Settings")]
    public float detectionRadius = 20f; // Jarak untuk mendeteksi pemain
    public float wanderRadius = 30f; // Jarak maksimum untuk jalan-jalan
    public float wanderInterval = 7f; // Seberapa sering AI mencari tujuan baru saat wandering

    private float wanderTimer; // Timer internal untuk wandering

    // 4. REFERENSI UI (TAMBAHAN BARU)
    [Header("Floating UI Settings")]
    public GameObject floatingUIPrefab; // Prefab UI yang akan kita buat
    public Vector3 uiOffset = new Vector3(0, 2.5f, 0); // Posisi UI di atas kepala

    private FloatingUIFollow floatingUIScript; // Skrip UI yang kita buat

    void Start()
    {
        // Ambil komponen yang terpasang di GameObject ini
        agent = GetComponent<NavMeshAgent>();
        crowdManager = GetComponent<CrowdManager>();
        
        // Cari GameObject pemain berdasarkan Tag-nya saat game dimulai.
        // PENTING: Pastikan Anda memberi Tag "PlayerLeader" pada GameObject Player Anda.
        GameObject player = GameObject.FindGameObjectWithTag("PlayerLeader");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("EnemyLeaderAI tidak bisa menemukan Player! Pastikan Player memiliki Tag 'PlayerLeader'.");
        }

        // AI akan mulai dengan mode jalan-jalan
        currentState = AIState.WanderAndCollect;

        // --- TAMBAHAN BARU: Buat Floating UI ---
        if (floatingUIPrefab != null)
        {
            // 1. Temukan Canvas utama di scene Anda
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                // 2. Buat (Instantiate) prefab UI sebagai anak dari Canvas
                GameObject uiInstance = Instantiate(floatingUIPrefab, mainCanvas.transform);
                
                // 3. Dapatkan skripnya
                floatingUIScript = uiInstance.GetComponent<FloatingUIFollow>();
                if (floatingUIScript != null)
                {
                    // 4. Beri tahu UI siapa yang harus diikuti
                    floatingUIScript.targetToFollow = this.transform;
                    floatingUIScript.offset = this.uiOffset;
                }
            }
            else
            {
                Debug.LogError("Tidak ada Canvas di scene untuk menempatkan Floating UI.");
            }
        }
    }

    void Update()
    {
        // Jika karena alasan tertentu agent tidak aktif di NavMesh, jangan lakukan apa-apa.
        if (!agent.isOnNavMesh) return;

        // Jika pemain tidak ditemukan (misal: pemain kalah lalu di-nonaktifkan),
        // AI akan kembali ke mode jalan-jalan.
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            currentState = AIState.WanderAndCollect;
        }

        // Ini adalah "otak" utama AI yang memilih status
        // Ini akan menjalankan fungsi yang berbeda tergantung pada statusnya saat ini.
        switch (currentState)
        {
            case AIState.WanderAndCollect:
                DoWanderAndCollect();
                CheckForHunt(); // Sambil jalan-jalan, cek apakah bisa memburu pemain
                break;

            case AIState.Hunt:
                DoHunt();
                CheckForWander(); // Sambil memburu, cek apakah harus berhenti
                break;
        }

        // --- TAMBAHAN BARU: Update Teks di UI ---
        UpdateFloatingUI();
    }

    // --- TAMBAHAN BARU: Fungsi untuk update UI ---
    void UpdateFloatingUI()
    {
        if (floatingUIScript != null && crowdManager != null)
        {
            // Ambil jumlah pengikut dari CrowdManager + 1 (dirinya sendiri)
            int currentCount = crowdManager.followers.Count + 1;
            floatingUIScript.SetText(currentCount.ToString());
        }
    }
    
    // --- TAMBAHAN BARU: Bersihkan UI saat musuh hancur ---
    void OnDestroy()
    {
        // Saat EnemyLeader hancur (misal: kalah), hancurkan juga UI-nya
        if (floatingUIScript != null)
        {
            Destroy(floatingUIScript.gameObject);
        }
    }

    // --- LOGIKA UNTUK SETIAP STATUS ---

    void DoWanderAndCollect()
    {
        // Fungsi ini membuat AI berjalan ke titik acak di NavMesh.
        wanderTimer += Time.deltaTime;

        // Jika sudah waktunya (timer habis) ATAU jika AI sudah sampai di tujuan...
        if (wanderTimer > wanderInterval || (agent.hasPath && agent.remainingDistance < 1f))
        {
            // ...cari posisi acak baru di NavMesh
            // Kita panggil fungsi helper dari UnitAI
            Vector3 newPos = UnitAI.RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            wanderTimer = 0; // Reset timer
        }
    }

    void DoHunt()
    {
        // Fungsi ini membuat AI terus mengejar posisi pemain.
        if (playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }

    // --- LOGIKA UNTUK PERALIHAN STATUS ---

    void CheckForHunt()
    {
        // Cek: Haruskah kita beralih dari Wander ke mode Hunt?
        if (playerTransform == null) return; // Tidak bisa berburu jika tidak ada pemain

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Jika pemain berada dalam jangkauan deteksi...
        if (distanceToPlayer < detectionRadius)
        {
            CrowdManager playerCrowd = playerTransform.GetComponent<CrowdManager>();
            
            // ...DAN jika kita lebih kuat dari pemain, BURU DIA!
            if (playerCrowd != null && crowdManager.followers.Count > playerCrowd.followers.Count)
            {
                currentState = AIState.Hunt;
            }
        }
    }

    void CheckForWander()
    {
        // Cek: Haruskah kita beralih dari Hunt kembali ke mode Wander?
        if (playerTransform == null)
        {
            currentState = AIState.WanderAndCollect;
            return;
        }
        
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        CrowdManager playerCrowdCheck = playerTransform.GetComponent<CrowdManager>();

        // Alasan untuk berhenti berburu:
        // 1. Pemain terlalu jauh (di luar jangkauan deteksi)
        // ATAU
        // 2. Jumlah kerumunan kita menjadi lebih kecil atau sama dengan pemain (kita tidak lagi menang)
        if (dist > detectionRadius * 1.5f || (playerCrowdCheck != null && crowdManager.followers.Count <= playerCrowdCheck.followers.Count))
        {
            currentState = AIState.WanderAndCollect;
        }
    }
}

