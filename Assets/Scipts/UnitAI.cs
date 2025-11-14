using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitAI : MonoBehaviour, IPooledObject // Implementasi interface
{
    public enum State { Neutral, Following }
    public State currentState = State.Neutral;
    
    public NavMeshAgent agent;
    public Transform followTarget;
    
    // Warna netral (akan kita atur saat reset)
    public Color neutralColor = Color.white; 

    void Awake()
    {
        // Ambil komponen sekali saja saat Awake
        agent = GetComponent<NavMeshAgent>();
    }

    // OnEnable dipanggil SETIAP KALI SetActive(true) dipanggil oleh pooler
    void OnEnable()
    {
        // Ini adalah implementasi dari IPooledObject
        // Kita tidak perlu memanggilnya secara manual
    }

    // Ini adalah fungsi dari IPooledObject
    // Ini akan dipanggil oleh pooler TEPAT saat di-spawn
    public void OnObjectSpawn()
    {
        // Reset status
        currentState = State.Neutral;
        followTarget = null;
        gameObject.tag = "NeutralNPC"; // Pastikan tag-nya kembali netral
        
        // Reset visual
        GetComponent<Renderer>().material.color = neutralColor;

        // Jika agent valid, suruh dia jalan-jalan
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            Wander();
        }
        else if (agent == null)
        {
            Debug.LogError("UnitAI tidak punya NavMeshAgent!", this);
        }
    }

    void Update()
    {
        if (currentState == State.Following && followTarget != null)
        {
            // Terus update destinasi ke target
            if (agent.isOnNavMesh && agent.isStopped == false)
            {
                agent.SetDestination(followTarget.position);
            }
        }
        
        // Jika dalam mode Neutral dan sudah sampai tujuan, cari tujuan baru
        if (currentState == State.Neutral && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Wander();
        }
    }

    // Fungsi untuk jalan-jalan random
    public void Wander()
    {
        if (!agent.isOnNavMesh) return;
        Vector3 randomDirection = Random.insideUnitSphere * 20f; // 20f = radius area
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas);
        agent.SetDestination(hit.position);
    }

    // Fungsi ini akan dipanggil oleh Player/AI Leader saat merekrut
    public void GetRecruited(Transform leader, Color newColor)
    {
        currentState = State.Following;
        followTarget = leader;
        GetComponent<Renderer>().material.color = newColor;

        // Hentikan pathfinding acak
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }
    
    // Fungsi statis helper (tidak berubah, tapi penting)
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}

