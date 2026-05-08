using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Globalization;
using Fusion.XR.Shared;

namespace Fusion.Samples.Metaverse.ArtGallery
{
    // Art institure of Chicago API (https://api.artic.edu/docs/)
    public class ArticAPIManager : MonoBehaviour
    {
        public static ArticAPIManager Instance;
        public PerformanceManager performanceManager;

        public string apiBaseUrl = "https://api.artic.edu/api/v1/";

        public float minDelayBetweenRequests = 0.05f;
        float lastRequest = -1;
        void Awake()
        {
            Instance = this;
            if (performanceManager == null) performanceManager = FindObjectOfType<PerformanceManager>(true);
            if (performanceManager == null) Debug.LogError("Missing performance manager");
            PurgeOldCache();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public async Task<PerformanceManager.TaskToken?> RequestToStartTask() {
            while (lastRequest != -1 && (Time.time - lastRequest) < minDelayBetweenRequests)
            {
                await AsyncTask.Delay(10);
            }
            var token = await performanceManager.RequestToStartTask(PerformanceManager.TaskKind.NetworkRequest);
            lastRequest = Time.time;
            return token;
        }

        public void TaskCompleted(PerformanceManager.TaskToken? token)
        {
            performanceManager.TaskCompleted(token);
        }

        void PurgeOldCache()
        {
            string cachePath = PrepareStorageFolder();
            string[] cacheFiles = System.IO.Directory.GetFiles(cachePath);

            if (cacheFiles.Length < 50) return;
            foreach (string cacheFile in cacheFiles)
            {
                System.IO.FileInfo info = new System.IO.FileInfo(cacheFile);
                if (info.LastAccessTime < DateTime.Now.AddDays(-7))
                {
                    Debug.Log($"Deleting unsed (for a week) cached file {info.Name} (last access: {info.LastAccessTime})");
                    info.Delete();
                }
            }
        }

        public string PrepareStorageFolder()
        {
            string localPath = Application.temporaryCachePath;
            localPath = System.IO.Path.Combine(localPath, "ArticCache");
            try
            {
                if (!System.IO.Directory.Exists(localPath))
                {
                    System.IO.Directory.CreateDirectory(localPath);
                }

            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            return localPath;
        }
    }

    public class ArticRequest
    {
        protected string methodUrlPart;
        public string url;
        protected bool requesting = false;

        public string error;
        public string resultText;
        protected string localId;
        public ArticRequest(string methodUrlPart = "")
        {
            this.methodUrlPart = methodUrlPart;
            this.url = ArticAPIManager.Instance.apiBaseUrl.TrimEnd('/') + "/" + methodUrlPart;
            localId = PrepareLocalId(methodUrlPart);
        }

        protected string PrepareLocalId(string potentialId)
        {
            var charactersToRemove = System.IO.Path.GetInvalidFileNameChars();
            var id = String.Join("-", potentialId.Split(charactersToRemove, StringSplitOptions.RemoveEmptyEntries));
            // . are not valid at the end of a file name
            id = id.TrimEnd('.');
            return id;
        }

        public async Task Launch()
        {
            var token = await ArticAPIManager.Instance.RequestToStartTask();
            ArticAPIManager.Instance.StartCoroutine(DoGetRequest());
            while (requesting)
            {
                await AsyncTask.Delay(10);
            }
            ArticAPIManager.Instance.TaskCompleted(token);
        }



        protected string PrepareLocalPath() {
            if (string.IsNullOrEmpty(localId)) return null;

            string localPath = ArticAPIManager.Instance.PrepareStorageFolder();
            if (localPath != null)
            {
                localPath = System.IO.Path.Combine(localPath, localId);
            }
            return localPath;
        }

        protected void SaveCache(string localPath, byte[] content)
        {
            if (localPath != null)
            {
                try
                {
                    //Debug.LogError($" ** Saving cache file {localPath}");
                    System.IO.File.WriteAllBytes(localPath, content);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to write cache >{localPath}< {e.Message}");
                    Debug.LogError(e);
                }
            }
        }

        protected byte[] LoadCache(string localPath)
        {
            try
            {
                if (System.IO.File.Exists(localPath))
                {
                    return System.IO.File.ReadAllBytes(localPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to load cache >{localPath}< {e.Message}");
                Debug.LogError(e);
            }
            return null;
        }

        protected virtual IEnumerator DoGetRequest()
        {
            requesting = true;

            string localPath = PrepareLocalPath();
            byte[] cache = LoadCache(localPath);
            if(cache != null)
            {
                //Debug.LogError($" !! Using cache file {localPath} {Application.temporaryCachePath}");
                resultText = System.Text.Encoding.UTF8.GetString(cache);
                requesting = false;
                yield break;
            }

            Debug.Log("Calling " + url);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        error = webRequest.error;
                        break;
                    case UnityWebRequest.Result.Success:
                        resultText = webRequest.downloadHandler.text;

                        SaveCache(localPath, webRequest.downloadHandler.data);

                        break;
                }
            }
            requesting = false;
        }
    }

    public class ArticParsedRequest<T> : ArticRequest where T : struct
    {
        public async Task<T?> ParsedRequest()
        {
            await Launch();
            if (!string.IsNullOrEmpty(resultText))
            {
                var result = JsonUtility.FromJson<T>(resultText);
                return result;
            }
            return null;
        }
    }

    [System.Serializable]
    public struct Config
    {
        public string iiif_url;
    }

    [System.Serializable]
    public struct Pagination
    {
        public int total;
        public int limit;
        public int offset;
        public int total_pages;
        public int current_pages;
    }

    [System.Serializable]
    public struct ArticData
    {
        public int _score;
        public int id;
        public string title;
        public string image_id;
        public string dimensions;
        public List<string> artist_titles;
        public string artist_display;
        public string medium_display;
        public string catalogue_display;
        public string date_display;
        public string place_of_origin;
        public string publication_history;
        public string exhibition_history;
        public string provenance_text;
        public string credit_line;
        public string artwork_type_title;
        public string is_public_domain;
        public bool is_boosted;
        // Fetched with additional calls
        public string manifest_main_description;

        public float ParseFraction(string intFraction)
        {
            if (string.IsNullOrEmpty(intFraction)) return 0;
            var decParts = intFraction.Split("/");
            if (decParts.Length >= 2)
            {
                var denominator = float.Parse(decParts[1], CultureInfo.InvariantCulture.NumberFormat);
                if (denominator != 0)
                {
                    var nominator = float.Parse(decParts[0], CultureInfo.InvariantCulture.NumberFormat);
                    return nominator / denominator;
                }
            }
            return 0;
        }

        Vector2 _parsedDimensions;
        public Vector2 ParsedDimensions
        {
            get
            {
                if (_parsedDimensions != Vector2.zero) return _parsedDimensions;

                var parsedDimensions = Vector2.zero;
                try
                {
                    string pattern = @"([0-9|\.]*) [×|x|X] ([0-9|\.]*) cm";
                    Match match = Regex.Match(dimensions, pattern);
                    if (match.Success && match.Groups.Count >= 3 && match.Groups[1].Captures.Count > 0 && match.Groups[2].Captures.Count > 0)
                    {
                        //Debug.LogError($"{dimensions} /// {match.Groups[1].Captures[0].Value} /// {match.Groups[2].Captures[0].Value}");
                        var y = float.Parse(match.Groups[1].Captures[0].Value, CultureInfo.InvariantCulture.NumberFormat);
                        var x = float.Parse(match.Groups[2].Captures[0].Value, CultureInfo.InvariantCulture.NumberFormat);
                        parsedDimensions = new Vector2(x / 100f, y / 100f);
                    }
                    // double check with the inches size
                    pattern = @"\(([0-9|\.]*)( [0-9|\.]*/[0-9|\.]*)? [×|x|X] ([0-9|\.]*)( [0-9|\.]*/[0-9|\.]*)? in.\)";
                    match = Regex.Match(dimensions, pattern);
                    if (parsedDimensions != Vector2.zero && match.Success && match.Groups.Count >= 5 && match.Groups[1].Captures.Count > 0 && match.Groups[3].Captures.Count > 0)
                    {
                        var y = float.Parse(match.Groups[1].Captures[0].Value, CultureInfo.InvariantCulture.NumberFormat);
                        if (match.Groups[2].Captures.Count > 0)
                        {
                            y += ParseFraction(match.Groups[2].Captures[0].Value);
                        }

                        var x = float.Parse(match.Groups[3].Captures[0].Value, CultureInfo.InvariantCulture.NumberFormat);
                        if (match.Groups[4].Captures.Count > 0)
                        {
                            x += ParseFraction(match.Groups[4].Captures[0].Value);
                        }

                        x = 0.0254f * x;
                        y = 0.0254f * y;

                        //Debug.LogError($"Inch proximity: {x}/{parsedDimensions.x}=>{x / parsedDimensions.x} {y}/{parsedDimensions.y}=>{y / parsedDimensions.y} ({dimensions})");
                        if ((x / parsedDimensions.x) < 0.8f || (parsedDimensions.x / x) < 0.8f || (y / parsedDimensions.y) < 0.8f || (parsedDimensions.y / y) < 0.8f) {
                            // x cm and inches value are not compatible. Taking the one where the ratio between x and y is the biggest
                            float cmRatio = parsedDimensions.x > parsedDimensions.y ? parsedDimensions.y / parsedDimensions.x : parsedDimensions.x / parsedDimensions.y;
                            float inchesRatio = x > y ? y / x : x / y;
                            if (inchesRatio > cmRatio)
                            {
                                Debug.LogError($"[Fix to {title} dimensions] Using inch ({x},{y})[Ratio:{inchesRatio}] instead of cm {parsedDimensions}[Ratio:{cmRatio}] for {dimensions}");
                                parsedDimensions = new Vector2(x, y);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Unable to parse dimension " + e.Message);
                    Debug.LogError(e);
                }
                _parsedDimensions = parsedDimensions;
                return parsedDimensions;
            }
        }

        public float Scale
        {
            get
            {
                var dim = ParsedDimensions;
                if(dim == Vector2.zero)
                {
                    return 1;
                }
                return Math.Max(dim.x, dim.y);
            }
        }

        public bool IsInPortraitMode
        {
            get
            {
                var dim = ParsedDimensions;
                if (dim == Vector2.zero)
                {
                    return false;
                }

                return dim.x < dim.y;
            }
        }
    }

    [System.Serializable]
    public struct ArticSearch
    {
        public Pagination pagination;
        public List<ArticData> data;
        public Config config;
    }

    [System.Serializable]
    public struct ArticDataInfo
    {
        public ArticData data;
        public Config config;
    }

    [System.Serializable]
    public struct ArticManifestText
    {
        public string value;
        public string language;
    }

    [System.Serializable]
    public struct ArticManifestMetadata
    {
        public string label;
        public string value;
    }

    [System.Serializable]
    public struct ArticArtworkManifest
    {
        public List<ArticManifestText> description;
        public List<ArticManifestMetadata> metadata;

        public string MainDescription
        {
            get
            {
                if (description == null || description.Count == 0) return null;
                return description[0].value;
            }
        }
    }

    public class ArticSearchRequest : ArticParsedRequest<ArticSearch>
    {
        public ArticSearchRequest(string search, bool forcePublicDomain = true, bool forceImageId = false, int page = 0, int limit = 40)
        {
            this.url = ArticAPIManager.Instance.apiBaseUrl.TrimEnd('/') + "/" + "artworks/search?q=" + Uri.EscapeDataString(search);
            if (forcePublicDomain) this.url += "&query[term][is_public_domain]=true";
            if (forceImageId)
            {
                // Maybe should not be used in production, hence false by default
                this.url += "&fields=id,image_id,title,dimensions,medium_display,artist_display,artwork_type_title,is_public_domain,is_boosted";
            }
            this.url += "&page=" + page + "&limit=" + limit;
            localId = PrepareLocalId($"SEARCH_{search}_{((forcePublicDomain)?"1":"0")}{((forceImageId)?"1":"0")}_p-{page}l{limit}");
        }
    }
    public class ArticDataRequest : ArticParsedRequest<ArticDataInfo>
    {
        public ArticDataRequest(int id)
        {
            this.url = ArticAPIManager.Instance.apiBaseUrl.TrimEnd('/') + "/" + "artworks/" + id;
            localId = PrepareLocalId($"DATA_{id}");
        }
    }

    public class ArticArtworkManifestRequest : ArticParsedRequest<ArticArtworkManifest>
    {
        public ArticArtworkManifestRequest(int id)
        {
            this.url = ArticAPIManager.Instance.apiBaseUrl.TrimEnd('/') + "/" + "artworks/" + id+ "/manifest.json";
            localId = PrepareLocalId($"MANIFEST_{id}");
        }
    }

    public class ArticImageRequest : ArticRequest
    {
        public Texture2D texture;
        public byte[] imageData;
        public const string DEFAULT_IIIF_URL = "https://www.artic.edu/iiif/2";

        public static string ImageUrl(string imageId, string iiifUrl = DEFAULT_IIIF_URL) => $"{iiifUrl}/{imageId}/full/843,/0/default.jpg";

        // Should preferably be used with public domain images
        public static string HighResImageUrl(string imageId, string iiifUrl = DEFAULT_IIIF_URL) => $"{iiifUrl}/{imageId}/full/1686,/0/default.jpg";
        public ArticImageRequest(string imageId, string iiifUrl = DEFAULT_IIIF_URL, bool useHighResImage = false)
        {
            SetImageURL(imageId, DEFAULT_IIIF_URL, useHighResImage: useHighResImage);
        }

        public void SetImageURL(string imageId, string iiif_url, bool useHighResImage = false) {
            if (useHighResImage)
            {
                url = ArticImageRequest.HighResImageUrl(imageId, iiif_url);
            }
            else
            {
                url = ArticImageRequest.ImageUrl(imageId, iiif_url);
            }
            localId = PrepareLocalId($"IMAGEID_{((useHighResImage) ? "H" : "L")}_{imageId}_{iiif_url.GetHashCode()}")+".jpg";
        }

        protected override IEnumerator DoGetRequest()
        {
            requesting = true;
#if !UNITY_WEBGL
            string localPath = PrepareLocalPath();
            if (System.IO.File.Exists(localPath))
            {
                //Debug.LogError($" !! [{Time.realtimeSinceStartup}] Using image cache file {localPath}");
                url = "file://" + localPath;
            } else
            {
                Debug.LogError("Calling " + url);
            }
#endif
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        error = webRequest.error;
                        break;
                    case UnityWebRequest.Result.Success:
                        texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                        imageData = webRequest.downloadHandler.data;
#if !UNITY_WEBGL
                        SaveCache(localPath, imageData);
#endif
                        break;
                }
            }
            requesting = false;
        }
    }
}
