using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum LayoutDirection
{
    Horizontal,
    Vertical,
}

[Serializable]
public class LayoutParameters
{
    public LayoutDirection layoutDirection;
    public float top = 0;
    public float bottom = 0;
    public float left = 0;
    public float right = 0;
    public float verticalSpace = 0;
    public float horizantalSpace = 0;
    public int preNum = 1;
}

public class SCVPP : MonoBehaviour
{
    public LayoutParameters layoutParameters;
    public bool autoLoadDataAtStart = true;
    public int countEverySingleItem = 1;
    public GameObject objCell;
    public RefreshList refreshList;
    public LoadMoreList loadMoreList;
    [Range(0, 100f)] public float dragBlur = 10f;
    [Range(0, 1000f)] public float startHandleData = 100f;

    private Transform tsmCurStart;
    private Transform tsmCurEnd;
    private int curStartIdx;
    private int curEndIdx;
    private int fullCount;
    private int itemCount;
    private Vector3 canvasScaler;
    private ScrollRect srScroll;
    private RectTransform rtsmContent;
    private RectTransform rtsmViewPort;
    private Transform _tsmRecycle;
    private Rect rectCell;

    private DataSource dataSource;
    private List<data> lData;

    private void Start()
    {
        srScroll = GetComponent<ScrollRect>();
        srScroll.horizontal = layoutParameters.layoutDirection == LayoutDirection.Horizontal;
        srScroll.vertical = layoutParameters.layoutDirection == LayoutDirection.Vertical;

        rtsmContent = srScroll.content;
        rtsmViewPort = srScroll.viewport;
        dataSource = GetComponent<DataSource>();
        rectCell = objCell.GetComponent<RectTransform>().rect;
        canvasScaler = GetComponentInParent<CanvasTools>().transform.localScale;
        lData = new List<data>();
        _setDataHandleLayout();
        GenerateRecycleTsm();

        if (autoLoadDataAtStart)
        {
            RefreshData();
        }
    }

    private void LoadMoreData()
    {
        List<data> results = dataSource.LoadData();
        lData.AddRange(results);
        int addCount = results.Count;
        int lessCount = (curEndIdx + 1) * countEverySingleItem - fullCount;

        int targetEndIdx = curEndIdx;
        if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
        {
            targetEndIdx = Mathf.Max(layoutParameters.preNum + 1, Mathf.CeilToInt(rtsmViewPort.rect.width * 1.1f / rectCell.width) + layoutParameters.preNum * 2);
        }
        else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
        {
            targetEndIdx = Mathf.Max(layoutParameters.preNum + 1, Mathf.CeilToInt(rtsmViewPort.rect.height * 1.1f / rectCell.height) + layoutParameters.preNum * 2);
        }

        if (targetEndIdx > itemCount - 1)
        {
            fullCount += addCount;
            GenerateCells(this.fullCount);
            return;
        }

        if (addCount >= lessCount && lessCount > 0)
        {
            float pos = 0;
            Transform temp = null;
            if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
            {
                pos = layoutParameters.top;
                for (var i = 0; i < lessCount; i++)
                {
                    temp = GetCell();
                    temp.SetParent(rtsmContent);
                    temp.GetComponent<SetCellData>().SetData(lData[(fullCount + i)]);
                    temp.localRotation = Quaternion.Euler(0, 0, 0);
                    temp.localScale = Vector3.one;
                    temp.localPosition = new Vector3(tsmCurEnd.localPosition.x, tsmCurEnd.localPosition.y - rectCell.height - layoutParameters.verticalSpace);
                    tsmCurEnd = GetTsmByIdx(-1);
                }
            }
            else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
            {
                for (var i = 0; i < lessCount; i++)
                {
                    temp = GetCell();
                    temp.SetParent(rtsmContent);
                    temp.GetComponent<SetCellData>().SetData(lData[(fullCount + i)]);
                    temp.localRotation = Quaternion.Euler(0, 0, 0);
                    temp.localScale = Vector3.one;
                    temp.localPosition = new Vector3(tsmCurEnd.localPosition.x + rectCell.width + layoutParameters.horizantalSpace, tsmCurEnd.localPosition.y);
                    tsmCurEnd = GetTsmByIdx(-1);
                }
            }
        }
        else if (addCount < lessCount && lessCount > 0)
        {
            float pos = 0;
            Transform temp = null;
            if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
            {
                pos = layoutParameters.top;
                for (var i = 0; i < addCount; i++)
                {
                    temp = _tsmRecycle.GetChild(_tsmRecycle.childCount - 1);
                    temp.SetParent(rtsmContent);
                    temp.localPosition = new Vector3(tsmCurEnd.localPosition.x, tsmCurEnd.localPosition.y - rectCell.height - layoutParameters.verticalSpace);
                    tsmCurEnd = GetTsmByIdx(-1);
                }
            }
            else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
            {
                for (var i = 0; i < addCount; i++)
                {
                    temp = _tsmRecycle.GetChild(_tsmRecycle.childCount - 1);
                    temp.SetParent(rtsmContent);
                    temp.localPosition = new Vector3(tsmCurEnd.localPosition.x + rectCell.width + layoutParameters.horizantalSpace, tsmCurEnd.localPosition.y);
                    tsmCurEnd = GetTsmByIdx(-1);
                }
            }
        }

        fullCount += addCount;
        itemCount = Mathf.CeilToInt(fullCount * 1.0f / Mathf.Max(1, countEverySingleItem));
        if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
        {
            rtsmContent.sizeDelta = new Vector2(Mathf.Max(rtsmViewPort.rect.width, itemCount * rectCell.width + (itemCount - 1) * layoutParameters.horizantalSpace + layoutParameters.left + layoutParameters.right), rtsmContent.sizeDelta.y);
        }
        else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
        {
            rtsmContent.sizeDelta = new Vector2(rtsmContent.sizeDelta.x, Mathf.Max(rtsmViewPort.rect.height, itemCount * rectCell.height + (itemCount - 1) * layoutParameters.verticalSpace + layoutParameters.top + layoutParameters.bottom));
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rtsmContent);
        tsmCurStart = GetTsmByIdx(0);
        tsmCurEnd = GetTsmByIdx(-1);
    }

