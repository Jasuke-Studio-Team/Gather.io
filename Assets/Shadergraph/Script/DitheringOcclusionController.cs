using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DitheringOcclusionController : MonoBehaviour
{
    [Header("Referensi Utama")]
    public Transform playerTransform;
    public LayerMask obstacleLayer; // Layer yang dianggap sebagai penghalang (Misal: Layer "Obstacle")
    public Material ditherMaterial; // Material Dithering dari Shader Graph yang sudah kita buat

    [Header("Pengaturan Tambahan")]
    public float raycastPadding = 0.5f; // Jarak tambahan Raycast melewati Player (agar objek dibelakang Player ikut ter-check)

    private Camera mainCamera;
    // Set untuk melacak Renderer mana saja yang sedang di-dither (transparan)
    private HashSet<Renderer> currentlyDithered = new HashSet<Renderer>();
    // Dictionary untuk menyimpan Material asli dari setiap Renderer
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    // ID untuk property di Shader Graph. Wajib sama persis!
    private static readonly int PlayerPositionID = Shader.PropertyToID("_PlayerPosition");

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[DitheringController] Tidak menemukan Main Camera di Scene.");
            enabled = false;
        }
        if (playerTransform == null || ditherMaterial == null)
        {
            Debug.LogError("[DitheringController] Player Transform atau Dither Material belum diatur.");
            enabled = false;
        }
    }

    void Update()
    {
        // 1. UPDATE POSISI PLAYER GLOBAL
        // Kirim posisi Player ke Shader. Ini akan berlaku untuk SEMUA objek yang menggunakan ditherMaterial.
        // Logika IF (apakah objek terhalang) tetap diatur oleh Shader Graph (Distance Check).
        ditherMaterial.SetVector(PlayerPositionID, playerTransform.position);

        // 2. PERSIAPAN RAYCAST
        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 playerPos = playerTransform.position;
        
        // Menentukan arah Raycast: Dari Kamera menuju Player
        Vector3 direction = (playerPos - cameraPos).normalized;
        // Jarak Raycast: Sampai ke Player + padding (agar mencakup objek di belakang Player)
        float distance = Vector3.Distance(cameraPos, playerPos) + raycastPadding;

        // 3. LAKUKAN RAYCAST (Mencari semua Objek di antara Kamera dan Player)
        // Kita menggunakan Physics.RaycastAll untuk mendapatkan semua yang terhalang
        RaycastHit[] hits = Physics.RaycastAll(cameraPos, direction, distance, obstacleLayer);

        // Set sementara untuk menampung objek yang TERTEMBAK di frame ini
        HashSet<Renderer> currentHits = new HashSet<Renderer>();

        // 4. PROSES OBJEK YANG TERKENA RAYCAST (Apply Dithering)
        foreach (var hit in hits)
        {
            Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
            
            if (hitRenderer != null)
            {
                currentHits.Add(hitRenderer);
                
                // Jika Renderer ini BELUM di-dither, kita tukar materialnya
                if (!currentlyDithered.Contains(hitRenderer))
                {
                    // Simpan material asli sebelum ditukar
                    originalMaterials[hitRenderer] = hitRenderer.sharedMaterials;
                    
                    // Terapkan material Dithering ke semua slot material
                    Material[] newMaterials = Enumerable.Repeat(ditherMaterial, hitRenderer.sharedMaterials.Length).ToArray();
                    hitRenderer.sharedMaterials = newMaterials;

                    currentlyDithered.Add(hitRenderer);
                }
            }
        }

        // 5. PEMBERSIHAN (Revert Material pada objek yang TIDAK TERTEMBAK lagi)
        // Buat list dari objek yang harus dikembalikan ke material asli (Objek di currentlyDithered TAPI TIDAK ada di currentHits)
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var renderer in currentlyDithered)
        {
            if (!currentHits.Contains(renderer))
            {
                // Kembalikan material ke material aslinya
                if (originalMaterials.ContainsKey(renderer))
                {
                    renderer.sharedMaterials = originalMaterials[renderer];
                    originalMaterials.Remove(renderer);
                }
                toRemove.Add(renderer);
            }
        }

        // Hapus dari list pelacak
        foreach (var renderer in toRemove)
        {
            currentlyDithered.Remove(renderer);
        }
    }
}