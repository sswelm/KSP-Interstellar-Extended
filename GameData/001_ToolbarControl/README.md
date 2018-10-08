# ToolbarControl

An interface to control both the Blizzy Toolbar and the stock Toolbar without having to code for each one.

Most important
All mods using this should add the following line to the AssemblyInfo.cs file:

	[assembly: KSPAssemblyDependency("ToolbarController", 1, 0)]

This will guarantee the load order.  One benefit is that KSP will output a warning and won't load an assembly if it's dependencies aren't met (which may be better than puking out a bunch of exceptions).  The only other real problem with the forced to the top of the sort list method is that technically there's a couple characters before zero ('~', '!', '@', etc.) and dlls directly in GameData come first too.  Of course someone pretty much has to be trying to break things if you have to worry about this particular case.


// If true, activates Blizzy toolbar, if available.  Otherwise, use the stock toolbar
public void UseBlizzy(bool useBlizzy)


// The TC_ClickHander is a delegate used to pass in a method reference in the AddToAllToolbars methods below
public delegate void TC_ClickHandler();

The method AddToAllToolbars has several definitions.  All parameters are the same, the only difference is that
the shorter ones don't pass in unneeded parameters

// The position of the mouse the last time the button was clicked
Vector2	buttonClickedMousePos	

//  Whether this button is currently enabled (clickable) or not. 
// If setting, sets enabled (clickable) or not. 
public bool Enabled

// The button's tool tip text. Set to null if no tool tip is desired. 
public string ToolTip

Definitions
===========
The onTrue parameter is unique in that it applies both to the stock toolbar and the Blizzy toolbar.
	onTrue				Corresponds to the onTrue parameter in the AddModApplication function.  This also corresponds to the
						blizzyButton.OnClick setting 

The following corresponds to the same parameter in the AddModApplication() method
	onFalse	
	onHover
	onHoverOut
	onenable
	onDisable

	visibleInScenes		The values are the same as stock ApplicationLauncher.AppScenes.  The mod will use this to build the appropriate
						values for the Blizzy toolbar

Icons
	largeToolbarIcon	used for the stock toolbar
	smallToolbarIcon	Used for the Blizzy toolbar

If used, the following will be used to change the icon depending on whether it is active or not
	largeToolbarIconActive		large is used for the stock toolbar
    largeToolbarIconInactive

    smallToolbarIconActive		small is used for Blizzy toolbar
    smallToolbarIconInactive

The following are used by the Blizzy toolbar only
	nameSpace					Namespace of the mod
	toolbarId					unique id for the toolbar
	tooltip						tooltip which is shown when hovering the mouse over the button


The following methods are available, more can be added if requested


public void AddToAllToolbars(TC_ClickHandler onTrue, TC_ClickHandler onFalse,
            ApplicationLauncher.AppScenes visibleInScenes, 
			string nameSpace, string toolbarId, 
			string largeToolbarIcon, string smallToolbarIcon, 
			string toolTip = "")

public void AddToAllToolbars(TC_ClickHandler onTrue, TC_ClickHandler onFalse,
            ApplicationLauncher.AppScenes visibleInScenes, 
			string nameSpace, string toolbarId, 
			string largeToolbarIconActive,
            string largeToolbarIconInactive,
            string smallToolbarIconActive,
            string smallToolbarIconInactive, 
			string toolTip = "")

public void AddToAllToolbars(TC_ClickHandler onTrue, TC_ClickHandler onFalse, TC_ClickHandler onHover, TC_ClickHandler onHoverOut, TC_ClickHandler onEnable, TC_ClickHandler onDisable,
            ApplicationLauncher.AppScenes visibleInScenes, 
			string nameSpace, string toolbarId, 
			string largeToolbarIcon, string smallToolbarIcon, 
			string toolTip = "")

