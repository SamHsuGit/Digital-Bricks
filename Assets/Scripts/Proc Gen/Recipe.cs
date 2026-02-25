using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "ProcGen/Recipe")]
public class Recipe : ScriptableObject
{
    public bool colorless;
    public RecipeShape[] recipeShapes;
    public byte outputID;
    public string outputPlacedBrickID;
    public int outputPlacedBrickColorID;
    public int outputQty;
}

[System.Serializable]
public class RecipeShape
{
    public int slot1;
    public int slot2;
    public int slot3;
    public int slot4;
    public int slot5;
    public int slot6;
    public int slot7;
    public int slot8;
    public int slot9;
}
