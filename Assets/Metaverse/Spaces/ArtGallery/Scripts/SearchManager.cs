using Fusion;
using Fusion.Addons.VirtualKeyboard.Touch;
using Fusion.Samples.Metaverse.ArtGallery;
using Fusion.XR.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/**
 * 
 * SearchManager manages the search UI
 * When the player touchs one of this predefined button, the LaunchPredefinedSearch()` method is called with the predefined word in parameter
 * If the user enters any keyword with the keyboard and use the Search button, then it calls the `LaunchSearch()` method.
 * When a search is initiated (asynchronous `DoLaunchSearch()` task), first it requests the `StateAuthority` and synchronizes the search keyword for remote players. 
 * Then, it asks the `ArticGalleryManager` to search for the string passed in parameter.
 * Please note that the search keyword is synchronized on the network : this is usefull for the UI but also to avoid resources consumption (bandwidth and API) if the same search is performed by two users.
 * 
 **/
public class SearchManager : NetworkBehaviour
{
    [SerializeField] private TouchableTMPInputField keywordInputField;
    [SerializeField] private TextMeshProUGUI placeHolder;
    [SerializeField] private ArticGalleryManager articGalleryManager;
    [SerializeField] private TMP_Text searchButtonText;

    [Networked]
    public NetworkString<_128> keyword { get; set; }

    public NetworkString<_128> LastSearch { get; set; }

    ChangeDetector changeDetector;

    private void OnKeywordChanged()
    {
        keywordInputField.Text= keyword.ToString();
    }

    private void Awake()
    {
        if (!placeHolder)
            placeHolder = GetComponentInChildren<TextMeshProUGUI>();

        if (!articGalleryManager)
            articGalleryManager = FindObjectOfType<ArticGalleryManager>(true);

        previewImages = new List<UnityEngine.UI.RawImage>(preview.GetComponentsInChildren<UnityEngine.UI.RawImage>(true));
        foreach (var image in previewImages)
        {
            image.texture = null;
        }
        preview.SetActive(false);
    }

    public override void Spawned()
    {
        base.Spawned();
        changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        OnKeywordChanged();
    }

    public override void Render()
    {
        base.Render();
        foreach (var changedVar in changeDetector.DetectChanges(this))
        {
            if (changedVar == nameof(keyword))
            {
                OnKeywordChanged();
            }
        }
    }


    private void OnDestroy()
    {
        foreach(var image in previewImages)
        {
            if (image.texture) Destroy(image.texture);
        }
    }

    private void OnEnable()
    {
        if (!keywordInputField)
            keywordInputField = GetComponentInChildren<TouchableTMPInputField>();

        if (keywordInputField)
        {
            keywordInputField.onTextChange.AddListener(UpdateInputField);
            keywordInputField.onSubmit.AddListener(LaunchSearch);
        }
    }

    private void OnDisable()
    {
        keywordInputField.onTextChange.RemoveListener(UpdateInputField);
        keywordInputField.onSubmit.RemoveListener(LaunchSearch);
    }

    // UpdateInputField is called when the input text buffer changed in order to update the input field
    private void UpdateInputField()
    {
        //keyword = keywordInputField.Text;
        Debug.Log($"keyword = {keyword}");
        
        if (status == Status.Searching) return;
        preview.SetActive(false);
        searchButtonText.text = "Search";
        if (status != Status.WaitingForInputWithoutPreview)
        {
            status = Status.WaitingForInput;
        }
    }


    public void LaunchPredefinedSearch(string predefinedKeyword)
    {
        keywordInputField.Text = predefinedKeyword;
        LaunchSearch();
    }

    public enum Status
    {
        WaitingForInputWithoutPreview,
        WaitingForInput,
        DisplayingPreview,
        Searching
    }
    public Status status = Status.WaitingForInput;

    List<ArticGalleryManager.ArtworkDescription> previewResults = null;
    public async void LaunchSearch()
    {
        switch (status)
        {
            case Status.WaitingForInputWithoutPreview:
                await DoLaunchSearch();
                break;

            case Status.WaitingForInput:
                status = Status.Searching;
                previewResults = await articGalleryManager.LaunchPreviewSearch(keywordInputField.Text.Trim(), targetResultCount: 7);

                if(previewResults != null)
                {
                    preview.SetActive(true);
                    foreach (var image in previewImages)
                    {
                        if (image.texture) Destroy(image.texture);
                    }
                    int i = 0;
                    foreach(var previewResult in previewResults)
                    {
                        if (i >= previewImages.Count) break;
                        ArticImageRequest imageRequest = new ArticImageRequest(previewResult.data.image_id, previewResult.iiif_url, useHighResImage: false);
                        await imageRequest.Launch();
                        if (imageRequest.texture == null)
                        {
                            continue;
                        }
                        UnityEngine.UI.RawImage image = previewImages[i];
                        image.gameObject.SetActive(true);
                        i++;


                        // Mip-maps (Source: https://forum.unity.com/threads/generate-mipmaps-at-runtime-for-a-texture-loaded-with-unitywebrequest.644842/#post-7571809)
                        var texture = new Texture2D(imageRequest.texture.width, imageRequest.texture.height, imageRequest.texture.format, true);
                        texture.SetPixelData(imageRequest.texture.GetRawTextureData<byte>(), 0);
                        texture.Apply(true, true);
                        image.texture = texture;

                        Vector3 scale = Vector3.one;
                        if (image.texture.height < image.texture.width)
                        {
                            scale = new Vector3(1, (float)image.texture.height / (float)image.texture.width, 1);
                        }
                        else
                        {
                            scale = new Vector3((float)image.texture.width / (float)image.texture.height, 1, 1) ;
                        }

                        image.transform.localScale = new Vector3(scale.x, scale.y, image.transform.localScale.z);

                    }
                    while (i < previewImages.Count)
                    {
                        UnityEngine.UI.RawImage image = previewImages[i];
                        image.gameObject.SetActive(false);
                        i++;
                    }
                    status = Status.DisplayingPreview;
                    searchButtonText.text = "Install artworks";
                }
                else
                {
                    status = Status.WaitingForInput;
                    searchButtonText.text = "Search";
                }
                break;

            case Status.DisplayingPreview:
                //await PrepareSearch();
                //articGalleryManager.DisplayArticDescriptions(previewResults);
                status = Status.Searching;
                await DoLaunchSearch();
                status = Status.WaitingForInput;
                searchButtonText.text = "Search";
                break;
        }
    }

    public async Task PrepareSearch()
    {
        Debug.Log("Launching search: " + keywordInputField.Text);
        if (Object.StateAuthority != Runner.LocalPlayer)
        {
            if (!await Object.WaitForStateAuthority()) return;
        }

        // sync the keyword
        keyword = keywordInputField.Text.Trim();

        if (keyword.ToLower() == LastSearch.ToLower())
        {
            Debug.Log("Skipping same search");
            return;
        }

        LastSearch = keyword;
    }

    public async Task DoLaunchSearch() 
    {
        await PrepareSearch();
        articGalleryManager.LaunchSearch(keyword.ToString());
    }

    public GameObject preview;
    public List<UnityEngine.UI.RawImage> previewImages = new List<UnityEngine.UI.RawImage>();

    void LoadPreview()
    {
        preview.SetActive(true);
    }
}
