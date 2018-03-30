# ToolbarControl

An interface to control both the Blizzy Toolbar and the stock Toolbar without having to code for each one.

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