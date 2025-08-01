using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartLevel : MonoBehaviour
{
    [Header("Restart Settings")]
    [Tooltip("Délai en secondes avant le redémarrage")]
    public float restartDelay = 1.0f;

    [Tooltip("Touche pour redémarrer le niveau")]
    public KeyCode restartKey = KeyCode.O;

    private bool isRestarting = false;

    void Update()
    {
        // Vérifier si la touche de redémarrage est pressée et qu'on n'est pas déjà en train de redémarrer
        if (Input.GetKeyDown(restartKey) && !isRestarting)
        {
            StartRestart();
        }
    }

    public void StartRestart()
    {
        if (!isRestarting)
        {
            isRestarting = true;
            StartCoroutine(RestartWithDelay());
        }
    }

    private IEnumerator RestartWithDelay()
    {
        // Optionnel: Ajouter des effets visuels ou sonores ici
        Debug.Log($"Redémarrage du niveau dans {restartDelay} secondes...");

        // Attendre le délai spécifié
        yield return new WaitForSeconds(restartDelay);

        // Redémarrer la scène actuelle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Méthode publique pour redémarrer immédiatement (utile pour les boutons UI)
    public void RestartImmediately()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Méthode publique pour redémarrer avec délai (utile pour les boutons UI)
    public void RestartWithCustomDelay(float delay)
    {
        if (!isRestarting)
        {
            restartDelay = delay;
            StartRestart();
        }
    }
}

