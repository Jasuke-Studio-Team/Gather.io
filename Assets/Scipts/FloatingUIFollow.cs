using UnityEngine;
using TMPro; // Diperlukan untuk TextMeshPro

public class FloatingUIFollow : MonoBehaviour
{
    // Variabel untuk melacak target (Enemy Leader)
    public Transform targetToFollow;
    
    // Variabel untuk mengatur posisi UI di atas kepala
    public Vector3 offset;
    
    // Referensi ke komponen Teks
    public TextMeshProUGUI countText;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    // Gunakan LateUpdate agar posisi UI diupdate SETELAH target bergerak
    void LateUpdate()
    {
        if (targetToFollow == null)
        {
            // Jika target hancur (misal: musuh kalah), hancurkan UI ini juga
            Destroy(gameObject);
            return;
        }

        // 1. Ambil posisi 3D dunia dari target + offset
        Vector3 targetPos = targetToFollow.position + offset;

        // 2. Ubah posisi 3D dunia menjadi posisi 2D di layar
        Vector2 screenPosition = mainCamera.WorldToScreenPoint(targetPos);

        // 3. Atur posisi RectTransform (UI) ke posisi layar tersebut
        transform.position = screenPosition;
    }

    // Fungsi ini akan dipanggil oleh EnemyLeaderAI untuk memperbarui teks
    public void SetText(string text)
    {
        if (countText != null)
        {
            countText.text = text;
        }
    }
}