using Fusion.Samples.Metaverse.ArtGallery;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Fusion.XR.Shared;
using Fusion;

/**
 * 
 * ArticGalleryManager is in charge to search items into the Chicago Art Institute library using a keyword and to display the results on the art work places holders.
 * To do so, a list of all `ArticDisplay` in the scene is initialized during the `Awake()`.
 * During the `LaunchSearch` task, it uses the `ArticSearchRequest` method to find approriate artworks.
 * Results are recorded into an `artworkDescriptions` list and displayed thanks to `FillDisplayFound()` & `FillDisplayAsync()`
 * - `FindMostFittingDisplay` is in charge to find a suitable artworks place holder (some walls can accommodate larger artworks than others).
 * - `FillDisplayAsync` updates the artwork properties with the data received from the API, requests the `StateAuthority` on the artwork place holder and synchronize it over the network.
 * 
 **/
public class ArticGalleryManager : MonoBehaviour
{
    public bool debugLaunchTests = false;
    [SerializeField] private bool onlyPublicDomain = true;
    [SerializeField] private bool filterDisplayedOnType = true;
    [SerializeField] private int page = 0;
    [SerializeField] private int maxSearch = 4;
    [SerializeField] private int limit = 50;
    [SerializeField] private List<ArticData> displayedArticDatas;
    [SerializeField] private List<ArticSearch> searchResults;
    [SerializeField] private GameObject loadingSpinner;

    public List<ArticDisplay> articDisplays = new List<ArticDisplay>();

    private void Awake()
    {
        // Look for the ArticDisplay in the scene
        articDisplays = new List<ArticDisplay>(FindObjectsOfType<ArticDisplay>());
        if (loadingSpinner) loadingSpinner.SetActive(false);

    }

    private void Update()
    {
        if (!debugLaunchTests) return;
        debugLaunchTests = false;
        LaunchSearch("cats");
    }

    ArticDisplay FindMostFittingDisplay(ArticData artwork, bool requireDescription)
    {
        // We first select a display where the art can be the most visisble (higher scale),
        //  then the smallest display among equalities to keep the biggest ones available (unless the artwork is boosted in search)
        float maxScale = 0;
        float minWidth = float.PositiveInfinity;
        ArticDisplay bestDisplay = null;
        //Debug.LogError($"Searching display for {artwork.title} {artwork.ParsedDimensions} ...");
        foreach(var display in articDisplays)
        {
            if (display.used) continue;
            float scale = display.DisplayableScaleForArtworkDimension(artwork.ParsedDimensions);
            bool bestDisplayCompatibleWithOriginalScale = (maxScale >= artwork.Scale);
            bool displayCompatibleWithOriginalScale = (scale >= artwork.Scale);
            //Debug.LogError($" - display {display.name} scale : {scale} (requireDescription: {requireDescription} no display:{bestDisplay == null} can show desc.: {display.fillDescription})" );
            if(requireDescription && bestDisplay && bestDisplay.fillDescription == false && display.fillDescription)
            {
                // We try to find a place where we can display the description for boosted artworks
                //Debug.LogError($"New best for boosted (requireDescription: {requireDescription})");
                maxScale = scale;
                bestDisplay = display;
                minWidth = display.DisplayableWidth;
            }
            bool compatibleForBoostedArtworks = requireDescription == false || bestDisplay == null || display.fillDescription;
            if (!bestDisplayCompatibleWithOriginalScale && maxScale < scale && compatibleForBoostedArtworks) {
                //Debug.LogError($"New best scale {scale} (boosted: {requireDescription} no display:{bestDisplay == null} can show desc.: {display.fillDescription}))");
                maxScale = scale;
                bestDisplay = display;
                minWidth = display.DisplayableWidth;
            }
            else if(requireDescription == false && displayCompatibleWithOriginalScale && minWidth > display.DisplayableWidth)
            {
                //Debug.LogError($"New best width {display.DisplayableWidth} (requireDescription: {requireDescription} no display:{bestDisplay == null} can show desc.: {display.fillDescription}))");
                bestDisplay = display;
                minWidth = display.DisplayableWidth;
            }
        }
        //Debug.LogError($"-> Most fitting dispaly {bestDisplay.name} (requireDescription: {requireDescription} / can show desc.: {bestDisplay.fillDescription}))");
        return bestDisplay;
    }

