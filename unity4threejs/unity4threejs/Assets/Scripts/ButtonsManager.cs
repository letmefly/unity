using UnityEngine;
using System.Collections;

public class ButtonsManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnButtonClick(string testCaseName) {
		Debug.Log ("Button clicked" + testCaseName);
	}
}
