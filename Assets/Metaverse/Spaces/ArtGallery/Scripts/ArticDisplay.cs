using Fusion;
using Fusion.Samples.Metaverse.ArtGallery;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using Fusion.XR.Shared;
using Fusion.Samples.IndustriesComponents;
using Fusion.Addons.Touch;


// All parameters of an artwork are registered in the network structure ArticArtwork
[System.Serializable]
public struct ArticArtwork : INetworkStruct
{
    public NetworkBool isVisible;
    public int id;
    public NetworkString<_64> image_id;
    public NetworkString<_128> title;
    public NetworkString<_128> artist_display;
    public Vector2 dimension;

    public float Scale { 
        get
        {
            if (dimension == Vector2.zero)
            {
                return 1;
            }
            return Math.Max(dimension.x, dimension.y);
        }
    }
}

/**
 * 
 * Artworks placeholders have been dispatched on the walls of the gallery at predefined positions.
 * Each artworks placeholder is managed by the `ArticDisplay` class 
 * In addition to the painting, we also display information about it (title, author, description).
 * There is also a button to display the real size of the work or a larger size.
 * As soon as an artwork property is changed, the `OnArticArtworkChanged()` method is called to update the artwork.
 * 
 **/
public class ArticDisplay : NetworkBehaviour
{
    const int LIMIT_SCALE_FOR_HIGHRES_IMAGES = 1;

    public float maxAllowedWidth = 2.5f;
    public float maxAllowedHeight = 2.3f;
    public float widthUsedForDescription = 0.38f;

    public bool used;


    [SerializeField] private TextMeshPro title;
    [SerializeField] private TextMeshPro artist;
    private MeshRenderer meshRenderer;
    [SerializeField] private GameObject displayVisual;
    [SerializeField] private float stickerMargin = 0.1f;

    public bool fillDescription = false;
    [SerializeField] private GameObject descriptionZone;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject frame;
    [SerializeField] private TextMeshPro descriptionText;

    [SerializeField] private GameObject imageLoadingSpinner;
    [SerializeField] private GameObject manifestLoadingSpinner;

    [SerializeField] private Transform titlePanel;
    [SerializeField] private Transform titleOnlyPosition;
    [SerializeField] private Transform titleWithDescriptionPosition;
    [SerializeField] private Transform sizeButtons;
    [SerializeField] private Transform buttonTittleOnlyPosition;
    [SerializeField] private Transform buttonTitleWithDescriptionPosition;

    [SerializeField] private Touchable OriginalSizeButton;
    [SerializeField] private Touchable MaxSizeButton;
    [SerializeField] private List<GameObject> associatedGameObjects = new List<GameObject>();


    [Networked]
    public NetworkBool artworkOriginalSize { get; set; } = true;

    ChangeDetector changeDetector;

    private void OnArticArtworkSizeChanged()
    {
        AdaptToDisplayScale();
    }

    [Networked]
    public ArticArtwork articArtwork { get; set; }
    public NetworkString<_64> Iiif_url { get; set; }

    Texture2D displaytexture;

    bool isHighResDisplayed = false;
    bool textureLoaded = false;

    string mainDescription = null;
    public float DisplayableWidth => (fillDescription && (!used || !string.IsNullOrEmpty(mainDescription)) ) ? maxAllowedWidth - widthUsedForDescription : maxAllowedWidth;

    public float DisplayableScaleForArtwork(ArticArtwork artwork)
    {
        return DisplayableScaleForArtworkDimension(artwork.dimension);
    }

    public float DisplayableScaleForArtworkDimension(Vector2 dimension)
    {
        if (dimension == Vector2.zero)
        {
            return DisplayableWidth;
        }
        else
        {
            if (dimension.x > dimension.y)
            {
                float scale = DisplayableWidth;
                float ratio = dimension.y / dimension.x;
                if ((scale * ratio) > maxAllowedHeight)
                {
                    scale = maxAllowedHeight / ratio;
                }
                return scale;
            }
            else
            {
                float scale = maxAllowedHeight;
                float ratio = dimension.x / dimension.y;
                if ((scale * ratio) > DisplayableWidth)
                {
                    scale = DisplayableWidth / ratio;
                }
                return scale;
            }
        }
    }

    private void Awake()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (displayVisual == null)
            Debug.LogError("displayVisual is not set");

        if (panel == null)
            Debug.LogError("panel is not set");

        if (frame == null)
            Debug.LogError("frame is not set");

