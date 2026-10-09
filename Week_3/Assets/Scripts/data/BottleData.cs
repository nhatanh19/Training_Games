using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BottleData
{
    [SerializeField] private List<Color> _layerColors = new List<Color>();

    public List<Color> LayerColors => _layerColors;
    public BottleData(){
        _layerColors = new List<Color>();
    }
    public BottleData(List<Color> colors){
        _layerColors = colors != null ? new List<Color>(colors) : new List<Color>();
    }
}