public void AddToAllToolbars(TC_ClickHandler onTrue, TC_ClickHandler onFalse, TC_ClickHandler onHover, TC_ClickHandler onHoverOut, TC_ClickHandler onEnable, TC_ClickHandler onDisable,
            ApplicationLauncher.AppScenes visibleInScenes, 
			string nameSpace, string toolbarId, 
			string largeToolbarIconActive, string largeToolbarIconInactive, string smallToolbarIconActive, string smallToolbarIconInactive, 
			string toolTip = "")

If you need to use Left and Right clicks (seperate from the onTrue and onFalse), the following is available.  Not that if you have an onTrue/onFalse specified along with an onLeftClick, 
both will be called:

public void AddLeftRightClickCallbacks(TC_ClickHandler onLeftClick, TC_ClickHandler onRightClick)

If you have the toolbar selectable in a settings page, you an ensure that any time the user changes the setting
the toolbar will change immediately by adding the following (example from FlightPlanner):

	private void OnGUI() 
	{
		if (toolbarControl != null)
				toolbarControl.UseBlizzy(HighLogic.CurrentGame.Parameters.CustomParams<FP>().useBlizzy);
	}

You can also add, if you like, various callbacks to monitor the settings.  This is a very lightweight call, if 
there isn't any change, it returns immediately

If you wish to manually change the button setting, you can use the following methods. If makeCall is true, then the appropriate 
call will be done, if there is a defined function for it

public void SetTrue(bool makeCall = false)
public void SetFalse(bool makeCall = false)


==========================================================================================
New Features

Buttons can now be displayed on both toolbars simultaneously.
Button settings can now be stored by the ToolbarController itself, eliminating the need to save it in the mod settings.  This is done by registering the button before or when the MainMenu is reached, see the example code below.

If using registration, then no need to have any call in the OnGUI method
If you do change the blizzy/stock options, then a single call to the following method will suffice:
	toolbarControl.UseButtons(string NameSpace);

Following used to register and set which button(s) are active

	NameSpace	Same namespace as for the AddToAllToolbars()
	DisplayName	Name of mod in display format, used on the ToolbarControl page

Method available to register the button:
	public static bool RegisterMod(string NameSpace, string DisplayName = "", bool useBlizzy = false, bool useStock = true, bool NoneAllowed = true)

Methods to either get or set the BlizzyActive and StockActive settings:
	public static bool BlizzyActive(string NameSpace, bool? useBlizzy = null)
	public static bool StockActive(string NameSpace, bool? useStock = null)

Method to set status of both buttons:
	public static void ButtonsActive(string NameSpace, bool? useStock, bool? useBlizzy)


New method to compliment the UseBlizzy method:
	public void UseStock(bool useStock)


The following example is from the Slingshotter mod:


Sample Code

Use code like the following to register the mod before or at the MainMenu:

	using UnityEngine;
	using ToolbarControl_NS;

	namespace KerbalSlingshotter
	{
		[KSPAddon(KSPAddon.Startup.MainMenu, true)]
		public class RegisterToolbar : MonoBehaviour
		{
			void Start()
			{
				ToolbarControl.RegisterMod(SlingshotCore.MODID, SlingshotCore.MODNAME);
			}
		}
	}

And in the file where the button is added to the ToolbarController:

	using System;
	using System.Linq;
	using UnityEngine;
	using KSP.UI.Screens;

	using ClickThroughFix;
	using ToolbarControl_NS;

	namespace KerbalSlingshotter
	{
		[KSPAddon(KSPAddon.Startup.Flight,false)]
		public class  SlingshotCore 
		{
			internal const string MODID = "Slingshotter_NS";
			internal const string MODNAME = "SlingShotter";
		}

		private void CreateButtonIcon()
			{
				toolbarControl = gameObject.AddComponent<ToolbarControl>();
				toolbarControl.AddToAllToolbars(ToggleOn, ToggleOff,
					ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.TRACKSTATION,
					MODID,
					"slingShotterButton",
					"SlingShotter/PluginData/Textures/icon_38",
					"SlingShotter/PluginData/Textures/icon_24",
					MODNAME
				);
			}
		}
	}