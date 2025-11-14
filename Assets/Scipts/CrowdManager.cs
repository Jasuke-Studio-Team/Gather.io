using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Diperlukan untuk List<>
using TMPro; // Diperlukan untuk TextMeshPro

/*
 * Skrip ini dipasang pada PlayerLeader DAN EnemyLeader.
 * Tugasnya mengelola list pengikut dan logika tempur.
 */
public class CrowdManager : MonoBehaviour
{
    [Header("Team Settings")]
    public Color teamColor; // Warna untuk tim ini
    public string leaderTag; // Tag untuk leader ini (misal: "PlayerLeader")
    public string followerTag; // Tag untuk followernya (misal: "PlayerFollower")
    public string enemyLeaderTag; // Tag untuk leader musuh (misal: "EnemyLeader")
    public string enemyFollowerTag; // Tag untuk follower musuh (misal: "EnemyFollower")

    [Header("UI")]
    public TextMeshProUGUI crowdCountText; // Referensi ke UI Text di Canvas

    // Daftar yang menyimpan semua pengikut kita
    public List<GameObject> followers = new List<GameObject>();

    // Variabel untuk mencegah 'tempur' berkali-kali dalam satu tabrakan
    private bool isFighting = false; 

    void Update()
    {
        // Update UI secara terus menerus
        if (crowdCountText != null)
        {
            // Jumlah total = pengikut + 1 (diri sendiri)
            crowdCountText.text = (followers.Count + 1).ToString();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. Cek untuk merekrut NPC netral
        if (other.CompareTag("NeutralNPC"))
        {
            UnitAI unitAI = other.GetComponent<UnitAI>();
            // Hanya rekrut jika dia netral
            if (unitAI != null && unitAI.currentState == UnitAI.State.Neutral)
            {
                RecruitUnit(other.gameObject);
            }
        }

        // 2. Cek untuk "tempur" (combat)
        // Kita bisa menabrak leadernya ATAU salah satu pengikutnya
        if (other.CompareTag(enemyLeaderTag) || other.CompareTag(enemyFollowerTag))
        {
            // Dapatkan CrowdManager dari musuh
            CrowdManager enemyCrowd = null;

            if (other.CompareTag(enemyLeaderTag))
            {
                // Kita menabrak leadernya langsung
                enemyCrowd = other.GetComponent<CrowdManager>();
            }
            else // Kita menabrak followernya
            {
                // Cari leader dari follower tersebut
                UnitAI enemyUnit = other.GetComponent<UnitAI>();
                if (enemyUnit != null && enemyUnit.followTarget != null)
                {
                    enemyCrowd = enemyUnit.followTarget.GetComponent<CrowdManager>();
                }
            }

            // Jika kita berhasil menemukan CrowdManager musuh dan kita TIDAK sedang sibuk tempur
            if (enemyCrowd != null && !isFighting)
            {
                // Mulai tempur
                CompareCrowds(enemyCrowd);
            }
        }
    }

    // Fungsi untuk merekrut 1 unit
    void RecruitUnit(GameObject unit)
    {
        UnitAI unitAI = unit.GetComponent<UnitAI>();
        
        // 1. Panggil fungsi di UnitAI untuk mengubah status, warna, dan target
        unitAI.GetRecruited(this.transform, teamColor); 
        
        // 2. Tambahkan ke list kita
        followers.Add(unit);
        
        // 3. Ubah tag-nya!
        unit.tag = followerTag;
    }

    // Fungsi inti untuk "tempur"
    void CompareCrowds(CrowdManager otherLeader)
    {
        // Tandai bahwa kita sedang tempur agar fungsi ini tidak dipanggil berkali-kali
        isFighting = true;
        otherLeader.isFighting = true;

        int myCount = followers.Count;
        int enemyCount = otherLeader.followers.Count;

        // KITA MENANG
        if (myCount > enemyCount)
        {
            Debug.Log(gameObject.name + " menang melawan " + otherLeader.gameObject.name);

            // Ambil alih semua pengikut musuh
            // Kita pakai loop 'for' terbalik karena kita akan memodifikasi list
            for (int i = otherLeader.followers.Count - 1; i >= 0; i--)
            {
                GameObject unitToSteal = otherLeader.followers[i];
                otherLeader.followers.RemoveAt(i); // Hapus dari list musuh
                RecruitUnit(unitToSteal); // Tambahkan ke list kita
            }
            
            // --- INI JAWABANNYA ---
            // Kalahkan leader musuh
            otherLeader.Defeat();
        }
        // KITA KALAH
        else if (myCount < enemyCount)
        {
            Debug.Log(gameObject.name + " kalah melawan " + otherLeader.gameObject.name);

            // Musuh mengambil alih semua pengikut kita
            for (int i = followers.Count - 1; i >= 0; i--)
            {
                GameObject unitToGive = followers[i];
                followers.RemoveAt(i); // Hapus dari list kita
                otherLeader.RecruitUnit(unitToGive); // Tambahkan ke list musuh
            }

            // Kalahkan diri kita sendiri
            Defeat();
        }
        // Jika seri, tidak terjadi apa-apa

        // Reset status tempur setelah jeda singkat
        StartCoroutine(ResetFightingStatus(otherLeader));
    }

    // --- FUNGSI INI DIPERBARUI ---
    // Fungsi ini dipanggil saat leader ini dikalahkan
    public void Defeat()
    {
        Debug.Log(gameObject.name + " telah dikalahkan!");

        // --- PEMBARUAN UTAMA ---
        // Kembalikan semua pengikut ke pool agar bisa di-spawn ulang
        for (int i = followers.Count - 1; i >= 0; i--)
        {
            // Panggil ObjectPooler untuk mengembalikan follower ke pool
            // Kita asumsikan tag pool mereka adalah "NPC"
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool("NPC", followers[i]);
            }
        }
        
        // Kosongkan daftar pengikut kita
        followers.Clear();
        // --- SELESAI PEMBARUAN ---
        
        // Menonaktifkan GameObject adalah cara termudah untuk "membunuhnya"
        // Ini juga akan otomatis menghentikan semua skrip di dalamnya.
        gameObject.SetActive(false); 
        
        // Jika Anda ingin mengembalikannya ke pool, Anda bisa panggil ObjectPooler di sini.
        // Tapi untuk leader, menonaktifkan biasanya sudah cukup.
    }

    // Coroutine untuk memberi jeda sebelum bisa tempur lagi
    private IEnumerator ResetFightingStatus(CrowdManager otherLeader)
    {
        // Tunggu 3 detik
        yield return new WaitForSeconds(3.0f);
        
        // Setel ulang status
        isFighting = false;
        if (otherLeader != null)
        {
            otherLeader.isFighting = false;
        }
    }
}

