using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceScaler : MonoBehaviour
{
    [SerializeField] private float _scaleFactor;
    [SerializeField] private float _maxSize;
    [SerializeField] private float _minSize;
    
    private void Update()
    {
        var newScale = (Vector3.Distance(Camera.main.transform.position, transform.position)/_scaleFactor);
        
        if (newScale < _maxSize && newScale > _minSize)
            transform.localScale = new Vector3(newScale, newScale, newScale);
    }
}
