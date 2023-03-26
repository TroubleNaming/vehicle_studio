using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Spawning;
using UnityEngine;

public class ComponentsMenu : MonoBehaviour
{
    public Transform Content;
    public GameObject MenuItemPrefab;
    public PrefabCatalogue Catalogue;

    void Start()
    {
        foreach(Transform child in Content.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in Catalogue.prefabs)
        {
            if (item.GetComponentsInChildren<MonoBehaviour>().Any(mb => mb is IComponent))
            {
                var control = GameObject.Instantiate(MenuItemPrefab, Content.transform).GetComponent<MenuItem>();
                control.Initialise(item);
            }
        }
    }

    public void OnClick(MenuItem control)
    {
        NetworkSpawnManager.Find(this).SpawnWithRoomScope(control.Item);
    }
}
