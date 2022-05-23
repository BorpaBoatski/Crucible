using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Delegates
{
    public delegate void VoidDelegate();
    public delegate void PassStringDelegate(string _Message);
    public delegate void PassFloatDelegate(float _Progress);
}
