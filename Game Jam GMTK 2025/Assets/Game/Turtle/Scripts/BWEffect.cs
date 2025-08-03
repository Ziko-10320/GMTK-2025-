using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BlackAndWhiteEffect : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private Shader blackAndWhiteShader; // Référence à votre shader Noir et Blanc
    [SerializeField] private Material blackAndWhiteMaterial; // Matériau créé à partir du shader

    [Header("Exclusion Settings")]
    [Tooltip("Le calque des objets qui doivent rester en couleur.")]
    [SerializeField] private LayerMask exclusionLayer; // Le calque à exclure de l'effet N&B
    [SerializeField] private Camera exclusionCamera; // Caméra dédiée au rendu du calque d'exclusion
    private RenderTexture exclusionRenderTexture; // Texture où le calque d'exclusion sera rendu

    [Header("Control Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.K; // Touche pour activer/désactiver l'effet
    [SerializeField] private bool effectEnabled = false; // État initial de l'effet

    private Camera mainCamera; // Référence à la caméra principale

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("BlackAndWhiteEffect requires a Camera component on the same GameObject.");
            enabled = false; // Désactiver le script si aucune caméra n'est trouvée
            return;
        }

        // Assurez-vous que le matériau est créé si le shader est assigné
        if (blackAndWhiteShader != null && blackAndWhiteMaterial == null)
        {
            blackAndWhiteMaterial = new Material(blackAndWhiteShader);
            blackAndWhiteMaterial.hideFlags = HideFlags.HideAndDontSave; // Cache le matériau dans l'éditeur
        }

        // Initialiser la caméra d'exclusion et sa RenderTexture
        SetupExclusionCamera();
    }

    void OnEnable()
    {
        // Assurez-vous que le matériau est valide lors de l'activation du script
        if (blackAndWhiteShader != null && blackAndWhiteMaterial == null)
        {
            blackAndWhiteMaterial = new Material(blackAndWhiteShader);
            blackAndWhiteMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        SetupExclusionCamera();
    }

    void OnDisable()
    {
        // Nettoyer les ressources lors de la désactivation du script
        if (blackAndWhiteMaterial != null)
        {
            DestroyImmediate(blackAndWhiteMaterial);
            blackAndWhiteMaterial = null;
        }
        ReleaseExclusionRenderTexture();
    }

    void Update()
    {
        // Gérer l'activation/désactivation de l'effet avec la touche
        if (Input.GetKeyDown(toggleKey))
        {
            effectEnabled = !effectEnabled;
            Debug.Log($"Black and White Effect: {(effectEnabled ? "Enabled" : "Disabled")}");
        }

        // Activer/désactiver la caméra d'exclusion en fonction de l'état de l'effet
        if (exclusionCamera != null)
        {
            exclusionCamera.gameObject.SetActive(effectEnabled);
        }
    }

    // Cette fonction est appelée après que la caméra a rendu la scène
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (blackAndWhiteMaterial == null || !effectEnabled)
        {
            // Si le matériau n'est pas assigné ou l'effet est désactivé, copier simplement la source vers la destination
            Graphics.Blit(source, destination);
            return;
        }

        // Rendre le calque d'exclusion dans sa propre RenderTexture
        if (exclusionCamera != null && exclusionRenderTexture != null)
        {
            // Sauvegarder les paramètres originaux de la caméra principale
            int originalCullingMask = mainCamera.cullingMask;
            CameraClearFlags originalClearFlags = mainCamera.clearFlags;
            Color originalBackgroundColor = mainCamera.backgroundColor;

            // Modifier le culling mask de la caméra principale pour exclure le calque d'exclusion
            mainCamera.cullingMask &= ~exclusionLayer; // Enlève le calque d'exclusion du culling mask de la caméra principale

            // Rendre la scène principale (sans le calque d'exclusion) dans une texture temporaire
            RenderTexture tempMainTex = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            Graphics.Blit(source, tempMainTex);

            // Rendre le calque d'exclusion avec la caméra d'exclusion
            exclusionCamera.targetTexture = exclusionRenderTexture;
            exclusionCamera.Render();

            // Passer les textures au shader
            blackAndWhiteMaterial.SetTexture("_MainTex", tempMainTex);
            blackAndWhiteMaterial.SetTexture("_ExclusionTex", exclusionRenderTexture);

            // Appliquer le shader et copier le résultat vers la destination
            Graphics.Blit(tempMainTex, destination, blackAndWhiteMaterial);

            // Libérer la texture temporaire
            RenderTexture.ReleaseTemporary(tempMainTex);

            // Restaurer le culling mask original de la caméra principale
            mainCamera.cullingMask = originalCullingMask;
        }
        else
        {
            // Si la caméra d'exclusion n'est pas configurée, appliquer juste le N&B sans exclusion
            blackAndWhiteMaterial.SetTexture("_MainTex", source);
            Graphics.Blit(source, destination, blackAndWhiteMaterial, 0); // Utilise la première passe du shader
        }
    }

    void SetupExclusionCamera()
    {
        if (mainCamera == null) return;

        if (exclusionCamera == null)
        {
            // Tente de trouver une caméra d'exclusion existante ou en crée une
            GameObject exclusionCamGO = new GameObject("ExclusionCamera");
            exclusionCamera = exclusionCamGO.AddComponent<Camera>();
            exclusionCamera.transform.SetParent(mainCamera.transform);
            exclusionCamera.transform.localPosition = Vector3.zero;
            exclusionCamera.transform.localRotation = Quaternion.identity;
            exclusionCamera.transform.localScale = Vector3.one;

            // Copie les paramètres de la caméra principale
            exclusionCamera.CopyFrom(mainCamera);
            exclusionCamera.depth = mainCamera.depth + 1; // Rendre après la caméra principale
            exclusionCamera.clearFlags = CameraClearFlags.Color; // Effacer avec une couleur
            exclusionCamera.backgroundColor = Color.clear; // Rendre le fond transparent
            exclusionCamera.cullingMask = exclusionLayer; // Ne rendre que le calque d'exclusion
            exclusionCamera.enabled = false; // Désactiver par défaut, sera activée par le script
        }

        // Assurez-vous que la RenderTexture est de la bonne taille
        if (exclusionRenderTexture == null || exclusionRenderTexture.width != mainCamera.pixelWidth || exclusionRenderTexture.height != mainCamera.pixelHeight)
        {
            ReleaseExclusionRenderTexture(); // Libérer l'ancienne si la taille change
            exclusionRenderTexture = new RenderTexture(mainCamera.pixelWidth, mainCamera.pixelHeight, 0, RenderTextureFormat.ARGB32);
            exclusionRenderTexture.name = "ExclusionRenderTexture";
            exclusionRenderTexture.Create();
        }
    }

    void ReleaseExclusionRenderTexture()
    {
        if (exclusionRenderTexture != null)
        {
            exclusionRenderTexture.Release();
            DestroyImmediate(exclusionRenderTexture);
            exclusionRenderTexture = null;
        }
    }
}
