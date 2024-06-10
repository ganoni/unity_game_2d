using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private int speed = 5;
    private Vector2 movement;
    private Rigidbody2D rb;
    public CoinManager cm;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public void Update()
    {
        if (cm.coinCount < 0)
            SceneManager.LoadScene("perd");
    }
    private void OnMovement(InputValue value)
    {
        movement = value.Get<Vector2>();
    }

    private void FixedUpdate()
    {
        // Déplacement du joueur
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Collision detected with: " + other.gameObject.name);

        // Vérifiez si l'objet en collision a le tag 'coin'
        if (other.gameObject.CompareTag("coin"))
        {
            Debug.Log("Coin collected");
            Destroy(other.gameObject);
            cm.coinCount++;
            Debug.Log("Coins collected: " + cm.coinCount);
        }
        // Vérifiez si l'objet en collision a le tag 'enemy'
        else if (other.gameObject.CompareTag("enemy"))
        {
            Debug.Log("Enemy hit");
            cm.coinCount--;
            Debug.Log("Coins remaining: " + cm.coinCount);
        }
    }
}
