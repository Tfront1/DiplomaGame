using UnityEngine;

public class UnitCollisionHandler : MonoBehaviour
{
    private float repulsionForce = 2.0f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Unit"))
        {
            Vector2 repulsionDirection = transform.position - collision.transform.position;
            repulsionDirection.Normalize();

            rb.AddForce(repulsionDirection * repulsionForce, ForceMode2D.Impulse);
        }
    }
}