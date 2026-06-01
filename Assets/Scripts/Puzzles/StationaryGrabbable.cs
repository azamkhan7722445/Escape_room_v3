using UnityEngine;
using Fusion.XR.Shared.Grabbing;

namespace Fusion.XR.Shared.Grabbing
{
    public class StationaryGrabbable : Grabbable
    {
        public override void Follow(Transform followedTransform, Vector3 localPositionOffsetToFollowed, Quaternion localRotationOffsetTofollowed)
        {
            // Do nothing to stay in place
        }
    }
}
