using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    // Objek yang akan diikuti oleh kamera (Wajib di-drag dari Inspector)
    public Transform target;
    
    public Vector3 offset = new Vector3(0f, 10f, 0f); // <-- UBAH DI SINI!
    
    [Range(0.1f, 1f)]
    public float smoothSpeed = 0.125f;

    // ... (Sisa kode LateUpdate tetap sama)
    
    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogError("Target Transform belum di-set di Inspector pada CameraFollowPlayer!");
            return;
        }

        // 1. Hitung Posisi Target (Menggunakan offset baru: 0, 10, 0)
        Vector3 desiredPosition = target.position + offset;

        // 2. Transisi Halus (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 3. Terapkan Posisi Baru
        transform.position = smoothedPosition;

        // 4. Arahkan Kamera ke Pemain (Ini akan otomatis membuat kamera melihat ke bawah)
        transform.LookAt(target);
    }
}