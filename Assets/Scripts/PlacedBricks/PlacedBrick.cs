using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New PlacedBrick", menuName = "ProcGen/PlacedBrick")]
public class PlacedBrick : ScriptableObject
{
    public int stackMax;
    public Sprite icon;
}
