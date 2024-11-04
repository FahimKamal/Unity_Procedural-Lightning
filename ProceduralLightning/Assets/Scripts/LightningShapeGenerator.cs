using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningShapeGenerator : MonoBehaviour
{
    public List<LightningBranch> CreateLightningShape(Vector3 originPoint, Vector3 impactPoint)
    {
        // Creating a new initial lightning bolt.
        LightningBranch initialLightningBranch = CreateLightningBranch(originPoint, impactPoint);
        List<LightningBranch> lightningBranches = new List<LightningBranch>() { initialLightningBranch };

        return lightningBranches;
    }

    private LightningBranch CreateLightningBranch(Vector3 originPoint, Vector3 impactPoint)
    {
        throw new System.NotImplementedException();
    }
}
