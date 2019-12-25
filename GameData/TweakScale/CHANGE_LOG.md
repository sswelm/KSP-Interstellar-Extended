# TweakScale :: Change Log

* 2019-0903: 2.4.3.4 (Lisias) for KSP >= 1.4.1
	+ Closing or reworking the following issues:\
		- [#30](https://github.com/net-lisias-ksp/TweakScale/issues/30) Prevent incorrectly initialized Modules to be used
		- [#71](https://github.com/net-lisias-ksp/TweakScale/issues/71) Check for typos on the _V2 parts from patches for Squad's revamped parts
			- Thanks to [Dizor](https://forum.kerbalspaceprogram.com/index.php?/profile/161502-dizor/). I'm still [laughing](https://forum.kerbalspaceprogram.com/index.php?/topic/179030-14-tweakscale-under-lisias-management-2433-2019-0814/page/33/&tab=comments#comment-3666432)! :D
	+ New hotfixes:
		- Contares ([old](https://forum.kerbalspaceprogram.com/index.php?/topic/122102-13x-contares-189-closed/) and [new](https://forum.kerbalspaceprogram.com/index.php?/topic/171305-17x-csa-contares-core-2012/)) breaking TweakScale.
* 2019-0814: 2.4.3.3 (Lisias) for KSP >= 1.4.1
	+ Added support for hot-fixes - handcrafted patches to brute force a correct path when the normal way is not possible - as when an unmaintained ARR Add'On is involved on the mess.
		- New hot fix for [CxAerospace:Station Parts](https://forum.kerbalspaceprogram.com/index.php?/topic/138910-dev-halted13-cxaerospace-stations-parts-pack-v162-2017-5-24/page/31/) breaking [Bluedog_DB](https://forum.kerbalspaceprogram.com/index.php?/topic/122020-16x-bluedog-design-bureau-stockalike-saturn-apollo-and-more-v152-бруно-8feb2019/). 
* 2019-0804: 2.4.3.2 (Lisias) for KSP >= 1.4.1
	+ This is an Emergencial Release due an Emergencial Release due an Emergencial Release. I love recursion, don't you? :P
	+ Closing or reworking the following issues:
		- [#65](https://github.com/net-lisias-ksp/TweakScale/issues/65) Support for new Nertea's Cryo Engines
			- Thanks to [friznet](https://github.com/friznit) and [marr75](https://github.com/marr75).
		- Fixing some tyops :P on Logging and Dialog Boxes.
* 2019-0725: 2.4.3.1 (Lisias) for KSP >= 1.4.1
	+ This is an emergencial Release due a Emergencial Release. :P
	+ Adding KSPe Light facilites:
		- Logging
	+ Closing or reworking the following issues:
		- [#31](https://github.com/net-lisias-ksp/TweakScale/issues/31) Preventing being ran over by other mods
			- A misbehaviour on detecting the misbehaviour :) was fixed. 
		- [#47](https://github.com/net-lisias-ksp/TweakScale/issues/47) Count failed Sanity Checks as a potential problem. Warn user.
		- [#48](https://github.com/net-lisias-ksp/TweakScale/issues/48) Backport the Heterodox Logging system into Orthodox (using KSPe.Light)
		- [#49](https://github.com/net-lisias-ksp/TweakScale/issues/49) Check the Default patches for problems due wildcard!
		- [#50](https://github.com/net-lisias-ksp/TweakScale/issues/50) Check the patches for currently supported Add'Ons
			- ModuleGeneratorExtended Behaviour 
		- [#51](https://github.com/net-lisias-ksp/TweakScale/issues/51) Implement a "Cancel" button when Actions are given to MessageBox
			- Yeah. Doing it right this time. 
		- [#54](https://github.com/net-lisias-ksp/TweakScale/issues/54) [ERR ***FATAL*** link provided in KSP.log links to 404
			- "Typo maldito, typo maldito - tralálálálálálá"
		- [#56](https://github.com/net-lisias-ksp/TweakScale/issues/56) "Breaking Parts" patches
		- [#57](https://github.com/net-lisias-ksp/TweakScale/issues/57) Implement Warning Dialogs
			- Warnings about Overrules, parts that couldn't be 			- Doing it right this time! 
checked and parts with TweakScale support withdrawn.
		- [#58](https://github.com/net-lisias-ksp/TweakScale/issues/58) Mk4 System Patch (addendum)
* 2019-0608: 2.4.3.0 (Lisias) for KSP >= 1.4.1
	+ This is an emergencial Release due a Show Stopper issue (see Issue #34 below) with some new features.
	+ Adding features:
		- [#7](https://github.com/net-lisias-ksp/TweakScale/issues/7) Adding support for new Parts from KSP 1.5 and 1.6 (and Making History)! (**finally!**)
		- [#35](https://github.com/net-lisias-ksp/TweakScale/issues/35) Checking for new Parts on KSP 1.7 (none found)
			- (Serenity is Work In Progress)
		- Adding KSPe.Light support for some UI features. 
	+ Fixing bugs:
		- [#31](https://github.com/net-lisias-ksp/TweakScale/issues/31) Preventing being ran over by other mods
		- [#34](https://github.com/net-lisias-ksp/TweakScale/issues/34) New Sanity Check: duplicated properties
	+ [Known Issues](https://github.com/net-lisias-ksp/TweakScale/blob/master/KNOWN_ISSUES.md) update:
		- A new and definitively destructive interaction was found due some old or badly written patches ends up injecting TweakScale properties **twice** on the Node.
* 2019-0505: 2.4.2.0 (Lisias) for KSP >= 1.4.1
	+ Adding features:
		- [#32](https://github.com/net-lisias-ksp/TweakScale/issues/32) Near Future Aeronautics Patches
	+ Fixing bugs:
		- [#20](https://github.com/net-lisias-ksp/TweakScale/issues/20)	Duplicated TweakScale support on some parts
		- [#23](https://github.com/net-lisias-ksp/TweakScale/issues/23) Unhappy merge on TweakScale exponents
		- [#24](https://github.com/net-lisias-ksp/TweakScale/issues/24) Fix that duplicated support on some parts
		- [#30](https://github.com/net-lisias-ksp/TweakScale/issues/30) Prevent incorrectly initialized Modules to be used
	+ [Known Issues](https://github.com/net-lisias-ksp/TweakScale/blob/master/KNOWN_ISSUES.md) update:
		- Users of "Classic" [Infernal Robotics](https://github.com/MagicSmokeIndustries/InfernalRobotics) should avoid scaling parts to "Small -" or Krakens will be released.
			- [Infernal Robotics/Next](https://github.com/meirumeiru/InfernalRobotics) fixes this issue.   
* 2019-0216: 2.4.1.0 (Lisias) for KSP >= 1.4.1
	+ Adding 1.875 scale as default (being now a Stock size on MH, it makes sense to properly acknowledge it). Suggested by Tyko.
		- Closing issue [#3](https://github.com/net-lisias-ksp/TweakScale/issues/3)
	+ Adding support for Stock Alike Station Parts. Courtesy of Speadge.
		- Closing issue [#8](https://github.com/net-lisias-ksp/TweakScale/issues/8)
	+ Fixed a critical craft corruption (even flying ones) as TweakScale is sometimes being injected twice (or even more) into a part. This patch does not fix the duplicity, but prevent your crafts from being corrupted once a fix is applied (yeah - fixing the bug would cause craft corruption without this patch!)
		- Closing issue [#20](https://github.com/net-lisias-ksp/TweakScale/issues/20)
* 2018-1229: 2.4.0.7 (Lisias) for KSP >= 1.4.1
	+ KSP 1.6 (partial) support certified.
	+ Actively reverting support in runtime for parts with problematic or unsupported modules.
		- Closing issue [#9](https://github.com/net-lisias-ksp/TweakScale/issues/9)
		- Closing issue [#11](https://github.com/net-lisias-ksp/TweakScale/issues/11)
		- Closing issue [#12](https://github.com/net-lisias-ksp/TweakScale/issues/12)
	+ Lifting the Max KSP restriction on the `.version` file.
	+ Updating Module Manager to 3.1.1
* 2018-1027: 2.4.0.6 (Lisias) for KSP 1.4.1+; 1.5
	+ KSP 1.5 support certified.
	+ Reverting some misunderstood versioning.
	+ Moving the repository to the Official Headquarters
	+ Some performance (and type safety) enhancements
	+ Fixes on the MX-3L Hermes (NFT) as proposed by NachtRaveVL
	+ Fixed an issue when Making History is present.
		- With additional failsafe measure 
	+ Bumping Release to assume official maintenance and mark the URL change.
	+ **Properly** Reverting KSPe dependency (for while, at least).
		- Unholy user settings files are back to the Sacred Land of GameData. Repent, Sinner!!
* 2018-1027: 2.4.0.5 (Lisias) for KSP 1.4.1+; 1.5
	* Move on, nothing to see here! =P 
* 2018-1025: 2.4.0.4 (Lisias) for KSP 1.4
	+ DITCHED
* 2018-1019: 2.4.0.3 (Lisias) for KSP 1.4
	+ DITCHED
* 2018-1016: 2.4.0.2 (Lisias) for KSP 1.4
	+ DITCHED
* 2018-1016: 2.4.0.1 (Lisias) for KSP 1.4
	+ DITCHED
* 2018-0816: 2.3.12.1 (Lisias) for KSP 1.4.x
	+ Saving xml config files under <KSP_ROOT>/PluginData Hierarchy
		- Added hard dependency for [KSP API Extensions/L](https://github.com/net-lisias-ksp/KSPAPIExtensions). 
	+ Removed deprecated DLLs
		- Needs TweakableEverything installed now
		- A small hack:
			- one DLL was moved to a new Plugin directory inside the dependency to overcome the loading order problem.
			- a better solution is WiP for the next release
	+ Removed Support code for deleted KSP functionalities
		- Not needed anymore? (RiP) 
* 2018-0416: 2.3.12 (Pellinor) for KSP 1.4.2
	+ configs for new parts
	+ fix for exceptions
	+ fix for solar panels
	+ recompile for KSP 1.4.2
* 2018-0314: 2.3.10 (Pellinor) for KSP 1.4.1
	+ don't overwrite stuff if the exponent is zero
	+ recompile against KSP 1.4.1
* 2018-0309: 2.3.9 (Pellinor) for KSP 1.3.1
	+ fix interaction with stock ModulePartVariants
* 2018-0307: 2.3.8 (Pellinor) for KSP 1.3.1
	+ [TweakScale-v2.3.8.zip](https://github.com/pellinor0/TweakScale/files/1790799/TweakScale-v2.3.8.zip)
		- recompile for KSP 1.4
		- a few patches for new parts
	+ Known issues:
		- the new stock texture switch messes up attachment nodes on scaled parts
		- (first switching and then scaling seems to work)
* 2017-1013: 2.3.7 (Pellinor) for KSP 1.3.1
	+ recompile for KSP 1.3.1
	+ only complain about negative dry mass if the number is significant
* 2017-0527: 2.3.6 (Pellinor) for KSP 1.3.0
	+ recompile for KSP 1.3
	+ lots of player-submitted patches (thanks eberkain, mikeloeven, OliverPA77)
	+ set SRB thrust exponent to 3 (so both TWR and burntime are preserved now)
	+ added a few null checks
	+ scaling support for resource lists (for drills and converters)
	+ Fix: don't scale mass if the part has a MFT module
	+ TWEAKSCALEEXPONENTS should now affect not only the mentioned module but also derived modules
	+ (e.g. the exponent for ModuleRCS also applies for ModuleRCSFX)
* 2017-0123: 2.3.4 (Pellinor) for KSP 1.2.2
	+ fix exponent for stock ModuleGenerator
	+ found a way to write dryCost of the prefab early enough (fixes a cost issue with KIS)
	+ functions to ask the prefab about stats of scaled parts (meant for KIS)
* 2016-1217: 2.3.3 (Pellinor) for KSP 1.2.2
	+ recompile for KSP 1.2.2
	+ fix cost bug with fsfuelswitch
	+ (ignore resource cost because FSfuelSwitch takes it into account)
	+ added a bit of documentation
* 2016-1102: 2.3.2 (Pellinor) for KSP 1.2
	+ recompile for KSP1.2.1
	+ update patches for RLA-Stockalike, OPT
	+ exponents for ModuleRCSFX
	+ remove an obsolete exponent
	+ remove patches for relay antennas since scaling of their main function does not work
	+ (which is relaying signals when the antenna is part of an unloaded vessel)
* 2016-1021: 2.3.1 (Pellinor) for KSP 1.2
	+ exponent for groundHeightOffset (fixes landing gears clipping the runway at launch)
	+ fix for node positions reverting when cloning a part
	+ antennas: refresh range display
	+ antennas: tweak exponent (so that downscaled antennas are a bit less overpowered)
* 2016-1015: 2.3 (Pellinor) for KSP 1.2
	+ fix for wheel colliders
	+ fix wheelMotor torque and ec consumption
	+ CrewCapacity: has a configurable exponent now
	+ CrewCapacity: additional seats are not shown in the editor (stock limitation)
	+ scaling support for FloatCurve (for wheel torque)
	+ scaling support for and int values (for CrewCapacity)
	+ fix scaling of input/outputResources (new stock "resHandler", e.g. consumption of reaction wheels)
	+ move workaround for stock UI_ScaleEdit bug into the plugin
* 2016-0624: 2.2.13 (Pellinor) for KSP 1.1.3
	+ recompile for KSP 1.1.3
	+ fix for solar panels
	+ rewrite of chainScaling: propagate relative scaling factor to child parts
* 2016-0519: 2.2.12 (Pellinor) for KSP 1.1.2
	+ scaling of crew capacity (hardcoded to use the mass exponent for now)
	+ Fix patches scale crewed parts with an exponent of 2 for crew and mass
	+ (not realistic but fits better to stock balance than 3). In any case mass/kerbal is hardcoded to be preserved.
	+ scaling of the IVA overlay
	+ support for new firespitter biplane
	+ support for HLAirships module (thanks SpannerMonkey)
	+ some missing patches for B9 (thanks BlowFish)
	+ (did some reordering but still need to sort through the content)
	+ fixed a few patches
	+ reorganize some patches into their own folder
	+ (B9, Squad including nasa and spacePlanePlus)
	+ small optimisation: disable partModule in flight if not scaled
* 2016-0512: 2.2.11 (Pellinor) for KSP 1.1.2
	+ remove obsolete IFS exponents
	+ fix bug in partMessage for MFT
	+ expose API via the part message system
	+ fix for mirrored parts
	+ workaround for tweakable bug: extra scaleFactor 500% for the free scaletypes
	+ (so the range from 200-400% is usable again)
	+ don't interfere if other mods illegaly write part.mass. Print a warning in this case
	+ fix thrust for moduleRCS (new maxFuelFlow exponent like for moduleEngines)
* 2016-0505: 2.2.10 (Pellinor) for KSP 1.1.2
	+ basic wheel scaling: scaled wheels work but still behave strange. They roll, are pretty close to touching the ground, and are able to bounce.
	+ make sure the exponents are applied before notifying other mods through the API (needed for interaction with FAR)
	+ MFT support changed to TweakScale using their API (instead of the other way round)
	+ tweaked downscaled science parts to be a little more expensive
* 2016-0430: 2.2.9 (Pellinor) for KSP 1.1.2
	+ fix for drag cube scaling
	+ update right click menu after rescale
	+ recompile for 1.1.2
	+ cleanup for mass scaling
* 2016-0424: 2.2.7.2 (Pellinor) for KSP 1.1
	+ fix scaling of the root part
* 2016-0423: 2.2.7.1 (Pellinor) for KSP 1.1
	+ update for 1.1
	+ Workaround for the camera breaking (root part scaling is still broken)
	+ support for new stock parts
	+ shrinking science and probe cores makes them more expensive
	+ (only changed for stock so far)
	+ update for the OPT v1.8 test release
* 2016-0102: 2.2.6 (Pellinor) for KSP 1.0.5
	+ Support for NF-Construction
	+ update for NFT Octo-Girders
	+ fix for infinite loop between TweakScale and MFT
	+ fix for engineer's report mass display
* 2015-1109: 2.2.5 (Pellinor) for KSP 0.90
	+ recompile for KSP 1.0.5 (still using the old KspApiExtensions)
	+ update MM
	+ patches for the new parts
* 2015-1030: 2.2.4 (Pellinor) for KSP 0.90
	+ Fix for scaling of lists. This should fix the trouble with cost of FSFuelSwitch parts.
	+ Partial fix for editor mass display not updating
	+ new file Examples.cfg with frequently used custom patches
	+ Removed MM switch for scaleable crew pods
	+ update of NFT patches
	+ Fix scaling of resource lists
	+ support for a few missing stock parts
	+ partial support for a few other mods
	+ stock radiator support
	+ Scale ImpactRange for stock drill modules (this is what determines if the drill has ground contact or not)
	+ scale captureRange for claw (this should fix 3.75m claws not grappling)
	+ removed brakingTorque exponent (not needed and breaks stock tweakable)
* 2015-0626: 2.2.1 (Pellinor) for KSP 0.90
	+ update for KSP 1.0.4
	+ KSP 1.0 support: scaling of dragCubes
	+ exponent -0.5 for heatProduction
	+ support for HX parts from B9-Aerospace
	+ support for firespitter modules: FSEngine, FSPropellerTweak, FSAlternator
	+ remove support for KAS connector port so it stays stackable in KIS
	+ a few missing part patches
	+ update NF-Solar patches (some parts were renamed)
	+ catch exceptions on rescale
	+ survive duplicate part config
* 2015-0502: 2.1 (Pellinor) for KSP 0.90
	+ recompile for KSP 1.0.2
	+ new stock part
* 2015-0501: 2.0.1 (Pellinor) for KSP 0.90
	+ restored maxThrust exponent to fix the editor CoT display
	+ added patch for new KIS container
	+ survive mistyped scaleTypes
* 2015-0430: 2.0 (Pellinor) for KSP 0.90
	+ recompile for KSP 1.0
	+ new TWEAKSCALEBEHAVIOR nodes (engines, decouplers, boosters)
	+ scale DryCost with the mass exponent if there is no DryCost exponent defined
	+ fuel fraction of tanks is now preserved [breaking]
	+ move part patches into their own directory
	+ KIS support
	+ proper MM switches for mod exponents
	+ removed KSPI support (will be distributed with KSPI)
	+ scaleExponents for NF-electrical capacitors
	+ cleanup of stock scaleExponents
	+ support for the new stock modules
	+ support for the changed engine modules
* 2015-0420: 1.53 (Pellinor) for KSP 0.90
	+ download address for version file
	+ added missing RLA configs
	+ only touch part cost of the part is rescaled
	+ fix for repairing incomplete scaletypes
	+ support for stock decoupling modules
	+ OPT support
	+ remove RF scale exponents (RF does its own support)
* 2015-0310: 1.52.1 (Pellinor) for KSP 0.90
	+ No changelog provided
* 2015-0308: 1.52 (Pellinor) for KSP 0.90
	+ New Tweakable with more flexible intervals.
	+ All scaletypes use scaleFactors now, max/minScale is obsolete.
	+ Better handling of incomplete or inconsistent scaletype configs.
	+ Vessels now survive a change of defaultScale.
	+ less persistent data
* 2015-0226: 1.51.1 (Pellinor) for KSP 0.90
	+ added KSP-AVC support
	+ freescale slider Increments are now part of the scaletype config
	+ added stock mk3 configs
	+ auto- and chain scaling off by default (the hotkeys are leftCtrl-L and leftCtrl-K)
	+ auto- and chain scaling restricted to parts of the same scaletype
	+ Changed the 'stack' scaletype to free scaling
	+ Moved stock adapters to stack scaletype
	+ Changed surface scaletype to free scaling
	+ added an example discrete scaletype for documentation, because there is none left
	+ fixed error spam with regolith & KAS
	+ removed duplicate MM patch for IntakeRadialLong
	+ hopefully restricted the camera bug to scaled root parts
* 2015-0225: 1.51 (Pellinor) for KSP 0.90
	+ added KSP-AVC support
	+ added stock mk3 configs
	+ autoscaling
		- auto- and chain scaling off by default
		- auto- and chain scaling restricted to parts of the same scaletype
		- rewrote GetRelativeScaling based on the nodes of the prefab part
	+ scaletypes
		- freescale slider Increments are now part of the scaletype config
		- Change the 'stack' scaletype to free scaling
		- Move stock adapters to stack scaletype
		- Change surface scaletype to free scaling
		- added an example discrete scaletype for documentation, because there is none left in the default configs
		- if min/maxScale are missing in a free scaletype take min/max of the scaleFactors list
	+ fixes
		- fixed error spam with scalable parts from KAS containers
		- removed duplicate MM patch for IntakeRadialLong
		- hopefully restricted the camera bug to scaled root parts
* 2014-1224: 1.50 (Biotronic) for KSP 0.24
	+ Fixed erroneous placement of attach nodes when duplicating parts.
* 2014-1218: 1.49 (Biotronic) for KSP 0.24
	+ Now saving hotkey states correctly
	+ 'Free' scaletype actually free
	+ Fixed bug in OnStart
	+ First attempt at scaling offsets
* 2014-1216: 1.48 (Biotronic) for KSP 0.24
	+ Added .90 support! (screw Curse for not having that option yet)(Admin Edit: Curse added it! File updated to reflect the proper version)
	+ Cleaned up autoscale and chain scaling!
* 2014-1117: 1.47 (Biotronic) for KSP 0.24
	+ Removed [RealChute](http://forum.kerbalspaceprogram.com/threads/57988) support
	+ Fixed a bug where TweakScale would try to set erroneous values for some fields and properties, which notably affected [Infernal Robotics](http://forum.kerbalspaceprogram.com/threads/37707)
	+ Fixed a bug where cloned fuel tanks would have erroneous volumes.
* 2014-1116: 1.46 (Biotronic) for KSP 0.24
	+ Fixed an issue where features were incorrectly scaled upon loading a ship,
	+ Scaling a part now scales its children if they have the same size.
	+ Parts now automatically guess which size they should be.
* 2014-1115: 1.45 (Biotronic) for KSP 0.24
	+ New Features:
		- Now updating UI sliders for float values.
		- Better support for [KSP Interstellar](http://forum.kerbalspaceprogram.com/threads/43839).
		- Added support for [TweakableEverything](http://forum.kerbalspaceprogram.com/threads/64711).
		- Added support for [FireSpitter](http://forum.kerbalspaceprogram.com/threads/24551)'s FSFuelSwitch.
* 2014-1010: 1.44 (Biotronic) for KSP 0.24
	+ Version 1.44:
		- Updated for KSP 0.25
		- Added ability to not update certain properties when a specific partmodule is on the part.
	+ Thanks a lot to NathanKell, who did all the work on this release!
* 2014-0824: 1.43 (Biotronic) for KSP 0.24
	+ Version 1.43:
		- Added licence file (sorry, mods!)
		- No longer chokes on null particle emitters.
* 2014-0815: 1.41 (Biotronic) for KSP 0.24
	+ Version 1.41:
		- Fixed scaling of Part values in unnamed TWEAKSCALEEXPONENTS blocks.
* 2014-0813: 1.40 (Biotronic) for KSP 0.24
	+ Version 1.40:
		- Removed [Karbonite](http://forum.kerbalspaceprogram.com/threads/89401) cfg, since that project does its own TweakScale config.
* 2014-0812: 1.39 (Biotronic) for KSP 0.24
	+ Version 1.39:
		- Fixed cost calculation for non-full tanks.
* 2014-0812: 1.38 (Biotronic) for KSP 0.24
	+ Version 1.38:
		- Added scaling of FX.
		- Added support for [Banana for Scale](http://forum.kerbalspaceprogram.com/threads/89570).
		- Updated [Karbonite](http://forum.kerbalspaceprogram.com/threads/87335) support.
		- Fixed a bug where no scalingfactors available due to tech requirements would lead to unintended consequences.
* 2014-0805: 1.37 (Biotronic) for KSP 0.24
	+ Version 1.37:
		- Updated cost calculation.
* 2014-0804: 1.36 (Biotronic) for KSP 0.24
	+ Version 1.36:
		- Updated [Real Fuels](http://forum.kerbalspaceprogram.com/threads/64118) and [Modular Fuel Tanks](http://forum.kerbalspaceprogram.com/threads/64117) support.
		- Added [KSPX](http://forum.kerbalspaceprogram.com/threads/30472) support.
* 2014-0803: 1.35 (Biotronic) for KSP 0.24
	+ Corrected cost calculation.
	+ Updated to [KSPAPIExtensions 1.7.0](http://forum.kerbalspaceprogram.com/threads/81496)
* 2014-0802: 1.34 (Biotronic) for KSP 0.24
	+ Version 1.34:
		- Fixed a bug where repeated scaling led to inaccurate placing of child parts.
		- Added [Karbonite](http://forum.kerbalspaceprogram.com/threads/87335) support.
* 2014-0728: 1.33 (Biotronic) for KSP 0.24
	+ Updated RealFuels support for 7.1
* 2014-0725: 1.32 (Biotronic) for KSP 0.24
	+ Version 1.32:
		- Updated KSPAPIExtension for 0.24.2 support.
* 2014-0725: 1.31 (Biotronic) for KSP 0.24
	+ Fixed a bug where parts with defaultScale inaccessible due to tech requirements were incorrectly scaled.
* 2014-0725: 1.30 (Biotronic) for KSP 0.24
	+ Updated KSPAPIExtensions with 24.1 support.
	+ Re-enabled Real Fuels support.
	+ Added support for IPartCostModifier.
* 2014-0724: 1.29 (Biotronic) for KSP 0.24
	+ Fixed Modular Fuel Tanks support.
* 2014-0724: 1.28 (Biotronic) for KSP 0.24
	+ Fixed more cross-platform bugs.
	+ Added [Tantares Space Technologies](http://forum.kerbalspaceprogram.com/threads/80550).
* 2014-0723: 1.27 (Biotronic) for KSP 0.24
	+ Fixed a bug on non-Windows platforms.
* 2014-0723: 1.26 (Biotronic) for KSP 0.24
	+ Version 1.26:
		- Fixed typo in DefaultScales.cfg that caused som parts to baloon ridiculously.
		- Added support for updated NFT and KW Rocketry
* 2014-0723: 1.25 (Biotronic) for KSP 0.24
	+ Version 1.25:
		- Modular Fuel Tanks](http://forum.kerbalspaceprogram.com/threads/64117) yet again supported! (Still waiting for Real Fuels)
		- Refactored IRescalable system to be easier for mod authors.
		- Fixed a bug where one field could have multiple exponents, and thus be rescaled multiple times.
* 2014-0721: 1.23 (Biotronic) for KSP 0.24
	+ Version 1.23:
		- Duplicate TweakScale dlls no longer interfere.
* 2014-0720: 1.22 (Biotronic) for KSP 0.24
	+ Version 1.22
		- Fixed tanks that magically refill.
		- Fixed technology requirements are too slow.
		- Updated KSPAPIExtensions to make TweakScale play nicely with other mods.
* 2014-0718: 1.21 (Biotronic) for KSP 0.24
	+ Version 1.21:
		- Updated for 0.24
		- Now supports global, per-part, and per-scaletype scaling of features (like mass, buoyancy, thrust, etc)
* 2014-0613: 1.20 (Biotronic) for KSP 0.23.5
	+ Version 1.20:
		- New algorithm for rescaling attach nodes. Tell me what you think!
		- Added [Deadly Reentry Continued](http://forum.kerbalspaceprogram.com/threads/54954) and [Large Structural/Station Components](http://forum.kerbalspaceprogram.com/threads/34664).
* 2014-0606: 1.19 (Biotronic) for KSP 0.23.5
	+ Version 1.19:
		- Added support for tech requirements for non-freescale parts.
* 2014-0606: 1.18 (Biotronic) for KSP 0.23.5
	+ Version 1.18
		- Factored out Real Fuels and Modular Fuel Tanks support to separate dlls.
* 2014-0603: 1.17 (Biotronic) for KSP 0.23.5
	+ Version 1.17:
		- Fixed bug where attachment nodes were incorrectly scaled after reloading. This time with more fix!
		- Added support for [Near Future Technologies](http://forum.kerbalspaceprogram.com/threads/52042).
* 2014-0603: 1.16 (Biotronic) for KSP 0.23.5
	+ Version 1.16:
		- Fixed bug where attachment nodes were incorrectly scaled after reloading.
* 2014-0603: 1.15 (Biotronic) for KSP 0.23.5
	+ Version 1.15:
		- Finally squished the bug where crafts wouldn't load correctly. This bug is present in 1.13 and 1.14, and affects certain parts from Spaceplane+, MechJeb, and KAX.
* 2014-0603: 1.14 (Biotronic) for KSP 0.23.5
	+ Version 1.14:
		- Fixed a bug where nodes with the same name were moved to the same location regardless of correct location. (Only observed with KW fairing bases, but there could be others)
* 2014-0602: 1.13 (Biotronic) for KSP 0.23.5
	+ Version 1.13:
		- Added support for [MechJeb](http://forum.kerbalspaceprogram.com/threads/12384), [Kerbal Aircraft eXpanion](http://forum.kerbalspaceprogram.com/threads/76668), [Spaceplane+](http://forum.kerbalspaceprogram.com/threads/80796), [Stack eXTensions](http://forum.kerbalspaceprogram.com/threads/79542), [Kerbal Attachment System](http://forum.kerbalspaceprogram.com/threads/53134), [Lack Luster Labs](http://forum.kerbalspaceprogram.com/threads/24906), [Firespitter](http://forum.kerbalspaceprogram.com/threads/24551), [Taverio's Pizza and Aerospace](http://forum.kerbalspaceprogram.com/threads/15348), [Better RoveMates](http://forum.kerbalspaceprogram.com/threads/75873), and [Sum Dum Heavy Industries Service Module System](http://forum.kerbalspaceprogram.com/threads/48357).
		- Fixed a bug where Modular Fuel Tanks were not correctly updated.
* 2014-0602: 1.12 (Biotronic) for KSP 0.23.5
	+ Version 1.12:
		- Added support for [КОСМОС](http://forum.kerbalspaceprogram.com/threads/24970).
		- No longer scaling heatDissipation, which I was informed was a mistake.
* 2014-0601: 1.11 (Biotronic) for KSP 0.23.5
	+ 1.11:
		- Removed silly requirement of 'name = *' for updating all elements of a list.
		- Added .cfg controlled scaling of Part fields.
* 2014-0601: 1.10 (Biotronic) for KSP 0.23.5
	+ 1.10:
		- Added support for nested fields.
* 2014-0531: 1.9 (Biotronic) for KSP 0.23.5
	+ Version 1.9
		- Fixed bugs in 1.8 where duplication of parts caused incorrect scaling.
* 2014-0530: 1.8 (Biotronic) for KSP 0.23.5
	+ Version 1.8:
		- Fixed a bug where rescaleFactor caused incorrect scaling.
		- Added (some) support for [Kethane](http://forum.kerbalspaceprogram.com/threads/23979-Kethane-Pack-0-8-5-Find-it-mine-it-burn-it!-0-23-5-\(ARM\)-compatibility-update) parts.
* 2014-0522: 1.7 (Biotronic) for KSP 0.23.5
	+ No changelog provided
* 2014-0522: 1.6 (Biotronic) for KSP 0.23.5
	+ Version 1.6:
		- Fixed a problem where parts were scaled back to their default scale after loading, duplicating and changing scenes.
* 2014-0520: 1.5.0.1 (Biotronic) for KSP 0.23.5
	+ Version 1.5.0.1:
		- Fixed a bug in 1.5
		- Changed location of KSPAPIExtensions.dll
* 2014-0520: 1.5 (Biotronic) for KSP 0.23.5
	+ Version 1.5
		- Changed from hardcoded updaters to a system using cfg files.
* 2014-0520: 1.4 (Biotronic) for KSP 0.23.5
	+ Version 1.4
		- Fixed incompatibilities with GoodspeedTweakScale
* 2014-0519: 1.3 (Biotronic) for KSP 0.23.5
	+ Version 1.3
		- Fixed a bug where parts would get rescaled to stupid sizes after loading.
		- Breaks compatibility with old version of the plugin (pre-1.0) and GoodspeedTweakScale. :(
* 2014-0518: 1.2 (Biotronic) for KSP 0.23.5
	+ Version 1.2 (2014-05-18, 22:00 UTC):
		- Fixed default scale for freeScale parts.
		- Fixed node sizes, which could get absolutely redonkulous. Probably not perfect now either.
		- B9, Talisar Cargo Transportation Solutions, and NASA Module Manager configs.
		- Now does scaling at onload, removing the problem where the rockets gets embedded in the ground and forcibly eject at launch.
		- Fixed a silly bug in surface scale type.
* 2014-0516: 1.1 (Biotronic) for KSP 0.23.5
	+ Version 1.1:
		- Added scaling support for [B9 Aerospace ](http://forum.kerbalspaceprogram.com/threads/25241)and [Talisar's Cargo Transportation Solutions](http://forum.kerbalspaceprogram.com/threads/77505)
		- Will now correctly load (some) save games using an older version of the plugin.
* 2014-0516: 1.0 (Biotronic) for KSP 0.23.5
	+ No changelog provided

- - -
	WiP : Work In Progress
	RiP : Research In Progress

