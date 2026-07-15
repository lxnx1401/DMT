using UnityEngine;

public class Crown : MonoBehaviour
{
    private bool collected = false;


    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Berührt von: " + other.name);
        if (collected)
            return;


        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hat Krone eingesammelt!");
            collected = true;

            GameManager.Instance.CollectCrown();

            Destroy(gameObject);
        }
    }
}