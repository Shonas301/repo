using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using static StardewValley.Menus.ItemGrabMenu;

namespace WorkbenchHelper.AddToWorkbenchChests
{
    class AddToWorkbenchChestsHandler
    {
        private ModEntry modEntry;
        private ClickableTextureComponent button;
        private string hoverText;
        internal GameLocation currentLocation;
        private Texture2D image;
        private Texture2D imageDisabled;
        private List<TransferredItemSprite> transferredItemSprites;

        /// <summary>
        /// Initializes stuff for the mod.
        /// </summary>
        /// <param name="modEntry"></param>
        /// <param name="image"></param>
        /// <param name="imageDisabled"></param>
        public AddToWorkbenchChestsHandler(ModEntry modEntry, Texture2D image, Texture2D imageDisabled)
        {
            this.modEntry = modEntry;
            this.image = image;
            this.imageDisabled = imageDisabled;
            modEntry.Monitor.Log($"Handler created.");
            button = new ClickableTextureComponent(Rectangle.Empty, null, new Rectangle(0, 0, 16, 16), 4f)
            {
                hoverText = modEntry.helper.Translation.Get("hoverText.enabled")
            };

            transferredItemSprites = new List<TransferredItemSprite>();
        }

        /// <summary>
        /// Updates the location of the button, in case window is moved
        /// or resized.
        /// </summary>
        private void UpdatePos()
        {
            // Fill Stacks Button Bounds
            // new Rectangle(xPositionOnScreen + width, yPositionOnScreen + height / 3 - 64 - 64 - 16, 64, 64)

            var menu = Game1.activeClickableMenu;
            if (menu == null) return;

            var length = 16 * Game1.pixelZoom;
            const int positionFromBottom = 3;
            const int gapSize = 16;

            var screenX = menu.xPositionOnScreen + menu.width;
            var screenY = menu.yPositionOnScreen + menu.height / 2 - (length * positionFromBottom) - (gapSize * (positionFromBottom - 1));

            button.bounds = new Rectangle(screenX, screenY, length, length);
        }

        /// <summary>
        /// Checks to see if any other chests around the workbench are in use.
        /// Only returns true if not.
        /// </summary>
        /// <returns></returns>
        internal bool IsWorkbenchWithChests()
        {
            var page = modEntry.ReturnCraftingPage();
            if (page != null && page._materialContainers.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Draws button on top of the workbench menu GUI
        /// </summary>
        public void DrawButton()
        {
            UpdatePos();

            // If chests aren't free, we use a desaturated image to
            // indicate the button is disabled.
            button.texture = IsWorkbenchWithChests() ? image : imageDisabled;
            button.draw(Game1.spriteBatch);

            if (hoverText != "")
                IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);

            // Draws cursor over the GUI element
            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
            Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero,
            4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 0);
        }

