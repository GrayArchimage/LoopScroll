using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class data
{
    public int id;
    public string name;
    public string text;
}
public class DataSource : MonoBehaviour
{
    public int curIdx = 0;
    public List<data> LoadData()
    {
        List<data> lData = new List<data>();
        for (var i = 0; i < 50; i++, curIdx++)
        {
            data a = new data();
            a.id = curIdx;
            a.name = "xxxx" + curIdx;
            a.text = "balabala" + curIdx;
            lData.Add(a);
        }

        return lData;
    }
}
