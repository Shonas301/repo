using WorkbenchHelper.AddToWorkbenchChests;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Reflection;

namespace WorkbenchHelper
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private AddToWorkbenchChestsHandler handler;
        internal IModHelper helper;

        private const string EXPANDED_WORKBENCH_ID = "shonas.WorkbenchHelper";

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Assets
            var button = helper.ModContent.Load<Texture2D>("assets/button.png");
            var buttonDisabled = helper.ModContent.Load<Texture2D>("assets/button_disabled.png");

            // Instantiation of Handler and Helper Objects
            this.helper = helper;
            this.handler = new AddToWorkbenchChestsHandler(this, button, buttonDisabled);

            AddEvents(helper);
        }


        /*********
        ** Private methods
        *********/

        /// <summary>Adds Events to the SMAPI helper.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        private void AddEvents(IModHelper helper)
        {
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.CursorMoved += OnCursorMoved;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }


        /*   Author: GordonBombay#6433 @ Discord, tweaked by Rui3 @ NexusMods
        *    Finding the mod's assembly and doing stuff with it consists of multiple steps
        *    1. Find out if the mod is even loaded by SMAPI
        *    2. If it has, then try to get the assembly, given what information SMAPI provides
        *    3. Start using classes from the mod's assembly. Be cautious that the mod may not exist
        */

        private void getModAssembly(string uniqueID)
        {
            /**************************************************
            *              STEP 1
            **************************************************/
            // If we find the mod, this will eventually contain a value
            IModInfo otherModInfo = null;

            // Loop through all of the mods SMAPI has loaded, and try to find the one we want
            foreach (var mod in helper.ModRegistry.GetAll())
            {
                // If we find the mod we want, then grab it and get out of the loop
                //    NOTE: The assembly may change in the future, means that classes/methods
                //          May no longer exist or have been renamed
                if (mod.Manifest.UniqueID == uniqueID)
                {
                    otherModInfo = mod;
                    break;
                }
            }



            /**************************************************
            *              STEP 2
            **************************************************/

            // Now that we know the mod exists, let's get its name without the ".dll" at the end
            string modName = otherModInfo.Manifest.EntryDll;
            string assemblyName = modName.Substring(0, modName.LastIndexOf("."));

            // This will hold the assembly of the mod that we care about
            Assembly otherModAssembly = null;

            // For each of the assemblies SMAPI has loaded, try to get the one we care about
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name.Equals(assemblyName))
                {
                    otherModAssembly = assembly;
                    Monitor.Log($"" + assemblyName + " assembly found.");
                    break;
                }
            }
        }


        /// <summary>After all mods are loaded, we check to see if those needing compatibility patches
        /// are installed.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Compatibility Check

            getModAssembly(EXPANDED_WORKBENCH_ID);
        }



        internal CraftingPage ReturnCraftingPage()
        {
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is CraftingPage)
            {
                var page = Game1.activeClickableMenu as CraftingPage;
                if (page is CraftingPage)
                {
                    return page;
                }
            }

            return null;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (ReturnCraftingPage() == null) return;
        }

        /// <summary>
        /// Checks to see if the cursor is hovering over the button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            int x = (int)e.NewPosition.ScreenPixels.X;
            int y = (int)e.NewPosition.ScreenPixels.Y;

            handler.TryHover(x, y);
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            var page = ReturnCraftingPage();
            if (page != null)
                if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA || e.Button == SButton.C)
                    handler.HandleClick(e.Cursor);
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (ReturnCraftingPage() != null)
            {
                handler.currentLocation = Game1.player.currentLocation;
                handler.DrawButton();

                if (!(ReturnCraftingPage() is CraftingPage))
                {
                    handler.DrawTransferredItems(e.SpriteBatch);
                }
            }
            else
            {
                handler.currentLocation = null;
            }
        }
    }
}