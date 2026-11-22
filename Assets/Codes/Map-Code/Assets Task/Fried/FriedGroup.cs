// WHERE TO PUT THIS FILE:  Assets Task / Fried
// ------------------------------------------------------------
using UnityEngine;

public class FriedGroup : MonoBehaviour
{
    [Tooltip("How many of this set's platforms are used (-1 = all of them). The others go back to the normal layer.")]
    public int nodeCount = -1;

    [Tooltip("ON: the flash order is shuffled every run.\nOFF: the order follows the Hierarchy, top to bottom.")]
    public bool randomizeOrder = true;
}