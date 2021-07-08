using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterCreative
{
    public class CreativeCell : InventoryCell, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
    {
        void Awake()
        {
            cellType = CellType.Chest;
            slot = GetComponent<RawImage>();
            itemImage = transform.GetChild(1).GetComponent<Image>();
            amount = transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            overlay = transform.GetChild(3).GetComponent<RawImage>();
        }
        
        public new void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        // necessary to prevent it from clicking if you scroll
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2) lastClickTime = Time.time;
            base.OnPointerDown(eventData);
        }
    }

    public class CreativeUI : InventoryExtensions
    {
        private void Awake()
        {
            cellsParent = (RectTransform)transform.GetChild(0).GetChild(0).GetChild(0);
            cellPrefab = Main.CreativeCell;
            var cells = new InventoryCell[ItemManager.Instance.allItems.Count];
            for (var i = 0; i < ItemManager.Instance.allItems.Count; i++)
            {
                var cell = Instantiate(cellPrefab).GetComponent<InventoryCell>();
                cell.transform.SetParent(cellsParent, false);
                var item = ScriptableObject.CreateInstance<InventoryItem>();
                item.Copy(ItemManager.Instance.allItems[i], 0);
                cell.currentItem = item;
                cell.UpdateCell();
                cells[i] = cell;
            }
        }
        public override void UpdateCraftables() { }
        public GameObject cellPrefab;
        public RectTransform cellsParent;
    }
}