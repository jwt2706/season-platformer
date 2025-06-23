using UnityEngine;

public class CharmHandler : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("charm"))
        {
            GameManager.Instance.AddCharm();
            Destroy(other.gameObject);
        }
    }
}
