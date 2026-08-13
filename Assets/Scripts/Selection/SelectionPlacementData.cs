using System;
using System.Collections.Generic;
using UnityEngine;

public enum SelectionPlacementToolMode
{
    Move = 0,
    Rotate = 1,
    Scale = 2
}

[Serializable]
public sealed class SelectionPlacedItemData
{
    [SerializeField] private SelectionCategoryType categoryType;
    [SerializeField] private string itemId;
    [SerializeField] private Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);
    [SerializeField] private float rotationDegrees;
    [SerializeField] private float scale = 1f;
    [SerializeField] private int displayOrder;

    public SelectionCategoryType CategoryType => categoryType;
    public string ItemId => itemId ?? string.Empty;
    public Vector2 NormalizedPosition => normalizedPosition;
    public float RotationDegrees => rotationDegrees;
    public float Scale => scale > 0f ? scale : 1f;
    public int DisplayOrder => displayOrder;

    public SelectionPlacedItemData()
    {
    }

    internal SelectionPlacedItemData(
        SelectionCategoryType categoryType,
        string itemId,
        Vector2 normalizedPosition,
        float rotationDegrees,
        float scale,
        int displayOrder)
    {
        this.categoryType = categoryType;
        this.itemId = itemId;
        this.normalizedPosition = normalizedPosition;
        this.rotationDegrees = rotationDegrees;
        this.scale = scale > 0f ? scale : 1f;
        this.displayOrder = displayOrder;
    }

    internal SelectionPlacedItemData DeepCopy()
    {
        return new SelectionPlacedItemData(
            categoryType,
            itemId,
            normalizedPosition,
            rotationDegrees,
            Scale,
            displayOrder);
    }

    internal void SetNormalizedPosition(Vector2 value)
    {
        normalizedPosition = new Vector2(
            Mathf.Clamp01(value.x),
            Mathf.Clamp01(value.y));
    }

    internal void SetRotationDegrees(float value)
    {
        rotationDegrees = Mathf.DeltaAngle(0f, value);
    }

    internal void SetScale(float value)
    {
        scale = Mathf.Clamp(value, 0.1f, 5f);
    }

    internal void SetDisplayOrder(int value)
    {
        displayOrder = Mathf.Max(0, value);
    }
}

[Serializable]
public sealed class SelectionCompositionData
{
    [SerializeField] private string backgroundItemId;
    [SerializeField] private List<SelectionPlacedItemData> placements =
        new List<SelectionPlacedItemData>();

    public string BackgroundItemId => backgroundItemId ?? string.Empty;

    public SelectionPlacedItemData[] Placements
    {
        get
        {
            SelectionPlacedItemData[] copies = new SelectionPlacedItemData[placements.Count];
            for (int index = 0; index < placements.Count; index++)
            {
                copies[index] = placements[index]?.DeepCopy();
            }

            return copies;
        }
    }

    public SelectionCompositionData()
    {
    }

    internal SelectionCompositionData(
        string backgroundItemId,
        IEnumerable<SelectionPlacedItemData> sourcePlacements)
    {
        this.backgroundItemId = backgroundItemId;
        placements = new List<SelectionPlacedItemData>();
        if (sourcePlacements == null)
        {
            return;
        }

        foreach (SelectionPlacedItemData placement in sourcePlacements)
        {
            if (placement != null)
            {
                placements.Add(placement.DeepCopy());
            }
        }
    }
}