    bool RemainingDisplayWithDescription()
    {
        foreach(var display in articDisplays)
        {
            if(display.used == false && display.fillDescription)
            {
                return true;
            }
        }
        return false;
    }

    public delegate bool OnDisplayFound(ArtworkDescription result);

    [ContextMenu("LaunchTests")]
    public async void LaunchSearch(string search)
    {
        HideAllDisplays();
        foreach (var display in articDisplays)
        {
            display.used = false;
            display.HideArtWork();
        }

        while (searchRunning) await AsyncTask.Delay(100);
        await LaunchSearch(search, FillDisplayFound, startPage: page, maxPageSearch: maxSearch, targetResultCount: articDisplays.Count);

        HideUnusedDisplays();
    }

    public async Task<List<ArtworkDescription>> LaunchPreviewSearch(string search, int targetResultCount = 5)
    {
        if (searchRunning) return null;
        return await LaunchSearch(search, null, startPage: page, maxPageSearch: maxSearch, targetResultCount: targetResultCount);
    }

    public void DisplayArticDescriptions(List<ArtworkDescription> artworkDescriptions)
    {
        //Debug.LogError("-- DisplayArticDescriptions ");
        HideAllDisplays();
        foreach (var artworkDescription in artworkDescriptions)
        {
            FillDisplayFound(artworkDescription);
        }
        HideUnusedDisplays();
    }

    bool searchRunning = false;
    public async Task<List<ArtworkDescription>> LaunchSearch(string search, OnDisplayFound onDisplayFound, int startPage, int maxPageSearch, int targetResultCount, int msBetweenPageSearches = 1000) 
    {        
        searchRunning = true;
        int displayedArtWorks = 0;
        int currentPage = startPage;
        bool searching = true;
        int launchedSearches = 0;

        Debug.Log($"Searching art works with keyword -{search}-");

        searchResults = new List<ArticSearch>();
        displayedArticDatas = new List<ArticData>();

        if (loadingSpinner) loadingSpinner.SetActive(true);
        ArtworkDescription artworkDescription;
        List<ArtworkDescription> artworkDescriptions = new List<ArtworkDescription>();
        while (searching)
        {
            Debug.Log($"Searching page {currentPage}");
            var request = new ArticSearchRequest(search, forcePublicDomain: onlyPublicDomain, forceImageId: true, page: currentPage, limit: limit);
            var result = await request.ParsedRequest();

            if (result == null) break;
            var requestSearchResult = result.GetValueOrDefault();
            searchResults.Add(requestSearchResult);

            // This debug only works due to forceImageId: true
            foreach (var searchResult in requestSearchResult.data) Debug.Log($"{ArticImageRequest.ImageUrl(searchResult.image_id, requestSearchResult.config.iiif_url)}");
            if (requestSearchResult.data.Count <= 0) break;

            artworkDescription.iiif_url = requestSearchResult.config.iiif_url;

            foreach (var data in requestSearchResult.data)
            {
                if (displayedArtWorks >= targetResultCount) break;
                if (!IsValidArtworks(data))
                {
                    continue;
                }

                var boosted = data.is_boosted;
                if (boosted == false && RemainingDisplayWithDescription())
                {
                    var manifestRequest = new ArticArtworkManifestRequest(data.id);
                    var manifestOpt = await manifestRequest.ParsedRequest();
                    if (manifestOpt != null)
                    {
                        var manifest = manifestOpt.GetValueOrDefault();
                        var description = manifest.MainDescription;
                        if (!string.IsNullOrEmpty(description))
                        {
                            boosted = true;
                        }
                    }
                }

                artworkDescription.data = data;
                artworkDescription.boosted = boosted;

                if (onDisplayFound != null)
                {
                    searching = onDisplayFound(artworkDescription);
                    if (!searching) break;
                }
                artworkDescriptions.Add(artworkDescription);

                displayedArticDatas.Add(data);

                displayedArtWorks++;
            }

            launchedSearches++;
            currentPage++;

            if (launchedSearches >= maxSearch)
            {
                Debug.LogWarning("Quit search because maxSearch reached");
                searching = false;
            }

            if (currentPage >= requestSearchResult.pagination.total_pages)
            {
                Debug.LogWarning("Quit search because total page reached");
                searching = false;
            }

            if (displayedArtWorks >= targetResultCount)
            {
                Debug.LogWarning("Quit search because nb of art works display reached");
                searching = false;
            }

            if (searching)
            {
                // time between page search
                await AsyncTask.Delay(1000);
            }
        }

        if (loadingSpinner) loadingSpinner.SetActive(false);
        searchRunning = false;
        return artworkDescriptions;
    }

