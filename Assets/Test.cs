using UnityEngine;
using ResetCore.NAsset;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
        AssetLoader.LoadAndCallback(Bundles.cube, callBack: () =>
        {
            GameObject obj = GameObject.Instantiate(AssetLoader.GetGameObjectByR(R.cube_Cube));
        });
	}
	
	// Update is called once per frame
	void Update () {

    }
}
