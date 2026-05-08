using Fusion;
using Fusion.XR.Shared;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 
 * NetworkProjectionPainter received a texture modification request from the gun that fired the projectile with PaintAtUV() method
 * Then it sends the request to the local `ProjectionPainter` component, which will actually perform the texture modification
 * When the texture modification is terminated, the `NetworkProjectionPainter` is informed by the OnPaintAtUV() callback,
 * The player having the state authority updates the network list which contains all the information on the impacts,
 * So, remote players can update the texture of the object using their local `ProjectionPainter` component.
 * 
 **/
public class NetworkProjectionPainter : NetworkBehaviour, ProjectionPainter.IProjectionListener
{
    [System.Serializable]
    public struct ProjectionInfo:INetworkStruct {
        public Vector2 uv;
        public Color color;
        public float sizeModifier;
        public int paintId;
    }

    ProjectionPainter painter;

    const int MAX_PROJECTIONS = 200;

    int nextStoredPaintId = 0;
    int lastPaintedPaintId = -1;

    [Networked]
    [Capacity(MAX_PROJECTIONS)]
    public NetworkLinkedList<ProjectionInfo> ProjectionInfosLinkedList { get; }

    ChangeDetector changeDetector;

    private void Awake()
    {
        painter = GetComponentInChildren<ProjectionPainter>();
    }

    public override void Spawned()
    {
        base.Spawned();
        changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        OnInfosChanged();
    }

    public override void Render()
    {
        base.Render();
        foreach (var changedVar in changeDetector.DetectChanges(this))
        {
            if (changedVar == nameof(ProjectionInfosLinkedList))
            {
                OnInfosChanged();
            }
        }
    }

    ProjectionInfo[] infoPendingPaint = new ProjectionInfo[MAX_PROJECTIONS];
    void OnInfosChanged()
    {
        if (ProjectionInfosLinkedList.Count == 0) return;
        // We only need to paint on non state authority: the ProjectionInfosLinkedList has been updated as the state authority detected a paint, through OnPaintAtUV
        if (Object.HasStateAuthority) return;

        // The interesting part is the new added entries (with a paint id greater than lastPaintedPaintId), so the end of the list. To avoid going through all the list, we start from the end
        int paintIndex = 0;
        for (int i = ProjectionInfosLinkedList.Count - 1;i >= 0; i--)
        {
            var info = ProjectionInfosLinkedList.Get(i);
            if (lastPaintedPaintId >= info.paintId)
            {
                break;
            }
            infoPendingPaint[paintIndex] = info;
            paintIndex++;
        }

        // paintIndex now contains the number of item to paint, in infoPendingPaint. We paint it backwards (as we started to fill infoPendingPaint from the end), starting from the last info
        paintIndex = paintIndex - 1;
        while (paintIndex >= 0)
        {
            var info = infoPendingPaint[paintIndex];
            painter.PaintAtUV(info.uv, info.sizeModifier, info.color);
            // We track that this paintId has been handled. paintId always increase
            lastPaintedPaintId = info.paintId;

            // Incase of state authority change, we keep nextStoredPaintId up to date, to continue increasing it if we become the next state authority
            if (nextStoredPaintId <= lastPaintedPaintId)
            {
                nextStoredPaintId = lastPaintedPaintId + 1;
            }
            paintIndex--;
        }
    }



    #region IProjectionListener
    // Called by the actual painter when a permanent paint is done
    bool storingPaintInfo = false;
    public async void OnPaintAtUV(Vector2 uv, float sizeModifier, Color color)
    {
        if (Object.HasStateAuthority)
        {
            while (storingPaintInfo) {
                // TODO Remove log
                Debug.LogError("* waiting to store paint info *");
                await AsyncTask.Delay(10);
            }
            storingPaintInfo = true;

            try {
                while (ProjectionInfosLinkedList.Count >= MAX_PROJECTIONS)
                {
                    int count = ProjectionInfosLinkedList.Count;
                    ProjectionInfosLinkedList.Remove(ProjectionInfosLinkedList[0]);
                    if (ProjectionInfosLinkedList.Count == count)
                    {
                        Debug.LogError("Unable to edit linked list");
                        return;
                    }
                }

                ProjectionInfosLinkedList.Add(new ProjectionInfo { uv = uv, sizeModifier = sizeModifier, color = color, paintId = nextStoredPaintId });
            }
            catch (System.Exception e)
            {
                Debug.LogError("Unable to paint: "+e.Message+"\n"+e.ToString());
            }
            nextStoredPaintId++;
            storingPaintInfo = false;
        }
    }
    #endregion

    // Used when someone wants to paint the object
    public void PaintAtUV(Vector2 uv, float sizeModifier, Color color)
    {
        if (Object.HasStateAuthority)
        {
            painter.PaintAtUV(uv, sizeModifier, color);
        }
        else
        {
            painter.PrePaintAtUV(uv, sizeModifier, color);
        }
    }
}