    private void RefreshData()
    {
        dataSource.curIdx = 0;
        List<data> results = dataSource.LoadData();
        lData.Clear();
        lData.AddRange(results);
        GenerateCells(lData.Count);
    }

    private void Update()
    {
        if (srScroll.velocity.x == 0 && srScroll.velocity.y == 0)
        {
            return;
        }

        switch (layoutParameters.layoutDirection)
        {
            case LayoutDirection.Horizontal:
                _horizontalScrollFunc();
                _horizontalDataFunc();
                break;
            case LayoutDirection.Vertical:
                _verticalScrollFunc();
                _verticalDataFunc();
                break;
        }
    }

    private bool hadReachedDataHandleSymbol = false;
    private void _horizontalDataFunc()
    {
        if (refreshList != null)
        {
            refreshList.transform.localPosition = rtsmContent.localPosition;
            if (refreshList.transform.position.x > rtsmViewPort.position.x + startHandleData)
            {
                refreshList.CouldRelease();
                hadReachedDataHandleSymbol = true;
            }
            else if (refreshList.transform.position.x > rtsmViewPort.position.x + dragBlur)
            {
                refreshList.Drag();

                if (hadReachedDataHandleSymbol == true)
                {
                    RefreshData();
                    hadReachedDataHandleSymbol = false;
                }
            }
        }

        if (loadMoreList != null)
        {
            loadMoreList.transform.localPosition = rtsmContent.localPosition + new Vector3(rtsmContent.rect.width, 0, 0);
            if (loadMoreList.transform.position.x < rtsmViewPort.position.x + rtsmViewPort.rect.width * canvasScaler.y - startHandleData)
            {
                loadMoreList.CouldRelease();
                hadReachedDataHandleSymbol = true;
            }
            else if (loadMoreList.transform.position.x < rtsmViewPort.position.x + rtsmViewPort.rect.width * canvasScaler.y - dragBlur)
            {
                loadMoreList.Drag();

                if (hadReachedDataHandleSymbol == true)
                {
                    LoadMoreData();
                    hadReachedDataHandleSymbol = false;
                }
            }
        }
    }

