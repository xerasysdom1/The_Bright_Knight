using UnityEngine;
using UnityEngine.SceneManagement;

public class BombHazard : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver("You blew up!");
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
