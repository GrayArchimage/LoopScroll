using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetCellData : MonoBehaviour
{
    public Text id;
    public Text userName;
    public Text msg;

    public void SetData(data data1)
    {
        id.text = data1.id.ToString();
        userName.text = data1.name;
        msg.text = data1.text;
    }
}
