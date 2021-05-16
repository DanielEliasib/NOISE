using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "NOISE/ColorTemplate", order = 1)]
public class ColorTemplate : ScriptableObject
{
    public Color[] _Colors;
}