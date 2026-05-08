using Fusion.XR.Shared;
using System.Collections.Generic;
using UnityEngine;

/**
 * 
 * ProjectionPainter is in charge to perform the object texture modification using the impact parameters received (UV coordinates, size & color of the projectile).
 * To do so : 
 *   - the texture of the object to paint is replaced by a temporary camera render texture,
 *   - an impact brush in displayed in front of the texture at the correct uv coordinates at each hit, 
 *   - then it is captured by a camera which update the render texture so the player can see the new shooting impact,
 *   - on a regular basis, the definitive object texture is updated by a new texture which includes all previous impacts. This operation is not performed on every hit to avoid resource consumption. 
 * 
 * 
 **/
public class ProjectionPainter : MonoBehaviour
{
    public MeshRenderer debugCaptureProjectionRenderer;

    public int textureResolution = 2048;
    [Header("Brush (created if empty)")]
    public List<Renderer> brushes = new List<Renderer>();
    [Header("Temporary (not saved) brush (created if empty)")]
    public bool prepareForPrePaints = true;
    public List<Renderer> temporaryBrushes = new List<Renderer>();

    [Header("Default brushes")]
    public Color brushColor = Color.red;
    public float brushSize = 0.002f;


    [Header("Automatically filled")]
    [SerializeField] private RenderTexture captureProjectionTexture;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material capturedMaterial;
    [SerializeField] private Material captureProjectionMaterial;
    [SerializeField] private GameObject captureRoot;
    [SerializeField] private GameObject captureProjectionSurface;
    [SerializeField] private string captureProjectionSurfaceShader = "Universal Render Pipeline/Unlit";
    [SerializeField] private string brushShader = "Universal Render Pipeline/Unlit";
    [SerializeField] private Camera captureCamera;
    [SerializeField] private Vector3 captureSpaceBase = new Vector3(-100,-100,-100);
    private Texture2D capturedTexture;

    int currentBrushIndex = 0;
    int currentTemporaryBrushIndex = 0;
    Renderer currentBrush;
    Renderer currentTemporaryBrush;
    Dictionary<Renderer, float> temporaryBrushEndOfLife = new Dictionary<Renderer, float>();
    
    static int PainterCount = 0;

    private bool isPainting = false;
    private bool wasCapturedThisUpdate = false;
    private bool pendingCapture = false;
    public interface IProjectionListener {
        public void OnPaintAtUV(Vector2 uv, float sizeModifier, Color color);
    }

    List<IProjectionListener> listeners = new List<IProjectionListener>();

    private void Awake()
    {
        listeners = new List<IProjectionListener>(GetComponentsInParent<IProjectionListener>());

        if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
        capturedTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.ARGB32, false);
        if (!captureProjectionTexture) captureProjectionTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        if (!capturedMaterial) capturedMaterial = new Material(Shader.Find(captureProjectionSurfaceShader));
        if (!captureProjectionMaterial) captureProjectionMaterial = new Material(meshRenderer.material);
        capturedMaterial.mainTexture = meshRenderer.material.mainTexture;
        captureProjectionMaterial.mainTexture = captureProjectionTexture;
        meshRenderer.sharedMaterial = captureProjectionMaterial;

        if (debugCaptureProjectionRenderer) debugCaptureProjectionRenderer.material = capturedMaterial;

