using UnityEngine;

// This script requires a CharacterController component on the same GameObject.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Joystick joystick;

    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // Degrees per second

    private CharacterController controller;
    private Animator animator; // Optional: if you have animations

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>(); // Optional
    }

    void Update()
    {
        // Read input from the virtual joystick
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        Vector3 direction = new Vector3(horizontal, 0f, vertical);

        // Move the character if there is input
        if (direction.magnitude >= 0.1f)
        {
            // --- Movement ---
            // Move relative to the camera, but grounded on the XZ plane.
            Vector3 moveDir = new Vector3(direction.x, 0, direction.z);
            controller.Move(moveDir * moveSpeed * Time.deltaTime);

            // --- Rotation ---
            // Rotate the player to face the direction of movement
            Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);

            // Optional: Trigger walk/run animation
            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
            }
        }
        else
        {
            // Optional: Trigger idle animation
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
            }
        }
    }
}