using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "ProcGen/Recipe")]
public class Recipe : ScriptableObject
{
    public bool shapeless;
    public RecipeShape[] recipeShapes;
    public byte output;
    public int outputQty;
}

[System.Serializable]
public class RecipeShape
{
    public byte slot1;
    public byte slot2;
    public byte slot3;
    public byte slot4;
    public byte slot5;
    public byte slot6;
    public byte slot7;
    public byte slot8;
    public byte slot9;
}