    private void _verticalDataFunc()
    {
        if (refreshList != null)
        {
            refreshList.transform.localPosition = rtsmContent.localPosition;
            if (refreshList.transform.position.y < rtsmViewPort.position.y - startHandleData)
            {
                refreshList.CouldRelease();
                hadReachedDataHandleSymbol = true;
            }
            else if (refreshList.transform.position.y < rtsmViewPort.position.y - dragBlur)
            {
                refreshList.Drag();

                if (hadReachedDataHandleSymbol == true)
                {
                    RefreshData();
                    hadReachedDataHandleSymbol = false;
                }
            }
        }

        if (loadMoreList != null)
        {
            loadMoreList.transform.localPosition = rtsmContent.localPosition - new Vector3(0, rtsmContent.rect.height, 0);
            if (loadMoreList.transform.position.y > rtsmViewPort.position.y - rtsmViewPort.rect.height * canvasScaler.y + startHandleData)
            {
                loadMoreList.CouldRelease();
                hadReachedDataHandleSymbol = true;
            }
            else if (loadMoreList.transform.position.y > rtsmViewPort.position.y - rtsmViewPort.rect.height * canvasScaler.y + dragBlur)
            {
                loadMoreList.Drag();

                if (hadReachedDataHandleSymbol == true)
                {
                    LoadMoreData();
                    hadReachedDataHandleSymbol = false;
                }
            }
        }
    }

    private void _horizontalScrollFunc()
    {
        if (srScroll.velocity.x > 0 && tsmCurStart.position.x + rectCell.height * canvasScaler.y * layoutParameters.preNum > rtsmViewPort.position.x)
        {
            if (JudgeOverNumber(curStartIdx - 1))
                return;

            Move(false);
            tsmCurStart = GetTsmByIdx(0);
            tsmCurEnd = GetTsmByIdx(-1);
        }
        else if (srScroll.velocity.x < 0 && tsmCurEnd.position.x - rectCell.width * canvasScaler.x * (layoutParameters.preNum - 1) <= rtsmViewPort.position.x + rtsmViewPort.rect.width * canvasScaler.x)
        {
            if (JudgeOverNumber(curEndIdx + 1))
                return;

            Move(true);
            tsmCurStart = GetTsmByIdx(0);
            tsmCurEnd = GetTsmByIdx(-1);
        }
    }

    private void _verticalScrollFunc()
    {
        if (srScroll.velocity.y < 0 && tsmCurStart.position.y - rectCell.height * canvasScaler.y * layoutParameters.preNum < rtsmViewPort.position.y)
        {
            if (JudgeOverNumber(curStartIdx - 1))
                return;

            Move(false);
            tsmCurStart = GetTsmByIdx(0);
            tsmCurEnd = GetTsmByIdx(-1);
        }
        else if (srScroll.velocity.y > 0 && tsmCurEnd.position.y + rectCell.height * canvasScaler.y * (layoutParameters.preNum - 1) >= rtsmViewPort.position.y - rtsmViewPort.rect.height * canvasScaler.y)
        {
            if (JudgeOverNumber(curEndIdx + 1))
                return;

            Move(true);
            tsmCurStart = GetTsmByIdx(0);
            tsmCurEnd = GetTsmByIdx(-1);
        }
    }

    private Transform GetTsmByIdx(int idx)
    {
        return rtsmContent.GetChild((idx >= 0 ? 0 : 1) * rtsmContent.childCount + idx);
    }

    private void Move(bool setLatest = false)
    {
        if (setLatest)
        {
            curEndIdx++;
            SetEnoughTsm2Target(0, -1);
            curStartIdx++;
            return;
        }

        curStartIdx--;
        SetEnoughTsm2Target(-1, 0);
        curEndIdx--;
    }

    private void SetEnoughTsm2Target(int fromIdx, int targetIdx)
    {
        fromIdx = (fromIdx >= 0 ? 0 : 1) * itemCount + fromIdx;
        targetIdx = (targetIdx >= 0 ? 0 : 1) * itemCount + targetIdx;

        if (fromIdx > targetIdx)
        {
            _fromIdxHigherThanTargetIdx(fromIdx, targetIdx);
        }
        else if (fromIdx < targetIdx)
        {
            _targetIdxHigherThanFromIdx(fromIdx, targetIdx);
        }
    }

