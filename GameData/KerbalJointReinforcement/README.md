# Kerbal Joint Reinforcement /L Experimental

Physics stabilizer plugin for Kerbal Space Program. Fork by Lisias.

This branch is confirmed to be KSP 1.2 to 1.5.1 compatible, with configuration/settings files saved to `<KSP_ROOT>/PluginData`.

This is the last fork **have no** assured backward compatibility with ferram4's KJR.


## In a Hurry

This is kind of "Take Over", where I will develop the plugin to meet my own needs. This probably will break the expected behaviour from previous versions.

If you need an updated, consistent and stable version from the Ferram4's KJR, but want some minor but cool features, this is the branch for you. On the other hand, if you prefer a classical and well known approach, perhaps you should try [the cannonical branch](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/tree/master).

* [Latest Release](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/releases)
    + [Binaries](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/tree/Archive)
* [Source](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/tree/experimental)
* [Issue Tracker](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/issues)
* Documentation	
	+ [Homepage](http://ksp.lisias.net/add-ons/Kerbal-Joint-Reinforcement) on L Aerospace KSP Division
	+ [Project's README](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/blob/master/README.md)
	+ [Install Instructions](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement/blob/master/INSTALL.md)
	+ [Change Log](./CHANGE_LOG.md)
	+ [TODO](./TODO.md) list
* Official Distribution Sites:
	+ [Homepage](http://ksp.lisias.net/add-ons/KerbalJointReinforcement) on L Aerospace
	+ [Source and Binaries](https://github.com/net-lisias-ksp/Kerbal-Joint-Reinforcement) on GitHub.


## Description

This is still about the original fork. It's going to change. Eventually.

### EXCITING FEATURES!

+ Physics Easing
	- Slowly dials up external forces (gravity, centrifugal, coriolis) when on the surface of a planet, reducing the initial stress during loading
	- All parts and joints are strengthened heavily during physics loading (coming off of rails) to prevent Kraken attacks on ships
+ Launch Clamp Easing
	- Prevents launch clamps from shifting on load, which could destroy the vehicle on the pad
+ Stiffen interstage connections
	- Parts connected to a decoupler will be connected to each other, reducing flex at the connection to reasonable levels
+ Stiffen launch clamp connections
	- Less vehicle movement on vessel initialization
	- Warning: may cause spontaneous rocket disintegration if rocket is too large and overconstrained (far too many lanuch clamps; their connections will fight each other and give rise to phantom forces)
+ Increase stiffness and strengths of connections
	- Larger parts will have stiffer connections to balance their larger masses / sizes
	- Sequential parts in a stack will be connected with a stiff, but weak connection to add even more stiffness and counteract wobble
+ Option to make connection strengths weaker to counteract increases in stiffness
+ Joint Stiffness parameters can be tweaked:
	- Default, standard values are in the config.xml in the add-on install folder
		- `<RSP_ROOT>/GameData/KerbalJointReinforcement/PluginData/config.xml`
	- User customizable user.xml, using the same syntax, can be found on the PluginData folder on your KSP root directory
		- `<RSP_ROOT>/PluginData/KerbalJointReinforcement/user.xml`

### config value documentation:

There are three possible configuration files:

* `<KSP_ROOT>/KerbalJointReinforcement/PluginData/config.xml`
	+ **Not user serviceable**
	+ Default values, defined by the Maintainer, will be available here.
	+ This file can, and must, be replaced on every new KJR/L version.
* `<KSP_ROOT>/PluginData/KerbalJointReinforcement/user.xml`
	+ User serviceable equivalent to the previous one.
	+ Any custom settings must be added here, using the very same syntax from the `config.xml`.
	+ Settings on this file **overwrites** or are **appended** to the ones from `config.xml`
		- **DO NOT** replicate the `exempt*` items from `config.xml`, or you will have duplicates on memory.
		- Not a big deal, but it wastes memory.
* `<KSP_ROOT>/PluginData/KerbalJointReinforcement/user.cfg`
	+ The **very same data**, but on a familiar KSP Config format. :)
		- This file has precedence over `user.xml`. If it is present, the xml version is ignored.
	+ This format is slightly easier to maintain, as you don't need to keep track of the counter on the `exempt*` tags as in the `user.xml`.

Example for a `user.cfg` file:

```
KJR
{
	debug = False
	breakTorqueMultiplier = 0.5
	Exempt
	{
		PartType = foo
		ModuleType = bar
		DecouplerStiffeningExtensionType = foobar
	}
	Exempt
	{
		DecouplerStiffeningExtensionType = barfoo
	}
	
}
```	  

#### General Values

| Type | Name | Default Value | Action | 
|:------|:------|:--------------|:----------|
| bool	| reinforceAttachNodes	| 1 | Toggles stiffening of all vessel joints |
| bool	| multiPartAttachNodeReinforcement	| 1	| Toggles additional stiffening by connecting parts in a stack one part further, but at a weaker strength |
| bool	| reinforceDecouplersFurther	| 1	| Toggles stiffening of interstage connections |
| bool	| reinforceLaunchClampsFurther	| 1	| Toggles stiffening of launch clamp connections |
| bool	| useVolumeNotArea	| 1	| Switches to calculating connection area based on volume, not area; not technically correct, but allows a better approximation of very large rockets |
| bool	| debug	| 0	| Toggles debug output to log; please activate and provide log if making a bug report |
| float	| massForAdjustment	| 0.01	| Parts below this mass will not be stiffened |
| float	| stiffeningExtensionMassRatioThreshold	| 5	| Sets mass ratio needed between parts to extend Decoupler Stiffening one part further than it normally would have gone; essentially, if the code would have stopped at part A, but part B that it is connected to is >5 times as massive as part A, include part B |
| float	| decouplerAndClampJointStrength	| -1	| Sets breaking strength for joints involved in decoupler and clamp additional strengthening; -1 makes them unbreakable |

#### Angular "Drive" Values (universally scales angular strength of connections)
| Type | Name | Default Value | Action | 
|:------|:------|:--------------|:----------|
| float	| angularDriveSpring	| 5e12	| Factor used to scale stiffness of angular connections |
| float	| angularDriveDamper	| 25	| Factor used to scale damping of motion in angular connections
| float	| angularMaxForceFactor	| -1	| Factor used to scale maximum force that can be applied before connection "gives out"; does not control joint strength; -1 makes this value infinite |

#### Joint Strength Values
| Type | Name | Default Value | Action | 
|:------|:------|:--------------|:----------|
| float	| breakForceMultiplier	| 1	| Factor scales the failure strength (for forces) of joint connections; 1 gives stock strength |
| float	| breakTorqueMultiplier	| 1	| Factor scales the failure strength (for torque) of joint connections; 1 gives stock strength |
| float	| breakStrengthPerArea	| 1500	| Overrides above values if not equal to 1; joint strength is based on the area of the part and failure strength is equal to this value times connection area |
| float	| breakTorquePerMOI	| 6000	| Same as above value, but for torques rather than forces and is based on the moment of inertia, not area |

#### Part and Module Exemptions
| Type | Name | Default Value | Action | 
|:------|:------|:--------------|:----------|
| string	| exemptPartType0	| MuMechToggle	| Part stiffening not applied to this type of "Part"; exemption to avoid interference with Infernal Robotics |
| string	| exemptPartType1	| MuMechServo	| Part stiffening not applied to this type of "Part"; exemption to avoid interference with Infernal Robotics |
| string	| exemptModuleType0	| WingManipulator	| Part stiffening not applied to parts with this type of PartModule; exemption to prevent problems with pWings |
| string	| exemptModuleType1	| SingleGroupMan	| Part stiffening not applied to parts with this type of PartModule; exemption to prevent problems with procedural adapter included with pWings |
| string	| exemptModuleType2	| KerbalEVA	| Part stiffening not applied to parts with this type of PartModule; exemption to prevent problems with Kerbals in command seats |
| string	| exemptModuleType3	| MuMechToggle	| Part stiffening not applied to parts with this type of PartModule; exemption to prevent problems with Kerbals in command seats |
| string	| exemptModuleType4	| WingProcedural	| Part stiffening not applied to parts with this type of PartModule; exemption to prevent problems with Kerbals in command seats |

Further part and module exemptions can be added using the same formating and changing the number


#### Decoupler Stiffening Extension Types
| Type | Name | Default Value | Action | 
|:------|:------|:--------------|:----------|
| string	| decouplerStiffeningExtensionType0	| ModuleEngines	| Decoupler stiffening will look for parts beyond this part type to add to stiffening
| string	| decouplerStiffeningExtensionType1	| ModuleEnginesFX	| Decoupler stiffening will look for parts beyond this part type to add to stiffening
| string	| decouplerStiffeningExtensionType2	| ModuleHybridEngine	| Decoupler stiffening will look for parts beyond this part type to add to stiffening
| string	| decouplerStiffeningExtensionType3	| ModuleHybridEngines	| Decoupler stiffening will look for parts beyond this part type to add to stiffening
| string	| decouplerStiffeningExtensionType4	| ModuleEngineConfigs	| Decoupler stiffening will look for parts beyond this part type to add to stiffening

These types are currently not used, but removing the a in front of them will cause KJR to make use of them again; their lack should not affect stiffening appreciably but does help reduce overhead and strange stiffening situations

| Type | Name | Default Value | Action | 
|:------|:------|:--------------|:----------|
| string	| adecouplerStiffeningExtensionType5	| ModuleDecouple	| Decoupler stiffening will look for parts beyond this part type to add to stiffening |
| string	| adecouplerStiffeningExtensionType6	| ModuleAnchoredDecoupler	| Decoupler stiffening will look for parts beyond this part type to add to stiffening |
| string	| adecouplerStiffeningExtensionType7	| ProceduralFairingBase		| Decoupler stiffening will look for parts beyond this part type to add to stiffening |


## Installation

Detailed installation instructions are now on its own file (see the [In a Hurry](#in-a-hurry) section) and on the distribution file.

### License

GPL3. See [here](./LICENSE).

Please note the copyrights and trademarks in [NOTICE](./NOTICE).


## UPSTREAM

* [ferram4](https://forum.kerbalspaceprogram.com/index.php?/profile/21328-ferram4/) ROOT
	+ [Forum](https://forum.kerbalspaceprogram.com/index.php?/topic/50911-13-kerbal-joint-reinforcement-v333-72417/)
	+ [SpaceDock](http://spacedock.info/mod/153/Kerbal%20Joint%20Reinforcement)
	+ [GitHub](https://github.com/ferram4/Kerbal-Joint-Reinforcement)
* [StarWaster](https://forum.kerbalspaceprogram.com/index.php?/profile/71262-starwaster/) Parallel Fork
	+ [GitHub](https://github.com/KSP-RO/Kerbal-Joint-Reinforcement-Continued)
* [linuxgurugamer](https://forum.kerbalspaceprogram.com/index.php?/profile/129964-linuxgurugamer/) Parallel fork
	+ [GitHub](https://github.com/linuxgurugamer/Kerbal-Joint-Reinforcement)
* [oeteletroll](https://forum.kerbalspaceprogram.com/index.php?/profile/144573-peteletroll/) Parallel?
	+ [GitHub](https://github.com/peteletroll/Kerbal-Joint-Reinforcement)
