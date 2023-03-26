using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuItem : MonoBehaviour
{
    public Text Name;

    public GameObject Item { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        GetComponentInParent<ComponentsMenu>().OnClick(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialise(GameObject Prefab)
    {
        Item = Prefab;
        Name.text = Prefab.name;
    }
}
