# Kerbal Joint Reinforcement /L :: Change Log

* 2018-1229: 3.4.0.4 (lisias) for KSP >= 1.2
	+ Adding support for ConfigNode (CFG) file format for the *user serviceable settings file* (`user.cfg`) on `<KSP_ROOT>/PluginData/KerbalJointReinforcement`
	+ Lifting the Max KSP restriction on `.version` file.
	+ This release **demands** the [newest KSPe](https://github.com/net-lisias-ksp/KSPAPIExtensions/releases), or things will not work. 
* 2018-1206: 3.4.0.3 (lisias) for {1.2 <= KSP <= 1.5.1}
	+ Splitting configuration files between **stock** and **user customizable** files.
	+ Some love to Logging
		- You will be flooded by log messages on debug mode! 
	+ Adding a INSTALL.md file with proper install instructions
* 2018-1202: 3.4.0.2 (lisias) for KSP 1.2 & 1.3 & 1.4 & 1.5
	+ Logging `config.xml` status when loading.
	+ REMERGE from ferrram4's and meiru's changes
		- Previous merge was trashed.
		- This is a new code-tree
	+ Bumping up version to match meiru's codetree 
	+ (Really now) Preventing KJR to mess with [Global/Ground Construction](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3497716).
		- Fixed as instructed on [Critter79606](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3494635) :)
	+ Preventing KJR to mess with [DockRotate](https://forum.kerbalspaceprogram.com/index.php?/topic/170484-15-14-dockrotate-lightweight-robotics-rotational-control-on-docking-ports-plus-noderotate-make-any-part-rotate/).
		- Change from [peteletroll](https://forum.kerbalspaceprogram.com/index.php?/profile/144573-peteletroll/), also mentioned by [AccidentalDisassembly](https://forum.kerbalspaceprogram.com/index.php?/topic/171377-130l-145-grounded-modular-vehicles-r40l-new-light-texture-switch-alternatives-fixes-oct-9-2018/&do=findComment&comment=3316608)
* 2018-1202: 3.4.0.1 (lisias) for KSP 1.2 & 1.3 & 1.4 & 1.5
	* DITCHED due a small fix on `config.xml`
* 2018-1129: 3.4.0.0 (lisias) for KSP 1.2 & 1.3 & 1.4 & 1.5
	+ DITCHED due a dumb mistake on .version
* 2018-1127: 3.3.3.4 (lisias) for KSP 1.2 & 1.3 & 1.4 & 1.5
	+ Tested (almost properly) on KSP 1.2 :)
		- 'Unifying' the releases in a single distribution file. 
	+ Fixed a typo on the configuration file
		- Thanks, [Critter79606](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3494635)
	+ Preventing KJR to mess with [Global/Ground Construction](https://forum.kerbalspaceprogram.com/index.php?/topic/154167-145-global-construction/).
		- Thanks again, , [Critter79606](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/&do=findComment&comment=3494635) :)
	+ Using KSPe Logging Facilities
		- I expect that errors on the LoadConstant method do not pass through silently again.  
* 2018-1119: 3.3.3.3 (lisias) for KSP 1.3 & 1.4 & 1.5
	+ Allowing KJR to run on 1.5 series.
		- For future laughing: https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/issues/1
* 2018-0820: 3.3.3.2 (lisias) for KSP 1.3 & 1.4
	+ Merging linuxgurugamer's merge from meirumeiru's code
	+ Project reworked for Multi KSP Versions support
* 2018-0819: 3.3.3.1 (lisias) for KSP 1.3.1
	+ Moving configuration/settings files to <KSP_ROOT>/PluginData 
	+ Added hard dependency for [KSP API Extensions/L](https://github.com/net-lisias-ksp/KSPAPIExtensions).
* 2017-0724: 3.3.3 (ferram4) for KSP 1.3.0
	+ Features
		- Recompile against KSP 1.3, ensure CompatChecker compatibility with 1.3
* 2017-0522: 3.3.2 (ferram4) for KSP 1.2.2
	+ Bugfixes
		- Fix multijoints breaking IR joints and any other exempted parts from moving
* 2016-1029: 3.3.1 (ferram4) for KSP 1.2
	+ Bugfixes
		- Fix a critical bug involving unphysical forces applied to vessels on load / unload of other vessels and SOI switches
* 2016-1027: 3.3.0 (ferram4) for KSP 1.2
	+ Features
		- Recompile to fix for KSP 1.2
		- Update method of handling multi-part-joints to ensure compatibility with Konstruction mod
		- Removal of old symmetry-based multi-part stabilization due to ineffectiveness in all situations to reduce overhead
		- Implementation of new vessel-part-tree leaf-based stabilization for greater stability on space stations and other convoluted shapes
* 2016-0630: 3.2 (ferram4) for KSP 1.1.3
	+ Features
		- Recompile to ensure KSP 1.1.3 compatibility
		- Change multi-part-joint system to stabilize space stations and similar vehicles with very large masses connected by very flexy parts
* 2016-0430: 3.1.7 (ferram4) for KSP 1.1.2
	+ Features
		- Recompile to ensure KSP 1.1.2 compatibility, especially within CompatibilityChecker utility
* 2016-0429: 3.1.6 (ferram4) for KSP 1.1.1
	+ Features
		- Update to ensure KSP 1.1.1 compatibility
		- Minor optimization in joint setups
		- Remove B9 pWings from stiffening exemption, as it is unnecessary
* 2016-0420: 3.1.5 (ferram4) for KSP 1.1
	+ Features
		- Updated to be compatible with KSP 1.1
		- Very minor efficiency improvements in physics easing and stiffening of joints
		- Fully exempt EVAs from all KJR effects
		- Update config parameters to function with stock fixing of never-breakable joints bug
* 2015-0622: 3.1.4 (ferram4) for KSP 1.0.5
	+ Misc
		- Fixed issue with .version file and compatible KSP versions
* 2015-0427: 3.1.3 (ferram4) for KSP 1.0
	+ Update for KSP 1.0
* 2015-0326: 3.1.2 (ferram4) for KSP 0.90
	+ Features
		- Added code to slightly stiffen connections between symmetrically-connected parts attached to a central part; should reduce some physics weirdness
	+ BugFixes
		- Fixed issue where undocking was impossible.
* 2015-0115: 3.1.1 (ferram4) for KSP 0.90
	+ BugFixes
		- Fixed a serious lock-to-worldspace issue involving multipart joints and physicsless parts
* 2015-0113: 3.1 (ferram4) for KSP 0.90
	+ Features
		- Set multipart joints to account for large mass ratios in choosing which parts to join
		- Set Decoupler Stiffenning to require the connection of immediate decoupler children to stiffen things even further
	+ BugFixes
		- Fixed a decoupling issues with multipart joints
		- Fixed multipart joint lock-to-worldspace issues
		- Fixed some issues on loading very large, heavy parts
* 2014-1229: 3.0.1 (ferram4) for KSP 0.90
	+ BugFixes
		- Fix some issues involving multipart joints
		- More null checking for situations that shouldn't happen, but might
* 2014-1228: 3.0 (ferram4) for KSP 0.90
	+ Features
		- MultiPart joints: weak, but stiff connections along a stack that will add even more stiffness without making the connection cheatingly strong
		- Proper, guaranteed application of stiffened properties, regardless of stock joint parameters
		- Updated default config values for greater sanity
		- Refactoring of code for sanity
	+ BugFixes
		- Longstanding issue with radially-attached parts that were larger than their parent are now fixed
		- Many NREs from bad events or bad states now avoided
* 2014-1216: 2.4.5 (ferram4) for KSP 0.90
	+ Features
		- KSP 0.90 compatibility
		- Include some extra checks to prevent errors from occurring
* 2014-1007: 2.4.4 (ferram4) for KSP 0.25
	+ Features
		- KSP 0.25 compatibility
		- Update CompatibilityChecker
		- Shutdown functionality if CompatibilityChecker returns warnings
* 2014-0820: 2.4.3 (ferram4) for KSP 0.24.2
	+ 0.24.2 compatibility
* 2014-0724: 2.4.2 (ferram4) for KSP 0.24.1
	+ 0.24.1 compatibility
* 2014-0718: 2.4.1 (ferram4) for KSP 2.4
	+ Bugfixes:
	+ Included JsonFx.dll, which is required by ModStats
	+ Relabeled ModStatistics.dll to allow simple overwriting for ModStats updates
* 2014-0717: 2.4 (ferram4) for KSP 2.4
	+ Update to ensure KSP v2.4 compatibility
* 2014-0703: 2.3 (ferram4) for KSP 0.23.5
	+ Current for KSP v0.23.5

## Missing Binaries
* v2.2
	+ Features
		- Updated to function with KSP ARM Patch (KSP 0.23.5)
		- Removed inertia tensor fix, as it is now stock
		- Main stiffening / strengthening is now disabled by default due to stock joint improvements
		- Decoupler stiffening is now disabled by default due to stock joint improvements
	+ Bugfixes:
		- Vessels can no longer become permanently indestructible
* v2.1
	+ Features
		- Reduced extent of decoupler stiffening joint creation; this shoul reduce physics overhead
		- Code refactoring for additional performance gains
		- Removed physics easing effect on inertia tensors; was unnecessary and added more overhead
		- Workaround for the stock "Launch Clamps shift on the pad and overstress your ship" bug that is particularly noticeable with RSS
		- Clamp connections are stiffer; now allowed by above workaround
	+ Bugfixes
		- KAS struts no longer break on load
* v2.0
	+ Features
		- Full release of proper inertia tensors!  Massive parts will feel more massive.
		- Full release of greater physics easing!  Landed and pre-launch crafts will have gravitational, centrifugal and coriolis forces slowly added to them, reducing the initial physics jerk tremendously
		- Launch clamps are now much stiffer when connected to more-massive-than-stock mod parts
		- Tightened up default joint settings more
		- Decoupler Stiffening Extension will now extend one part further if it's final part is much less massive than its parent / child part
		- Added Majiir's CompatibilityChecker; this will simply warn the user if they are not using a compatible version of KSP
	+ Bugfixes
		- Joints during physics easing strengthened
* v2.0x2
	+ Features
		- Elaborated physics easing: joints' flexion range is initially great and decreases, and gravitational + rotating ref frame forces are cancelled out to resolve internal joint stresses ere loading the rocket
		- Greatly tightened default joint settings
	+ Bugfixes
		- Non-zero angular limits no longer wrongly reorient parts.
* v2.0x1
	+ Features
		- Fixed part inertia tensors: heavy, large objects should now "feel" more massive, and their connections should better behave. Thanks to a.g. for finding this one.
		- Slightly stiffened Launch Clamps
		- Removed v1.7's improper stiffening for stretchy tanks, which the ability to stretch stretchy tanks makes unnecessary
	+ Bugfixes
		- Non-zero angular limits no longer wrongly reorient parts.
* v1.7
	+ Features
		- Connection area can be from volume instead of connection area calculated; for very, very large vehicles that the standard settings cannot handle
		- Default joint parameters stiffened
		- Stretchy tanks stiffened--a better solution is being developed while this one helps RSS
	+ Bugfixes
		- Decoupling no longer further stiffens joints being deleted from non-staged decouplers during decoupling / partial crashing
* v1.6
	+ Features
		- BreakStrengthPerUnitArea will not override large breakForces, easing I-beams and structural elements' use
	+ Bugfixes
		- Fixed decoupler-dockingport combination parts from causing strange disassembly when undocking
* v1.5
	+ Features
		- Updated to KSP 0.23
		- Joint breaking strength can be set to increase with connection area so that large part connections can have realistically large strength; on by default
		- Vessels are further strengthened for the first 30 physics frames after coming off rails or loading, reducing initialization jitters.
	+ Bugfixes:
		- Launch clamps after staging remain clamped to the ground.
		- Kraken no longer throws launchpads at orbiting craft
* v1.4.2
	+ Bugfixes
		- Wobble reduced
		- General tweaks to reduce wobbling further
* v1.4.1
	+ Bugfixes
		- Maximum joint forces correctly calculated
		- Docking no longer causes exceptions to be thrown and cause lag
* v1.4
	+ Features
		- Increased calculation of surface-attached connection area's accuracy
	+ Bugfixes
		- Wobble between stack-attached parts of very different sizes greatly reduced
* v1.3
	+ Features
		- Better solution for failure to apply decoupler ejection forces
		- Will not stiffen parts below a given mass, which can be changed in config
		- Properly updates on docking
	+ Bugfixes
		- Launch clamps no longer to the surface lock ships
* v1.2
	+ Features
		- Workaround for stock KSP bug where struts would prevent decoupler ejection forces from being applied
	+ Tweaks
		- Reduced default maxForceFactors to be more reasonable levels
	+ BugFixes
		- Struts properly disconnect
		- Decouplers properly function
* v1.1
	+ Features
		- Stiffness of joint no longer erroneously dependent on breakForce / breakTorque
		- Decoupler stiffening function made more comprehensive
	+ BugFixes
		- Further decoupler stiffening affects radial decouplers
		- Decoupler further stiffening no longer causes Nulls to be thrown when attached to physics-disabled parts
		- Procedural fairings no longer locked to rockets
		- Infernal Robotics parts function
		- Temporary stopgap measure: stiffening not applied to pWings to prevent ultra-flexy wings
	+ Known Issues
		- Decouplers exert no detach force with extra decoupler stiffening enabled
	+ Same issues as strut attachment bug
* v1.0
	+ Release