    private void _targetIdxHigherThanFromIdx(int fromIdx, int targetIdx)
    {
        int fromNum = Mathf.Min(countEverySingleItem, fullCount - curStartIdx* countEverySingleItem);
        int targetNum = Mathf.Min(countEverySingleItem, fullCount - curEndIdx * countEverySingleItem);

        float pos = 0;
        if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
        {
            pos += layoutParameters.top;
        }
        else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
        {
            pos += layoutParameters.left;
        }

        Transform temp = null;
        int k = 0; // item内部idx
        for (; fromNum > 0 || targetNum > 0; fromNum--, targetNum--)
        {
            if (targetNum <= 0 && fromNum > 0)
            {
                RecycleCell(0);
                continue;
            }
            else if (targetNum > 0 && fromNum > 0)
            {
                temp = rtsmContent.GetChild(0);
            }
            else if (targetNum > 0 && fromNum <= 0)
            {
                temp = GetCell();
                temp.SetParent(rtsmContent);
            }

            if (temp != null)
            {
                temp.SetAsLastSibling();
                temp.GetComponent<SetCellData>().SetData(lData[(curEndIdx * countEverySingleItem + k++)]);
                if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
                {
                    temp.localPosition = new Vector3(tsmCurEnd.localPosition.x + layoutParameters.horizantalSpace + rectCell.width, pos);
                    pos -= rectCell.height + layoutParameters.verticalSpace;
                }
                else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
                {
                    temp.localPosition = new Vector3(pos, tsmCurEnd.localPosition.y - layoutParameters.verticalSpace - rectCell.height);
                    pos += rectCell.width + layoutParameters.horizantalSpace;
                }
            }
        }
    }

    private void _fromIdxHigherThanTargetIdx(int fromIdx, int targetIdx)
    {
        int fromNum = Mathf.Min(countEverySingleItem, fullCount - curEndIdx * countEverySingleItem);
        int targetNum = Mathf.Min(countEverySingleItem, fullCount - curStartIdx * countEverySingleItem);

        float pos = 0;
        if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
        {
            pos += layoutParameters.top;
        }
        else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
        {
            pos += layoutParameters.left;
        }

        Transform temp = null;
        int k = 0; // item内部idx

        for (; fromNum > 0 || targetNum > 0; fromNum--, targetNum--)
        {
            if (targetNum <= 0 && fromNum > 0)
            {
                RecycleCell(rtsmContent.childCount - 1);
                continue;
            }
            else if (targetNum > 0 && fromNum > 0)
            {
                temp = rtsmContent.GetChild(rtsmContent.childCount - 1);
            }
            else if (targetNum > 0 && fromNum <= 0)
            {
                temp = GetCell();
                temp.SetParent(rtsmContent);
            }

            if (temp != null)
            {
                temp.SetAsFirstSibling();
                temp.GetComponent<SetCellData>().SetData(lData[(curStartIdx * countEverySingleItem + k++)]);

                if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
                {
                    temp.localPosition = new Vector3(tsmCurStart.localPosition.x - layoutParameters.horizantalSpace - rectCell.width, pos);
                    pos -= rectCell.height + layoutParameters.verticalSpace;
                }
                else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
                {
                    temp.localPosition = new Vector3(pos, tsmCurStart.localPosition.y + layoutParameters.verticalSpace + rectCell.height);
                    pos += rectCell.width + layoutParameters.horizantalSpace;
                }
            }
        }
    }

    private bool JudgeOverNumber(int idx)
    {
        return idx > itemCount - 1 || idx < 0;
    }

