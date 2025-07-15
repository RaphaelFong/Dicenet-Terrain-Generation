using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHandle : MonoBehaviour
{
    public Button button;
    public GameObject shop;

    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(ShopActive);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ShopActive()
    {
        shop.SetActive(true);
    }

    void ShopInactive()
    {
        shop.SetActive(false);
    }

    void Type1()
    {

    }


}