    void HideAllDisplays()
    {
        foreach (var display in articDisplays)
        {
            display.used = false;
            display.HideArtWork();
        }
    }

    void HideUnusedDisplays() { 
        ArticArtwork articArtWorkTemp = new ArticArtwork();
        foreach (var display in articDisplays)
        {
            if (display.used) continue;
            articArtWorkTemp.isVisible = false;
            display.articArtwork = articArtWorkTemp;
        }
    }

    public struct ArtworkDescription {
        public ArticData data;
        public bool boosted;
        public string iiif_url;
    }

    bool FillDisplayFound(ArtworkDescription result)
    {      
        var display = FindMostFittingDisplay(result.data, result.boosted);
        if (display == null)
        {
            Debug.LogWarning("Quit search because no fitting display");
            return false;
        }
        display.used = true;

        FillDisplay(result.data, display, result.iiif_url);
        return true;
    }

    async void FillDisplay(ArticData data, ArticDisplay display, string iiif_url)
    {
        await FillDisplayAsync(data, display, iiif_url);
    }

    async Task FillDisplayAsync(ArticData data, ArticDisplay display, string iiif_url)
    {
        ArticArtwork articArtWorkTemp = new ArticArtwork();
        articArtWorkTemp.isVisible = true;
        articArtWorkTemp.title = data.title;
        articArtWorkTemp.artist_display = data.artist_display;
        articArtWorkTemp.id = data.id;
        articArtWorkTemp.image_id = data.image_id;
        articArtWorkTemp.dimension = data.ParsedDimensions;

        // GET authority on the ArticDisplay
        if (display.Object.StateAuthority != display.Runner.LocalPlayer)
        {
            if (!await display.Object.WaitForStateAuthority()) return;
        }

        if (display.Iiif_url != iiif_url) display.Iiif_url = iiif_url;
        display.artworkOriginalSize = true;
        if(display.articArtwork.id == articArtWorkTemp.id && display.articArtwork.image_id == articArtWorkTemp.image_id)
        {
            //Debug.LogError("reloading the same artwork at the same place");
            display.DisplayArtWork();
        }
        else
        {
            // sync the artwork
            display.articArtwork = articArtWorkTemp;
        }
    }

    bool IsValidArtworks(ArticData data)
    {
        if (filterDisplayedOnType && data.artwork_type_title != "Painting" && data.artwork_type_title != "Print" && data.artwork_type_title.Contains("Drawing") == false)
        {
            //Debug.LogError("Skipping " + data.artwork_type_title);
            return false;
        }
        var scale = data.Scale;
        if (scale < 0.2f)
        {
            //Debug.LogError($"Skipping size too small: {scale}");
            return false;
        }
        foreach(var displayedData in displayedArticDatas)
        {
            if (displayedData.title == data.title && displayedData.artist_display == data.artist_display && displayedData.date_display == data.date_display)
            {
                //Debug.LogError($"Skipping , already displayed a copy: {data.title}");
                return false;
            }
        }
        return true;
    }
}