        internal bool TryHover(float x, float y)
        {
            this.hoverText = "";
            var page = modEntry.ReturnCraftingPage();

            if (page != null)
            {
                if (IsWorkbenchWithChests())
                    button.tryHover((int)x, (int)y, 0.25f);

                if (button.containsPoint((int)x, (int)y))
                {
                    this.hoverText = IsWorkbenchWithChests() ? button.hoverText : modEntry.helper.Translation.Get("hoverText.disabled");
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Modified version of game's ItemGrabMenu.FillOutStacks().
        /// Works with any given chest instead of just the one
        /// the Farmer is currently interacting with.
        /// 
        /// Because the Expanded Fridge doesn't come with any support for
        /// FillOutStacks() (and thus has no support for transferred
        /// item sprites/shakeItem), for now if the main chest is
        /// accessed while Expanded Fridge is installed, the visual
        /// nuances will not display.
        /// </summary>
        /// <param name="chest"></param>
        public void FillOutStacks(InventoryMenu inventory, IInventory chest)
        {
            if (chest == null || chest.Count == 0)
            {
                modEntry.Monitor.Log("Chest is null or has no items for player.", LogLevel.Error);
                return;
            }
            IList<Item> actualInventory = inventory.actualInventory;
            HashSet<int> hashSet = new HashSet<int>();
            ILookup<string, Item> lookup = chest.Where((Item item5) => item5 != null).ToLookup((Item item5) => item5.QualifiedItemId);
            if (lookup.Count == 0)
            {
                return;
            }

            for (int num = 0; num < actualInventory.Count; num++)
            {
                Item item = actualInventory[num];
                if (item == null)
                {
                    continue;
                }

                bool flag = false;
                foreach (Item item5 in lookup[item.QualifiedItemId])
                {
                    flag = item5.canStackWith(item);
                    if (flag)
                    {
                        break;
                    }
                }

                if (!flag)
                {
                    continue;
                }

                Item item2 = item;
                bool flag2 = false;
                int num2 = -1;
                for (int num3 = 0; num3 < chest.Count; num3++)
                {
                    Item item3 = chest[num3];
                    if (item3 == null)
                    {
                        if (num2 == -1)
                        {
                            num2 = num3;
                        }
                    }
                    else
                    {
                        if (!item3.canStackWith(item))
                        {
                            continue;
                        }

                        int num4 = item.Stack - item3.addToStack(item);
                        if (num4 > 0)
                        {
                            flag2 = true;
                            hashSet.Add(num3);
                            item = item.ConsumeStack(num4);
                            if (item == null)
                            {
                                actualInventory[num] = null;
                                break;
                            }
                        }
                    }
                }

                if (item != null)
                {
                    if (num2 == -1 && chest.HasEmptySlots())
                    {
                        num2 = chest.Count;
                        chest.Add(null);
                    }

                    if (num2 > -1)
                    {
                        flag2 = true;
                        hashSet.Add(num2);
                        item.onDetachedFromParent();
                        chest[num2] = item;
                        actualInventory[num] = null;
                    }
                }

                // if (flag2)
                // {
                //     TransferredItemSprite item_sprite = new TransferredItemSprite(item2.getOne(), inventory.inventory[num].bounds.X, inventory.inventory[num].bounds.Y);
                //     var transferredItemSprites = modEntry.helper.Reflection.GetField<List<TransferredItemSprite>>(inventory, "_transferredItemSprites").GetValue();
                //     transferredItemSprites.Add(item_sprite);
                // }
            }

        }
        internal void UpdateTransferredItemSprites()
        {
            var chest = new Chest();
            if (transferredItemSprites.Count > 0)
                modEntry.Monitor.Log($"transferredItemSprites.Count: {transferredItemSprites.Count}");
            for (int i = 0; i < transferredItemSprites.Count; i++)
            {
                if (transferredItemSprites[i].Update(Game1.currentGameTime))
                {
                    transferredItemSprites.RemoveAt(i);
                    i--;
                }
            }
        }

        internal void DrawTransferredItems(SpriteBatch spriteBatch)
        {
            foreach (TransferredItemSprite transferredItemSprite in transferredItemSprites)
            {
                transferredItemSprite.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Using the FillOutStacks method, fills each chest around the workbench.
        /// </summary>
        private void FillChests()
        {
            // Fill main chest first
            var page = modEntry.ReturnCraftingPage();
            if (page != null && page._materialContainers.Count > 0)
            {
                foreach (var container in page._materialContainers)
                {
                    if (container is Inventory)
                    {
                        string joined = string.Join(",", (container as Inventory).Select(i => i?.DisplayName ?? "null"));
                        FillOutStacks(page.inventory, container);
                    }
                }
            }
        }

        /// <summary>
        /// Plays the sound and fills fridges if the user clicks on the button
        /// and the fridges are not in use by other players.
        /// </summary>
        /// <param name="cursor"></param>
        internal void HandleClick(ICursorPosition cursor)
        {
            var screenPixels = cursor.ScreenPixels;

            if (!button.containsPoint((int)screenPixels.X, (int)screenPixels.Y)) return;

            if (IsWorkbenchWithChests())
            {
                Game1.playSound("Ship");

                FillChests();
            }
        }
    }
}
