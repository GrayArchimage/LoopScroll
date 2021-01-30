using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadMoreList : MonoBehaviour
{
    public Text text;
    public void Drag()
    {
        text.text = "上拉刷新";
    }

    public void CouldRelease()
    {
        text.text = "释放刷新";
    }
}
