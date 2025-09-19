using System.Collections.Generic;
using UnityEngine;

public abstract class UITool : MonoBehaviour
{
    public virtual bool ValidatePoints(List<GF_GridPoint> gridPoints) => true;

    public virtual void OnToolActivated(List<GF_GridPoint> gridPoints) { }

    public virtual void OnToolDeactivated() { }
}
