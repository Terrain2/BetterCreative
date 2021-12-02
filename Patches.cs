using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterCreative
{
    [HarmonyPatch(typeof(InventoryCell))]
    class CreativeInventoryPatches
    {
        [HarmonyPatch(nameof(InventoryCell.SetItem)), HarmonyPrefix]
        static bool SetItem(InventoryCell __instance, ref InventoryItem __result, InventoryItem pointerItem, PointerEventData eventData)
        {
            if (!(__instance is CreativeCell)) return true;
            int amount = pointerItem.amount;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                amount = 1;
            }
            if (pointerItem.Compare(__instance.currentItem)) amount = -1;
            amount = pointerItem.amount - amount;
            if (amount == 0)
            {
                __result = null;
                return false;
            }
            var item = ScriptableObject.CreateInstance<InventoryItem>();
            item.Copy(pointerItem, amount);
            __result = item;
            return false;
        }

        [HarmonyPatch(nameof(CreativeCell.PickupItem)), HarmonyPrefix]
        static bool PickupItem(InventoryCell __instance, ref InventoryItem __result, PointerEventData eventData)
        {
            if (!(__instance is CreativeCell)) return true;
            var item = ScriptableObject.CreateInstance<InventoryItem>();
            var amount = 1;
            if (__instance.currentItem.stackable) switch (eventData.button)
                {
                    case PointerEventData.InputButton.Middle: amount = __instance.currentItem.max; break;
                    case PointerEventData.InputButton.Right: amount = __instance.currentItem.max / 2; break;
                }
            item.Copy(__instance.currentItem, amount);
            __result = item;
            return false;
        }

        [HarmonyPatch(nameof(InventoryCell.ShiftClick)), HarmonyPrefix]
        static bool ShiftClick(InventoryCell __instance, ref bool __result)
        {
            if (!(__instance is CreativeCell)) return true;
            if (!InventoryUI.Instance.CanPickup(__instance.currentItem))
            {
                __result = false;
                return false;
            }
            var item = ScriptableObject.CreateInstance<InventoryItem>();
            item.Copy(__instance.currentItem, __instance.currentItem.stackable ? __instance.currentItem.max : 1);
            InventoryUI.Instance.AddItemToInventory(item);
            __result = true;
            return false;
        }

        [HarmonyPatch(nameof(InventoryCell.RemoveItem)), HarmonyPrefix]
        static bool RemoveItem(InventoryCell __instance) => !(__instance is CreativeCell);

        [HarmonyPatch(nameof(InventoryCell.DoubleClick)), HarmonyPrefix]
        static bool DoubleClick(InventoryCell __instance) => !(__instance is CreativeCell);
    }

    [HarmonyPatch]
    class BetterCreativePatches
    {
        [HarmonyPatch(typeof(OtherInput), nameof(OtherInput.Awake)), HarmonyPostfix]
        static void Awake(OtherInput __instance)
        {
            if (!(Main.dontDestroy = GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative)) return;
            var creative = MonoBehaviour.Instantiate(Main.CreativeMenu).GetComponent<CreativeUI>();
            creative.transform.SetParent(__instance.handcrafts.transform.parent, false);
            creative.transform.SetSiblingIndex(0);
            var button = MonoBehaviour.Instantiate(Main.CreativeButton).GetComponent<Button>();
            button.transform.SetParent(__instance.handcrafts.transform.GetChild(1).GetChild(1), false);
            button.onClick.AddListener(() =>
            {
                __instance.handcrafts.gameObject.SetActive(false);
                creative.gameObject.SetActive(true);
                __instance._currentCraftingUiMenu = creative;
                InventoryUI.Instance.CraftingUi = creative;
            });
        }

        static DateTime lastJump = DateTime.MinValue;
        static DateTime jumpDelay = DateTime.MinValue;

        [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.MyInput)), HarmonyPostfix]
        static void MyInput()
        {
            if (GameManager.gameSettings.gameMode != GameSettings.GameMode.Creative) return;
            if (CommonFly.flying && Input.GetKeyDown(Main.noclip.Value)) CommonFly.noclip = !CommonFly.noclip;
            if (Input.GetKeyDown(InputManager.jump))
            {
                if (jumpDelay > DateTime.Now)
                {
                    lastJump = DateTime.MinValue;
                    return;
                }
                if (lastJump + TimeSpan.FromMilliseconds(500) > DateTime.Now)
                {
                    CommonFly.flying = !CommonFly.flying;
                    jumpDelay = DateTime.Now + TimeSpan.FromMilliseconds(500);
                    lastJump = DateTime.MinValue;
                    return;
                }
                lastJump = DateTime.Now;
            }
        }

        [HarmonyPatch(typeof(HitableMob), nameof(HitableMob.Hit)), HarmonyPrefix]
        static void HitMob(ref int damage)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative) damage = int.MaxValue;
        }

        [HarmonyPatch(typeof(HitableResource), nameof(HitableResource.Hit)), HarmonyPrefix]
        static void HitResource(ref int damage)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative) damage = Math.Sign(damage) * int.MaxValue;
        }

        [HarmonyPatch(typeof(StatusUI), nameof(StatusUI.Start)), HarmonyPostfix]
        static void StatusUI_Start(StatusUI __instance)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative) __instance.gameObject.SetActive(false);
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.DayLoop)), HarmonyPrefix]
        static bool DayLoop() => GameManager.gameSettings.gameMode != GameSettings.GameMode.Creative;

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.CheckMobSpawns)), HarmonyPrefix]
        static bool CheckMobSpawns() => GameManager.gameSettings.gameMode != GameSettings.GameMode.Creative;

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.SpawnMob)), HarmonyPrefix]
        static bool SpawnMob(ref int __result)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative)
            {
                __result = -1;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Hotbar), nameof(Hotbar.UseItem)), HarmonyPrefix]
        static bool UseItem() => GameManager.gameSettings.gameMode != GameSettings.GameMode.Creative;

        [HarmonyPatch(typeof(LootContainerInteract), nameof(LootContainerInteract.Start)), HarmonyPostfix]
        static void LootContainerInteract_Start(LootContainerInteract __instance)
        {
            if (GameManager.gameSettings?.gameMode == GameSettings.GameMode.Creative)
            {
                __instance.basePrice = 0;
                __instance.price = 0;
            }
        }

        [HarmonyPatch(typeof(LootExtra), nameof(LootExtra.CheckDrop)), HarmonyPrefix]
        static bool CheckDrop() => GameManager.gameSettings.gameMode != GameSettings.GameMode.Creative;

        [HarmonyPatch(typeof(MobZone), nameof(MobZone.ServerSpawnEntity)), HarmonyPrefix]
        static bool ServerSpawnEntity() => GameManager.gameSettings.gameMode != GameSettings.GameMode.Creative;

        [HarmonyPatch(typeof(MuckSettings.Settings), nameof(MuckSettings.Settings.Controls)), HarmonyPostfix]
        static void Controls(MuckSettings.Settings.Page page)
        {
            page.AddControlSetting("Noclip", Main.noclip);
            page.AddControlSetting("Precision Select Trigger", Main.precisionTriggers);
        }

        [HarmonyPatch(typeof(ChatBox), nameof(ChatBox.ChatCommand)), HarmonyPostfix]
        static void ChatCommand(ChatBox __instance, string message)
        {
            if (message == "/dontdestroyneighbors" || message == "/dontdestroyneighbours" || message == "/ddn")
            {
                if (!LocalClient.serverOwner)
                {
                    __instance.AppendMessage(-1, "<color=#018786>Only the server host can enable/disable destroying neighbors<color=white>", "");
                    return;
                }
                Packets.DontDestroyNeighbors(!Main.dontDestroy);
            }
        }

        [HarmonyPatch(typeof(BuildDestruction), nameof(BuildDestruction.OnDestroy)), HarmonyPrefix]
        static bool OnDestroy() => !Main.dontDestroy;

        [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.InitAllItems)), HarmonyPostfix]
        static void InitAllItems(ItemManager __instance)
        {
            var id = __instance.allItems.Count;
            Main.Precision.id = id;
            __instance.allItems[id] = Main.Precision;
        }

        [HarmonyPatch(typeof(UseInventory), nameof(UseInventory.Use)), HarmonyPrefix]
        static bool Use(UseInventory __instance) => __instance.currentItem?.id != Main.Precision.id;

        [HarmonyPatch(typeof(UseInventory), nameof(UseInventory.UseButtonUp)), HarmonyPrefix]
        static bool UseButtonUp(UseInventory __instance)
        {
            if (__instance.currentItem?.id != Main.Precision.id) return true;
            if (!Main.dontDestroy)
            {
                ChatBox.Instance.AppendMessage(-1, LocalClient.serverOwner ? "<color=#B00020>Cannot use precision delete right now. Please run /dontdestroyneighbors first.<color=white>" : $"<color=#B00020>Cannot use precision delete right now. Please ask the host to run /dontdestroyneighbors first.<color=white>", "");
                return false;
            }
            if (Physics.Raycast(PlayerMovement.Instance.playerCam.position, PlayerMovement.Instance.playerCam.forward, out var hit, float.PositiveInfinity, ~(1 << LayerMask.NameToLayer("Player")), Input.GetKey(Main.precisionTriggers.Value) ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore))
            {
                Hitable target = hit.collider.GetComponentInParent<Hitable>();
                if (target != null) target.Hit(int.MaxValue, 1, 0, hit.point);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerStatus))]
    class PlayerStatusPatches
    {
        [HarmonyPatch(nameof(PlayerStatus.stamina), MethodType.Getter), HarmonyPrefix]
        static bool stamina(PlayerStatus __instance, ref float __result)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative)
            {
                __result = __instance.maxStamina;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(PlayerStatus.hunger), MethodType.Getter), HarmonyPrefix]
        static bool hunger(PlayerStatus __instance, ref float __result)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative)
            {
                __result = __instance.maxHunger;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(PlayerStatus.StopInvincible)), HarmonyPrefix]
        static bool StopInvincible() => GameManager.gameSettings.gameMode != GameSettings.GameMode.Creative;

        [HarmonyPatch(nameof(PlayerStatus.Awake)), HarmonyPostfix]
        static void Awake(PlayerStatus __instance)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative)
            {
                __instance.invincible = true;
                __instance.maxStamina = 100f;
                __instance.maxHunger = 100f;
            }
        }

        [HarmonyPatch(nameof(PlayerStatus.DealDamage)), HarmonyPrefix]
        static bool DealDamage(PlayerStatus __instance) => !__instance.invincible;
    }

    [HarmonyPatch(typeof(InventoryUI))]
    class InventoryUIPatches
    {
        [HarmonyPatch(nameof(InventoryUI.CanRepair)), HarmonyPrefix]
        static bool CanRepair(ref bool __result)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative)
            {
                __result = true;
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(InventoryUI.Repair)), HarmonyPrefix]
        static bool Repair(ref bool __result)
        {
            if (GameManager.gameSettings.gameMode == GameSettings.GameMode.Creative)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
