using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RefreshList : MonoBehaviour
{
    public Text text;
    public void Drag()
    {
        text.text = "下拉刷新";
    }

    public void CouldRelease()
    {
        text.text = "释放刷新";
    }
}