        HideArtWork();

    }

    public override void Spawned()
    {
        base.Spawned();
        changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        if (Object.StateAuthority != Runner.LocalPlayer)
            OnArticArtworkSizeChanged();
        OnArticArtworkChanged();
    }

    public override void Render()
    {
        base.Render();
        foreach (var changedVar in changeDetector.DetectChanges(this))
        {
            if (changedVar == nameof(artworkOriginalSize))
            {
                if (Object.StateAuthority != Runner.LocalPlayer)
                    OnArticArtworkSizeChanged();
            }

            if (changedVar == nameof(articArtwork))
            {
                OnArticArtworkChanged();
            }
        }
    }

    private void OnArticArtworkChanged()
    {
        if (articArtwork.isVisible)
            DisplayArtWork();
        else
            HideArtWork();
    }

    private void OnDestroy()
    {
        if (displaytexture) Destroy(displaytexture);
    }

    public void HideArtWork()
    {
        meshRenderer.enabled = false;
        displayVisual.SetActive(false);
        if (manifestLoadingSpinner) manifestLoadingSpinner.SetActive(false);
        if (imageLoadingSpinner) imageLoadingSpinner.SetActive(false);
        mainDescription = null;
        textureLoaded = false;
        foreach(var associatedGameObject in associatedGameObjects)
        {
            if(associatedGameObject) associatedGameObject.SetActive(false);
        }
    }

    void ShowArtwork()
    {
        meshRenderer.enabled = true;
        displayVisual.SetActive(true);
        foreach (var associatedGameObject in associatedGameObjects)
        {
            if(associatedGameObject) associatedGameObject.SetActive(true);
        }
    }

    protected bool UseHighResForScale(float scale) => scale > LIMIT_SCALE_FOR_HIGHRES_IMAGES;

    async Task<bool> DisplayTextureForScale(float scale){
        ArticImageRequest imageRequest;
        var iiif_url = Iiif_url.ToString();
        if (string.IsNullOrEmpty(iiif_url)) iiif_url = ArticImageRequest.DEFAULT_IIIF_URL;

        if (UseHighResForScale(scale))
        {
            imageRequest = new ArticImageRequest(articArtwork.image_id.ToString(), iiif_url, useHighResImage: true);
        }
        else
        {
            imageRequest = new ArticImageRequest(articArtwork.image_id.ToString(), iiif_url, useHighResImage: false);
        }
        if (imageLoadingSpinner) imageLoadingSpinner.SetActive(true);
        await imageRequest.Launch();
        if (imageLoadingSpinner) imageLoadingSpinner.SetActive(false);
        if (imageRequest.texture == null)
        {
            Debug.LogError("Missing texture");
            return false;
        }

        textureLoaded = true;
        isHighResDisplayed = UseHighResForScale(scale);

        if (displaytexture) Destroy(displaytexture);
        // Mip-maps (Source: https://forum.unity.com/threads/generate-mipmaps-at-runtime-for-a-texture-loaded-with-unitywebrequest.644842/#post-7571809)
        displaytexture = new Texture2D(imageRequest.texture.width, imageRequest.texture.height, imageRequest.texture.format, true);
        displaytexture.SetPixelData(imageRequest.texture.GetRawTextureData<byte>(), 0);
        displaytexture.Apply(true, true);

        meshRenderer.material.mainTexture = displaytexture;

        return true;
    }

    [ContextMenu("Display artwork (download the image)")]
    public async void DisplayArtWork()
    {
        used = true;
        ShowArtwork();
        descriptionZone.SetActive(false);
        meshRenderer.material.mainTexture = null;

        if (manifestLoadingSpinner) manifestLoadingSpinner.SetActive(false);
        
        if (titlePanel && titleOnlyPosition)
        {
            titlePanel.position = titleOnlyPosition.position;
            sizeButtons.position = buttonTittleOnlyPosition.position;
        }

        // Update texts
        title.text = articArtwork.title.ToString();
        artist.text = articArtwork.artist_display.ToString();

        // Check if make sens to display the resize buttons
        if (articArtwork.Scale < DisplayableScaleForArtwork(articArtwork))
            sizeButtons.gameObject.SetActive(true);
        else
            sizeButtons.gameObject.SetActive(false);

        if (!await DisplayTextureForScale(articArtwork.Scale))
        {
            HideArtWork();
            return;
        }

        AdaptToDisplayScale();


        // Manifest and description
        if (fillDescription)
        {
            var manifestRequest = new ArticArtworkManifestRequest(articArtwork.id);
            if (manifestLoadingSpinner) manifestLoadingSpinner.SetActive(true);
            var manifestOpt = await manifestRequest.ParsedRequest();
            if (manifestLoadingSpinner) manifestLoadingSpinner.SetActive(false);
            if (manifestOpt != null)
            {
                var manifest = manifestOpt.GetValueOrDefault();
                var description = manifest.MainDescription;
                if (string.IsNullOrEmpty(description) == false)
                {
                    description = description.Replace(".", ".\n\n");
                    mainDescription = description;
                    if (titlePanel && titleOnlyPosition)
                    {
                        titlePanel.position = titleWithDescriptionPosition.position;
                      //  Vector3 newButtonPosition = new Vector3(buttonTitleWithDescriptionPosition.position.x, buttonTitleWithDescriptionPosition.position.y, sizeButtons.position.z);
                        sizeButtons.position = buttonTitleWithDescriptionPosition.position;
                    }
                    descriptionZone.SetActive(true);
                    descriptionText.text = description;
                }
            }
        }
    }

    private void AdaptToDisplayScale()
    {
        if (artworkOriginalSize)
            ScaleClosestToOriginalSize();
        else
            ScaleToDisplayableSizeWithProperResolution();
    }

    [ContextMenu("Scale to displayable size")]
    public void ScaleToDisplayableSize()
    {
        //Debug.LogError($"Scale: {DisplayableScaleForArtwork(articArtwork)}");
        AdaptToScale(DisplayableScaleForArtwork(articArtwork));
    }

    [ContextMenu("Scale to original size")]
    public void ScaleToOriginalSize()
    {
        AdaptToScale(articArtwork.Scale);
    }

    [ContextMenu("Scale to a one meter square")]
    public void ScaleToBasicSquare()
    {
        AdaptToScale(1);
    }

    [ContextMenu("Scale closest to original size (up to displayable size)")]
    public async void ScaleClosestToOriginalSize()
    {
        OriginalSizeButton.SetButtonStatus(true);
        MaxSizeButton.SetButtonStatus(false);

        if (!ResizeIsAuthorized())
        {
            OriginalSizeButton.SetButtonStatus(false);
            MaxSizeButton.SetButtonStatus(true);
            return;
        }


        if (Object.StateAuthority != Runner.LocalPlayer)
        {
            if (!await Object.WaitForStateAuthority()) return;
        }
        artworkOriginalSize = true;
        AdaptToScale(Mathf.Min(articArtwork.Scale, DisplayableScaleForArtwork(articArtwork)) );
    }

    [ContextMenu("Scale to displayable size (download high resolution if relevant)")]
    public async void ScaleToDisplayableSizeWithProperResolution()
    {
        OriginalSizeButton.SetButtonStatus(false);
        MaxSizeButton.SetButtonStatus(true);

        if (!ResizeIsAuthorized())
        {
            OriginalSizeButton.SetButtonStatus(true);
            MaxSizeButton.SetButtonStatus(false);
            return;
        }

        if (Object.StateAuthority != Runner.LocalPlayer)
        {
            if (!await Object.WaitForStateAuthority()) return;
        }
        artworkOriginalSize = false;
        ScaleToDisplayableSize();

        bool highResRequired = UseHighResForScale(DisplayableScaleForArtwork(articArtwork)) && isHighResDisplayed == false;
        if (highResRequired)
        {
            await DisplayTextureForScale(DisplayableScaleForArtwork(articArtwork));
        }
    }

    private void AdaptToScale(float targetScale)
    {
        Vector3 scale;

        // check if texture is loaded
        if (textureLoaded == false)
        {
            return;
        }

        if (meshRenderer.material.mainTexture.height < meshRenderer.material.mainTexture.width)
        {
            scale = new Vector3(1, (float)meshRenderer.material.mainTexture.height / (float)meshRenderer.material.mainTexture.width, 1) * targetScale;
        }
        else
        {
            scale = new Vector3((float)meshRenderer.material.mainTexture.width / (float)meshRenderer.material.mainTexture.height, 1, 1) * targetScale;
        }

        meshRenderer.transform.localScale = new Vector3(scale.x, scale.y, meshRenderer.transform.localScale.z);


        // Get the width of the Canvas using its RectTransform component
        RectTransform panelRect = titlePanel.GetComponent<RectTransform>();
        float panelWidth = panelRect.rect.width * panel.transform.localScale.x;

        // Calculate the distance that the Canvas needs to be from the right edge of the meshRenderer
        float distance = (scale.x * frame.transform.localScale.x / 2f) + (panelWidth / 2f) + stickerMargin;
        
        // Set the position of the Canvas based on the calculated distance and the current position of the meshRenderer
        panel.transform.localPosition = new Vector3(0, 0, -distance);
    }

    private float timeOfLastResize = 0f;
    private float resizeCoolDown = 0.3f;
    private bool ResizeIsAuthorized()
    {
        if (Time.time > timeOfLastResize + resizeCoolDown)
        {
            timeOfLastResize = Time.time;
            return true;
        }
        else
        {
            return false;
        }
    }
}