        captureRoot = new GameObject(name+"-Capture");
        captureRoot.transform.position = captureSpaceBase + PainterCount * 2 * Vector3.left;
        captureProjectionSurface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(captureProjectionSurface.GetComponent<Collider>());
        captureProjectionSurface.GetComponent<MeshRenderer>().material = capturedMaterial;
        captureProjectionSurface.transform.parent = captureRoot.transform;
        captureProjectionSurface.transform.position = captureRoot.transform.position + Vector3.forward;
        captureProjectionSurface.transform.rotation = Quaternion.identity;
        // Source: https://forum.unity.com/threads/getting-output-textures-from-a-surface-shader.690619/
        captureCamera = captureRoot.AddComponent<Camera>();
        captureCamera.orthographic = true;
        captureCamera.orthographicSize = 0.5f;
        captureCamera.targetTexture = captureProjectionTexture;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0);
        //Vector3 captureProjectionSurfaceScale = captureProjectionSurface.transform.localScale / (float)((Screen.height / 2.0) / Camera.main.orthographicSize);
        var scale = captureProjectionSurface.transform.localScale;
        captureCamera.aspect = scale.x / scale.y;
        captureCamera.rect = new Rect(0, 0, scale.x, scale.y);
        captureCamera.enabled = false;
        captureCamera.allowHDR = false; 
        PainterCount++;

        RefreshCapturedMaterial();

        if (brushes.Count == 0)
        {
            var brushMaterial = new Material(Shader.Find(brushShader));
            brushMaterial.color = brushColor;
            const int brushCount = 10;
            for (int i = 0; i < brushCount; i++)
            {
                var brush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                brush.transform.localScale = brushSize * Vector3.one;
                Destroy(brush.GetComponent<Collider>());
                brush.transform.parent = captureProjectionSurface.transform;
                var renderer = brush.GetComponent<Renderer>();
                renderer.material = brushMaterial;
                renderer.enabled = false;
                brushes.Add(renderer);
            }
        }
        foreach (var brush in brushes)
        {
            brush.transform.parent = captureProjectionSurface.transform;
            brush.transform.localScale = brushSize * Vector3.one;
            brush.transform.localRotation = Quaternion.identity;
            brush.enabled = false;
        }

        if (prepareForPrePaints)
        {
            foreach (var brush in brushes)
            {
                var temporaryBrush = GameObject.Instantiate(brush);
                temporaryBrush.transform.parent = captureProjectionSurface.transform;
                temporaryBrush.transform.localScale = brushSize * Vector3.one;
                temporaryBrush.transform.localRotation = Quaternion.identity;
                temporaryBrush.enabled = false;
                temporaryBrushes.Add(temporaryBrush);
            }
        }

        currentBrushIndex = 0;
        currentBrush = brushes[0];
        currentTemporaryBrushIndex = 0;
        if (temporaryBrushes.Count > 0) currentTemporaryBrush = temporaryBrushes[0];

    }

    private void OnDestroy()
    {
        Destroy(capturedTexture);
        captureProjectionTexture.Release();
    }

    const float DEFAULT_BRUSH_DEPTH = 0.05f;
    public Vector3 UV2CaptureLocalPosition(Vector2 uv, float depth = DEFAULT_BRUSH_DEPTH)
    {
        //TODO Check that depth is clamped to be before the camera near clip plane
        var localPosition = new Vector3(uv.x - 0.5f, uv.y - 0.5f, -depth);
        return localPosition;
    }

    public Vector3 UV2CaptureWorldPosition(Vector2 uv, float depth = DEFAULT_BRUSH_DEPTH)
    {
        return captureProjectionSurface.transform.TransformPoint(UV2CaptureLocalPosition(uv, depth));
    }

    [ContextMenu("Capture")]
    public void Capture(bool force=false)
    {
        if(force == false && wasCapturedThisUpdate)
        {
            pendingCapture = true;
            return;
        }

        captureCamera.Render();
        wasCapturedThisUpdate = true;
        pendingCapture = false;
    }

    private void LateUpdate()
    {
        if (wasCapturedThisUpdate)
            wasCapturedThisUpdate = false;
    }

    [ContextMenu("RefreshCapturedMaterial")]
    public void RefreshCapturedMaterial()
    {
        // Hide temporary brushes
        var activeBrushes = new List<Renderer>();
        foreach (var temporaryBrush in temporaryBrushes)
        {
            if (temporaryBrush.enabled)
            {
                activeBrushes.Add(temporaryBrush);
                temporaryBrush.enabled = false;
            }
        }
        Capture(true);
        var activeRT = RenderTexture.active;
        RenderTexture.active = captureProjectionTexture;
        capturedTexture.ReadPixels(new Rect(0, 0, capturedTexture.width, capturedTexture.height), 0, 0);
        capturedTexture.Apply();
        capturedMaterial.mainTexture = capturedTexture;
        RenderTexture.active = activeRT;
        foreach (var activeBrush in activeBrushes) 
            activeBrush.enabled = true;
    }

    void Update()
    {
        foreach(var temporaryInfo in temporaryBrushEndOfLife)
        {
            if (temporaryInfo.Value < Time.time)
            {
                temporaryInfo.Key.enabled = false;
                temporaryBrushEndOfLife.Remove(temporaryInfo.Key);
                var index = temporaryBrushes.IndexOf(temporaryInfo.Key);
                if (prepaints.Contains(index)){
                    prepaints.Remove(index);
                }
                break;
            }
        }

        if (pendingCapture)
            Capture();
    }

  
    public void PaintAtUV(Vector2 uv, float sizeModifier = 1)
    {
        PaintAtUV(uv, sizeModifier, brushColor);
    }

    public void PaintAtUV(Vector2 uv, float sizeModifier, Color color)
    {
        var paintPosition = UV2CaptureLocalPosition(uv);
        Paint(paintPosition, sizeModifier, color);
        foreach (var listener in listeners)
        {
            listener.OnPaintAtUV(uv, sizeModifier, color);
        }
    }

    async void Paint(Vector3 paintPosition, float sizeModifier, Color color)
    {
        while (isPainting)
        {
            await AsyncTask.Delay(10);
        }
        isPainting = true;


        if (prepaints.Contains(currentBrushIndex))
        {
            temporaryBrushes[currentBrushIndex].enabled = false;
            prepaints.Remove(currentBrushIndex);
        }
        currentBrush.transform.localPosition = paintPosition;
        currentBrush.transform.localScale = sizeModifier * brushSize * Vector3.one;
        currentBrush.material.color = color;
        currentBrush.enabled = true;
        currentBrushIndex++;
        if(currentBrushIndex < brushes.Count)
        {
            Capture();
        }
        else
        {
            RefreshCapturedMaterial();
            foreach (var brush in brushes) brush.enabled = false;
            currentBrushIndex = 0;
        }
        currentBrush = brushes[currentBrushIndex];
        isPainting = false;
    }

    public void PrePaintAtUV(Vector2 uv, float sizeModifier, Color color)
    {
        var paintPosition = UV2CaptureLocalPosition(uv);
        PrePaint(paintPosition, sizeModifier, color);
    }

    List<int> prepaints = new List<int>();
    void PrePaint(Vector3 paintPosition, float sizeModifier, Color color, float duration = 3f)
    { 
        if(prepaints.Count == 0)
        {
            currentTemporaryBrushIndex = currentBrushIndex;
            currentTemporaryBrush = temporaryBrushes[currentTemporaryBrushIndex];
        } else
        {
            currentTemporaryBrushIndex = (prepaints[prepaints.Count - 1] + 1) % temporaryBrushes.Count;
            currentTemporaryBrush = temporaryBrushes[currentTemporaryBrushIndex];
        }
        prepaints.Add(currentTemporaryBrushIndex);
        TemporaryPaint(paintPosition, sizeModifier * 1f, color, duration);
    }
    void TemporaryPaint(Vector3 paintPosition, float sizeModifier, Color color, float duration = 3f)
    {
        if (currentTemporaryBrush == null) return;
        temporaryBrushEndOfLife[currentTemporaryBrush] = Time.time + duration;
        currentTemporaryBrush.transform.localPosition = paintPosition;
        currentTemporaryBrush.transform.localScale = sizeModifier * brushSize * Vector3.one;
        currentTemporaryBrush.material.color = color;
        currentTemporaryBrush.enabled = true;
        currentTemporaryBrushIndex++;
        Capture();
        if (currentTemporaryBrushIndex >= temporaryBrushes.Count)
        {
            currentTemporaryBrushIndex = 0;
        }
        currentTemporaryBrush = temporaryBrushes[currentTemporaryBrushIndex];
    }

}
