using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		_webView = GameObject.Find("UniWebView").GetComponent<UniWebView>();
		_webView.BackgroundColor = new Color(1, 1, 1, 0);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	UniWebView _webView;


}
