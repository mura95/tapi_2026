using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FoodItem : ItemBase
{
}


[CreateAssetMenu(fileName = "FoodItemListSO", menuName = "PetData/FoodItemList")]
public class FoodItemListSO : ScriptableObject
{
    public List<FoodItem> items;
}
