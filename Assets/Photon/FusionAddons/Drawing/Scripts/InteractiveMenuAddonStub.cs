using UnityEngine;

namespace Fusion.Addons.InteractiveMenuAddon
{
    public interface IColorProvider
    {
        Color CurrentColor { get; }
    }

    public class InteractiveMenu : MonoBehaviour
    {
        public virtual void EnableInteractiveMenu(bool enable) { }
    }
}
