using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics; // For Vector2
using System.Threading;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using UnIdy.Utils;
using System.Drawing; // For Color

namespace UnIdy
{
    public class UnIdy : BaseSettingsPlugin<Settings>
    {
        private IngameState _ingameState;
        private Vector2 _windowOffset;

        public UnIdy()
        {
        }

        public override bool Initialise()
        {
            base.Initialise();
            Name = "UnIdy";

            _ingameState = GameController.Game.IngameState;
            var windowRectangle = GameController.Window.GetWindowRectangle();
            _windowOffset = new Vector2(windowRectangle.TopLeft.X, windowRectangle.TopLeft.Y);
            return true;
        }

        public override void Render()
        {
            try
            {
                base.Render();

                var inventoryPanel = _ingameState?.IngameUi?.InventoryPanel;
                if (inventoryPanel != null && inventoryPanel.IsVisible &&
                    Keyboard.IsKeyPressed(Settings.HotKey))
                {
                    Identify();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in Render: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Identify()
        {
            try
            {
                var inventoryPanel = _ingameState?.IngameUi?.InventoryPanel;
                if (inventoryPanel == null)
                {
                    LogError("Identify called but InventoryPanel is null.");
                    return;
                }

                var playerInventory = inventoryPanel[InventoryIndex.PlayerInventory];

                var scrollOfWisdom = GetItemWithBaseName(
                    "Metadata/Items/Currency/CurrencyIdentification",
                    playerInventory.VisibleInventoryItems);
                
                if (scrollOfWisdom == null)
                {
                    LogError("Scroll of Wisdom not found.");
                    return;
                }

                LogMessage(scrollOfWisdom.Text, 1);

                var normalInventoryItems = playerInventory.VisibleInventoryItems?.ToList();
                if (normalInventoryItems == null)
                {
                    LogError("Normal Inventory Items are null.");
                    return;
                }

                if (Settings.IdentifyVisibleTabItems.Value && _ingameState.IngameUi.StashElement.IsVisible)
                {
                    var stashItems = _ingameState.IngameUi.StashElement.VisibleStash?.VisibleInventoryItems;
                    if (stashItems != null)
                    {
                        normalInventoryItems.AddRange(stashItems);
                    }
                }

                var listOfNormalInventoryItemsToIdentify = new List<NormalInventoryItem>();

                foreach (var normalInventoryItem in normalInventoryItems)
                {
                    if (normalInventoryItem?.Item == null || !normalInventoryItem.Item.HasComponent<Mods>())
                        continue;

                    var mods = normalInventoryItem.Item.GetComponent<Mods>();

                    if (mods.Identified)
                        continue;

                    switch (mods.ItemRarity)
                    {
                        case ItemRarity.Unique when !Settings.IdentifyUniques.Value:
                        case ItemRarity.Rare when !Settings.IdentifyRares.Value:
                        case ItemRarity.Magic when !Settings.IdentifyMagicItems.Value:
                        case ItemRarity.Normal:
                            continue;
                    }

                    var sockets = normalInventoryItem.Item.GetComponent<Sockets>();
                    if (!Settings.IdentifySixSockets.Value && sockets?.NumberOfSockets == 6)
                        continue;

                    var itemIsMap = normalInventoryItem.Item.HasComponent<Map>();
                    if (!Settings.IdentifyMaps.Value && itemIsMap)
                        continue;

                    listOfNormalInventoryItemsToIdentify.Add(normalInventoryItem);
                }

                if (listOfNormalInventoryItemsToIdentify.Count == 0)
                {
                    Keyboard.KeyPress(Settings.HotKey.Value);
                    return;
                }

                Mouse.SetCursorPosAndRightClick(scrollOfWisdom.GetClientRect().Center, Settings.ExtraDelay, _windowOffset);
                Thread.Sleep(200);

                Keyboard.KeyDown(Keys.LShiftKey);
                foreach (var normalInventoryItem in listOfNormalInventoryItemsToIdentify)
                {
                    if (Settings.Debug.Value)
                    {
                        Graphics.DrawFrame(normalInventoryItem.GetClientRect(), Color.AliceBlue, 2);
                    }

                    Mouse.SetCursorPosAndLeftClick(
                        new Vector2(normalInventoryItem.GetClientRect().Center.X, normalInventoryItem.GetClientRect().Center.Y),
                        Settings.ExtraDelay.Value,
                        _windowOffset
                    );

                    Thread.Sleep(Constants.WHILE_DELAY + Settings.ExtraDelay.Value);
                }
                Keyboard.KeyUp(Keys.LShiftKey);
            }
            catch (Exception ex)
            {
                LogError($"Error in Identify: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private NormalInventoryItem GetItemWithBaseName(string path,
            IEnumerable<NormalInventoryItem> normalInventoryItems)
        {
            try
            {
                return normalInventoryItems?
                    .FirstOrDefault(normalInventoryItem =>
                        normalInventoryItem?.Item?.Path == path);
            }
            catch (Exception ex)
            {
                LogError($"Error in GetItemWithBaseName: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}
