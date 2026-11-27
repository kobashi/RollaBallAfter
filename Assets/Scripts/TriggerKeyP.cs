using UnityEngine;
using UnityEngine.InputSystem;

public class TriggerKeyP : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (animator != null)
            {
                animator.SetTrigger("keyP");
            }
        }
    }
}
