using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PlayItem : ItemBase
{
}

[CreateAssetMenu(fileName = "PlayItemListSO", menuName = "PetData/PlayItemList")]
public class PlayItemListSO : ScriptableObject
{

    public List<PlayItem> items;
}
