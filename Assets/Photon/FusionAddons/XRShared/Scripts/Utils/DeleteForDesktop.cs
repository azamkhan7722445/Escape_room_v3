using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Samples.IndustriesComponents
{
    public class DeleteForDesktop : MonoBehaviour
    {
        void OnEnable()
        {

            if (Pref_manager.Instance.pc)
            {
                DestroyImmediate(gameObject);
            }


        }
    }
}

