using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AutoTiling : MonoBehaviour
{
    void Start()
    {
        var rend = GetComponent<Renderer>();
        rend.material = new Material(rend.material); 
        Vector3 scale = transform.lossyScale;
        rend.material.mainTextureScale = new Vector2(scale.x, scale.y);
    }
}
