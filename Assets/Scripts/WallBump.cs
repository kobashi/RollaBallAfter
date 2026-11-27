using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBump : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision hit)
    {
        if (hit.gameObject.CompareTag("Player"))
        {
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}