    private void GenerateCells(int fullCount)
    {
        RecycleAllCell();

        this.fullCount = fullCount;
        itemCount = Mathf.CeilToInt(fullCount * 1.0f / Mathf.Max(1, countEverySingleItem));
        curStartIdx = 0;
        if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
        {
            rtsmContent.sizeDelta = new Vector2(Mathf.Max(rtsmViewPort.rect.width, itemCount * rectCell.width + (itemCount - 1) * layoutParameters.horizantalSpace + layoutParameters.left + layoutParameters.right), rtsmContent.sizeDelta.y);
            curEndIdx = Mathf.Max(layoutParameters.preNum + 1, Mathf.CeilToInt(rtsmViewPort.rect.width * 1.1f / rectCell.width) + layoutParameters.preNum * 2);
        }
        else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
        {
            rtsmContent.sizeDelta = new Vector2(rtsmContent.sizeDelta.x, Mathf.Max(rtsmViewPort.rect.height, itemCount * rectCell.height + (itemCount - 1) * layoutParameters.verticalSpace + layoutParameters.top + layoutParameters.bottom));
            curEndIdx = Mathf.Max(layoutParameters.preNum + 1, Mathf.CeilToInt(rtsmViewPort.rect.height * 1.1f / rectCell.height) + layoutParameters.preNum * 2);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rtsmContent);

        for (int i = curStartIdx, k = 0; i <= curEndIdx; i++)
        {
            for (int j = 0; j < countEverySingleItem; j++, k++)
            {
                if (k >= fullCount)
                {
                    goto Layout;
                }

                Transform tsm = GetCell();
                tsm.SetParent(rtsmContent.transform);
                tsm.GetComponent<SetCellData>().SetData(lData[(i * countEverySingleItem + j)]);
                tsm.localRotation = Quaternion.Euler(0, 0, 0);
                tsm.localScale = Vector3.one;

                if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
                {
                    tsm.localPosition = new Vector3(layoutParameters.left + Mathf.Floor(k / countEverySingleItem) * (rectCell.width + layoutParameters.horizantalSpace),
                        -(layoutParameters.top + k % countEverySingleItem * (rectCell.height + layoutParameters.verticalSpace)), 0);
                }
                else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
                {
                    tsm.localPosition = new Vector3(layoutParameters.left + (k % countEverySingleItem) * (rectCell.width + layoutParameters.horizantalSpace),
                        -(layoutParameters.top + Mathf.Floor(k / countEverySingleItem) * (rectCell.height + layoutParameters.verticalSpace)), 0);
                }
            }
        }

    Layout:
        curEndIdx = Mathf.Min(itemCount - 1, curEndIdx);
        tsmCurStart = GetTsmByIdx(0);
        tsmCurEnd = GetTsmByIdx(-1);
    }

    private void RecycleCell(int idx)
    {
        if (_tsmRecycle == null) GenerateRecycleTsm();
        rtsmContent.GetChild(idx).SetParent(_tsmRecycle);
        _tsmRecycle.GetChild(_tsmRecycle.childCount - 1).localPosition = Vector3.zero;
    }

    private void RecycleCell(Transform tsm)
    {
        tsm.SetParent(_tsmRecycle);
        tsm.localPosition = Vector3.zero;
    }

    private void RecycleAllCell()
    {
        while (rtsmContent.childCount > 0)
            rtsmContent.GetChild(rtsmContent.childCount - 1).SetParent(_tsmRecycle);
    }

    private Transform GetCell()
    {
        if (_tsmRecycle.childCount > 0)
        {
            return _tsmRecycle.GetChild(_tsmRecycle.childCount - 1);
        }
        return GameObject.Instantiate(objCell).GetComponent<Transform>();
    }

    private void GenerateRecycleTsm()
    {
        var obj = new GameObject("_recycle");
        obj.transform.SetParent(transform);
        obj.transform.position = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.SetActive(false);
        _tsmRecycle = obj.GetComponent<Transform>();
    }

    private void _setDataHandleLayout()
    {
        RectTransform rectRefreshListItem = refreshList == null ? null : refreshList.GetComponent<RectTransform>();
        RectTransform rectLoadMoreListItem = refreshList == null ? null : loadMoreList.GetComponent<RectTransform>();
        if (layoutParameters.layoutDirection == LayoutDirection.Horizontal)
        {
            rectRefreshListItem.pivot = new Vector2(1, 1);
            rectRefreshListItem.anchorMax = new Vector2(1, 1);
            rectRefreshListItem.anchorMin = new Vector2(1, 1);

            rectLoadMoreListItem.pivot = new Vector2(0, 0);
            rectLoadMoreListItem.anchorMax = new Vector2(0, 1);
            rectLoadMoreListItem.anchorMax = new Vector2(0, 1);
        }
        else if (layoutParameters.layoutDirection == LayoutDirection.Vertical)
        {
            rectRefreshListItem.pivot = new Vector2(0, 0);
            rectRefreshListItem.anchorMax = new Vector2(0, 1);
            rectRefreshListItem.anchorMax = new Vector2(0, 1);

            rectLoadMoreListItem.pivot = new Vector2(0, 1);
            rectLoadMoreListItem.anchorMax = new Vector2(0, 1);
            rectLoadMoreListItem.anchorMax = new Vector2(0, 1);
        }
    }
}
