using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpyController : MonoBehaviour
{
    public TF2Player player;
    public InvisWatch invisWatch;

    private void Awake()
    {
        player = transform.root.GetComponent<TF2Player>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public bool Invisibility()
    {
        return invisWatch.Invisibility();
    }

    public bool ShowAvailble()
    {
       return  invisWatch.ShowAvailble();
    }
}
