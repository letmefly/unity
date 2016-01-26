using UnityEngine;
using System.Collections;

public class ButtonListController : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnButtonClick(GameObject sender)
    {
        Debug.Log("test:" + sender.name);
    }
}

