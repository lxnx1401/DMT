using UnityEngine;

public class Crown : MonoBehaviour
{
    private bool collected = false;
    [SerializeField] private AudioClip crownCollectSound;



    private void OnTriggerEnter2D(Collider2D other)
    {

        if (collected)
            return;


        if (other.CompareTag("Player"))
        {

            collected = true;

            AudioSource.PlayClipAtPoint(
                crownCollectSound,
                transform.position
            );

            GameManager.Instance.CollectCrown();

            Destroy(gameObject);
        }
    }
}