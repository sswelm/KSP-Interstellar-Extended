PatchManager is a simple mod which will allow mod and patch authors to 
package various optional patches with their mods, or as stand-alone patch
sets.

By creating a simple config file for each patch, PatchManager makes it possible
to install and remove patches from inside the game.  Gone are the days where you
would have to copy patches into the game.

An additional benefit is that all active patches are stored in a single location, 
making it easy to save, package up and redistribute to your friends.

This mod will do nothing by itself.  It requires a mod to include patches in a 
specific format.

As of this release, the only mod which supports it (and is an example for other mod
authors) is the new KW Rocketry Rebalanced mod.

When the window is open, lines with text in red are patches which are not enabled, lines
in green are patches which are enabled.

Note that the changes aren't done until until you click the "Apply All" button


Settings
--------

The following options are available on the Settings page:

- Always show toolbar button
- Store active patches in PatchManager folder

| key                                        | Default  | Description |
| ---                                        | ---      | --- |
| Always show toolbar button                 | Disabled | Show the toolbar button even if no patches are available due to dependencies/exclusions |
| Store active patches in PatchManager folder | Enabled | Selects where the active patches will be stored.  If disabled, will store in the patch's parent mod directory |

If you change where the patches are stored, the mod will move any active patches to the correct location

Instructions for Mod authors
============================

I'll be referring to the KWRocketry Rebalanced mod, so if you have any questions, I
suggest you first download that and look at it as a working example.


PatchManager looks for config nodes which have the following format:

	PatchManager
	{
		// Required settings.  
		// srcPath should use forward slashes, and include the full file name.  srcPath should be in a directory 
		// called ".../PatchManager/PluginData"
		modname = KW Rocketry
		patchName = GraduatedPowerResponse
		srcPath = KWRocketry/PatchManager/PluginData/GraduatedPowerResponse.cfg
		shortDescr = Graduated Power Response

		// Optional, but recommended
		longDescr = Makes the engines take time to spool up and down


		//// Optional entries here

		// dependencies, only show this patch if these specified mods are available
		// List either the directory of the mod (as show by ModuleManager), or the 
		// mod DLL (as show by ModuleManager)
		//dependencies = 

		// exclusions, this patch is exclusive with these, in other words, don't install this
		// if a patch listed in the exclusion is installed
		// exclusions = 

		// Path to icon, if desired.  Can be a flag, but will be shrunken down to a 38x38 image
		icon = KWRocketry/Flags/KWFlag04

		// Author's name, if desired
		author = Linuxgurugamer 

		// installedWithMod, if true, then this patch is active when the mod is installed
		// installedWithMod = true
	}

| key              | value |
| ---              | --- |
| patchName        | This is the name of the patch.  It should be short but descriptive. |
| srcPath          | Where the patch file is located, relative to the GameData directory. You MUST include the full file name as well. |
|                  | The filename of the patch MUST match the patchName above |
| shortDescr       | A short description of the patch. |
| longDescr        | A longer description of the patch. |
| dependencies     | What mods this patch is dependent on.  If these aren't installed, the patch won't be shown.  This is a comma separated list of mods. |
| icon             | An icon to show, if desired. |
| author           | Author of the patch. |
| installedWithMod | If true, then this patch is active when the mod is installed.  See the special instructions below about this option |

The directory structure is intentionally rigid.  This is done to make sure that patches 
are found properly, that patches aren't accidently made active, etc.

PatchManager configs should be in a directory in the mod folder called PatchManager.
The patches themselves should be in a directoryy called PluginData, inside the Patchmanager
directory.  See the following tree diagram for an example of how it's set up in KW Rocketry 
Rebalanced.  Note that while I did use different names for the PatchManager file and the 
actual patch file, and recommend that you do so, it isn't absolutely necessary:



	KWRocketry
	   |
	   |->Flags/
	   |->KWCommunityFixes/
	   |->Parts/
	   |
	   |->PatchManager/
	   |       |
	   |       |->PluginData/
	   |       |      |
	   |       |      |->GraduatedPowerResponse.cfg				// This is the actual patch file
	   |       |
	   |       |->PM_GraduatedPowerResponse.cfg					// This is the PatchManager file
	   |
	   |->SoundBank/
	   |
	   |->KWRocketryRedux.version
	   |->MiniAVC.dll


Now, as I was developing this, I was working with another mod author, who had a lot of
difficulty understanding what I was trying to explain.  The problem was we each were
thinking of a "mod" in different manners.  So, to be clear, I'll define here what
I'm referring to when I talk about a mod:

Mod Definition:

- KW is a mod, it has it's own set of patches
- CommunityPatches is a mod, it has it's own set of patches.
- JoesKWPatches is a  mod. it has it's own set of patches

Special Instructions regarding the "installedWithMod" option
============================================================
Some mods may wish to have patches which are installed and active when the mod is installed.
This requires special handling:

The patch goes into a different directory called ActiveMMPatches in the initial Patchmanager folder.  
Internally, the operations are somewhat reversed.  Instead of the patch being copied to the main PatchManager directory, when 
deactivated the patch will be moved to the local PluginData directory, and back again if reactivated.
The "srcPath" should still point to the PluginData directory, even though the patch will be in the ActiveMMPatches directory.  In 
this case, "srcPath" is telling the mod where to move the patch to in order to disable it.  The name of the file will be obtained from this entry.

srcPath = KWRocketry/PatchManager/PluginData/GraduatedPowerResponse.cfg

Using the same diagram as earlier, the new directory layout is:

	KWRocketry
	   |
	   |->Flags/
	   |->KWCommunityFixes/
	   |->Parts/
	   |
	   \->PatchManager/
	           |
	           |->ActiveMMPatches/
	           |      |
	           |      |->InitialActivePatch.cfg					// This is the actual patch file
	           |
	           |->PluginData/
	           |      |
	           |      |->GraduatedPowerResponse.cfg				// This is the actual patch file
	           |
	           |->PM_InitialActivePatch.cfg						// This is the PatchManager file
	           |->PM_GraduatedPowerResponse.cfg					// This is the PatchManager file
	    




Some final notes:

- If there aren't any mod patches available to be installed, the toolbar button will not be displayed.
- You can disable the toolbar button in the standard game settings page.
- There is an override which will force the toolbar button to be always shown, regardless of dependencies.
- If you install some patches, and then remove a dependency that one or more of those patches depend on, the patch WILL NOT be removed.
