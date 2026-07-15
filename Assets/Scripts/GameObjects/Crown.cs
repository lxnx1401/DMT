using UnityEngine;

public class Crown : MonoBehaviour
{
    private bool collected = false;


    private void OnTriggerEnter2D(Collider2D other)
    {

        if (collected)
            return;


        if (other.CompareTag("Player"))
        {

            collected = true;

            GameManager.Instance.CollectCrown();

            Destroy(gameObject);
        }
    }
}