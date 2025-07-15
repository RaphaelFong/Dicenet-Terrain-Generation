using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dicenet : MonoBehaviour
{
    public Vector3[] offsets; // an array of offsets where the first Vec3 is the anchor 0,0,0
                              // the rest of the offsets are relative to anchor

    public Dicenet(Vector3[] offsets)
    {
        this.offsets = offsets;
    }
}
