using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public float speed = 10f;
    public int item_count = 12;
    public TextMeshProUGUI countText;
    public GameObject winTextObject;

    private Rigidbody rb;
    private int count;
    private float movementX;
    private float movementY;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        count = 0;
        SetCountText();
        if (winTextObject != null)
        {
            winTextObject.SetActive(false);
        }
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    void SetCountText()
    {
        if (countText != null)
        {
            countText.text = "Count: " + count.ToString();
        }

        if (count >= item_count)
        {
            if (winTextObject != null)
            {
                winTextObject.SetActive(true);
            }

            StartCoroutine(DelayMethod(5.0f, () =>
            {
                int totalScore = PlayerPrefs.GetInt("TotalScore", 0) + count;
                PlayerPrefs.SetInt("TotalScore", totalScore);
                PlayerPrefs.Save();

                int sceneIndex = SceneManager.GetActiveScene().buildIndex;
                SceneManager.LoadScene(sceneIndex + 1);
            }));
        }
    }

    private IEnumerator DelayMethod(float waitTime, Action action)
    {
        yield return new WaitForSeconds(waitTime);
        action();
    }

    private void FixedUpdate()
    {
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement * speed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            other.gameObject.GetComponent<Renderer>().enabled = false;
            other.gameObject.GetComponent<BoxCollider>().enabled = false;
            count = count + 1;
            SetCountText();
        }
    }
}
