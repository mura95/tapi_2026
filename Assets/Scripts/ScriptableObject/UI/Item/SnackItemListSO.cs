using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SnackItem : ItemBase
{
}

[CreateAssetMenu(fileName = "SnackItemListSO", menuName = "PetData/SnackItemList")]
public class SnackItemListSO : ScriptableObject
{

    public List<SnackItem> items;
}
