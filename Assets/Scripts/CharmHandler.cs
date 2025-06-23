using UnityEngine;

public class CharmHandler : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("charm"))
        {
            Debug.Log("Collected a charm!");
            Destroy(other.gameObject); // Make the charm disappear
            // You can add score logic or sound here too
        }
    }
}
