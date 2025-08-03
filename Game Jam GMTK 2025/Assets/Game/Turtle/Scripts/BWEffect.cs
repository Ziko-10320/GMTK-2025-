using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BlackAndWhiteEffect : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private Shader blackAndWhiteShader; // R�f�rence � votre shader Noir et Blanc
    [SerializeField] private Material blackAndWhiteMaterial; // Mat�riau cr�� � partir du shader

    [Header("Exclusion Settings")]
    [Tooltip("Le calque des objets qui doivent rester en couleur.")]
    [SerializeField] private LayerMask exclusionLayer; // Le calque � exclure de l'effet N&B
    [SerializeField] private Camera exclusionCamera; // Cam�ra d�di�e au rendu du calque d'exclusion
    private RenderTexture exclusionRenderTexture; // Texture o� le calque d'exclusion sera rendu

    [Header("Control Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.K; // Touche pour activer/d�sactiver l'effet
    [SerializeField] private bool effectEnabled = false; // �tat initial de l'effet

    private Camera mainCamera; // R�f�rence � la cam�ra principale

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("BlackAndWhiteEffect requires a Camera component on the same GameObject.");
            enabled = false; // D�sactiver le script si aucune cam�ra n'est trouv�e
            return;
        }

        // Assurez-vous que le mat�riau est cr�� si le shader est assign�
        if (blackAndWhiteShader != null && blackAndWhiteMaterial == null)
        {
            blackAndWhiteMaterial = new Material(blackAndWhiteShader);
            blackAndWhiteMaterial.hideFlags = HideFlags.HideAndDontSave; // Cache le mat�riau dans l'�diteur
        }

        // Initialiser la cam�ra d'exclusion et sa RenderTexture
        SetupExclusionCamera();
    }

    void OnEnable()
    {
        // Assurez-vous que le mat�riau est valide lors de l'activation du script
        if (blackAndWhiteShader != null && blackAndWhiteMaterial == null)
        {
            blackAndWhiteMaterial = new Material(blackAndWhiteShader);
            blackAndWhiteMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        SetupExclusionCamera();
    }

    void OnDisable()
    {
        // Nettoyer les ressources lors de la d�sactivation du script
        if (blackAndWhiteMaterial != null)
        {
            DestroyImmediate(blackAndWhiteMaterial);
            blackAndWhiteMaterial = null;
        }
        ReleaseExclusionRenderTexture();
    }

    void Update()
    {
        // G�rer l'activation/d�sactivation de l'effet avec la touche
        if (Input.GetKeyDown(toggleKey))
        {
            effectEnabled = !effectEnabled;
            Debug.Log($"Black and White Effect: {(effectEnabled ? "Enabled" : "Disabled")}");
        }

        // Activer/d�sactiver la cam�ra d'exclusion en fonction de l'�tat de l'effet
        if (exclusionCamera != null)
        {
            exclusionCamera.gameObject.SetActive(effectEnabled);
        }
    }

    // Cette fonction est appel�e apr�s que la cam�ra a rendu la sc�ne
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (blackAndWhiteMaterial == null || !effectEnabled)
        {
            // Si le mat�riau n'est pas assign� ou l'effet est d�sactiv�, copier simplement la source vers la destination
            Graphics.Blit(source, destination);
            return;
        }

        // Rendre le calque d'exclusion dans sa propre RenderTexture
        if (exclusionCamera != null && exclusionRenderTexture != null)
        {
            // Sauvegarder les param�tres originaux de la cam�ra principale
            int originalCullingMask = mainCamera.cullingMask;
            CameraClearFlags originalClearFlags = mainCamera.clearFlags;
            Color originalBackgroundColor = mainCamera.backgroundColor;

            // Modifier le culling mask de la cam�ra principale pour exclure le calque d'exclusion
            mainCamera.cullingMask &= ~exclusionLayer; // Enl�ve le calque d'exclusion du culling mask de la cam�ra principale

            // Rendre la sc�ne principale (sans le calque d'exclusion) dans une texture temporaire
            RenderTexture tempMainTex = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            Graphics.Blit(source, tempMainTex);

            // Rendre le calque d'exclusion avec la cam�ra d'exclusion
            exclusionCamera.targetTexture = exclusionRenderTexture;
            exclusionCamera.Render();

            // Passer les textures au shader
            blackAndWhiteMaterial.SetTexture("_MainTex", tempMainTex);
            blackAndWhiteMaterial.SetTexture("_ExclusionTex", exclusionRenderTexture);

            // Appliquer le shader et copier le r�sultat vers la destination
            Graphics.Blit(tempMainTex, destination, blackAndWhiteMaterial);

            // Lib�rer la texture temporaire
            RenderTexture.ReleaseTemporary(tempMainTex);

            // Restaurer le culling mask original de la cam�ra principale
            mainCamera.cullingMask = originalCullingMask;
        }
        else
        {
            // Si la cam�ra d'exclusion n'est pas configur�e, appliquer juste le N&B sans exclusion
            blackAndWhiteMaterial.SetTexture("_MainTex", source);
            Graphics.Blit(source, destination, blackAndWhiteMaterial, 0); // Utilise la premi�re passe du shader
        }
    }

    void SetupExclusionCamera()
    {
        if (mainCamera == null) return;

        if (exclusionCamera == null)
        {
            // Tente de trouver une cam�ra d'exclusion existante ou en cr�e une
            GameObject exclusionCamGO = new GameObject("ExclusionCamera");
            exclusionCamera = exclusionCamGO.AddComponent<Camera>();
            exclusionCamera.transform.SetParent(mainCamera.transform);
            exclusionCamera.transform.localPosition = Vector3.zero;
            exclusionCamera.transform.localRotation = Quaternion.identity;
            exclusionCamera.transform.localScale = Vector3.one;

            // Copie les param�tres de la cam�ra principale
            exclusionCamera.CopyFrom(mainCamera);
            exclusionCamera.depth = mainCamera.depth + 1; // Rendre apr�s la cam�ra principale
            exclusionCamera.clearFlags = CameraClearFlags.Color; // Effacer avec une couleur
            exclusionCamera.backgroundColor = Color.clear; // Rendre le fond transparent
            exclusionCamera.cullingMask = exclusionLayer; // Ne rendre que le calque d'exclusion
            exclusionCamera.enabled = false; // D�sactiver par d�faut, sera activ�e par le script
        }

        // Assurez-vous que la RenderTexture est de la bonne taille
        if (exclusionRenderTexture == null || exclusionRenderTexture.width != mainCamera.pixelWidth || exclusionRenderTexture.height != mainCamera.pixelHeight)
        {
            ReleaseExclusionRenderTexture(); // Lib�rer l'ancienne si la taille change
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
