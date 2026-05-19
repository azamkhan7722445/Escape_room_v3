using UnityEngine;

public enum CanopicJarType
{
    Imsety,     // Liver
    Hapi,       // Lungs
    Duamutef,   // Stomach
    Qebehsenuef // Intestines
}

public class CanopicJar : MonoBehaviour
{
    public CanopicJarType jarType;
}
