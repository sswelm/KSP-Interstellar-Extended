Kerbal Joint Reinforcement Next, v4.0.0
===============================

Physics stabilizer plugin for Kerbal Space Program

Source available at: https://github.com/meirumeiru/Kerbal-Joint-Reinforcement

***************************************************
****** INSTALLING KERBAL JOINT REINFORCEMENT ******
***************************************************

Merge the GameData folder with the existing one in your KSP directory.  KSP will then load it as an add-on.
The source folder simply contains the source code (in C#) for the plugin.  If you didn't already know what it was, you don't need to worry about it; don't copy it over.


********************************
****** EXCITING FEATURES! ******
********************************



-- Physics Easing

	- Slowly dials up external forces (gravity, centrifugal, coriolis) when on the surface of a planet, reducing the initial stress during loading
	- All parts and joints are strengthened heavily during physics loading (coming off of rails) to prevent Kraken attacks on ships

-- Launch Clamp Easing

	- Prevents launch clamps from shifting on load, which could destroy the vehicle on the pad

-- Stiffen interstage connections

	- Parts connected to a decoupler will be connected to each other, reducing flex at the connection to reasonable levels


-- Stiffen launch clamp connections

	- Less vehicle movement on vessel initialization
	- Warning: may cause spontaneous rocket disintegration if rocket is too large and overconstrained (far too many lanuch clamps; their connections will fight each other and give rise to phantom forces)


-- Increase stiffness and strengths of connections

	- Larger parts will have stiffer connections to balance their larger masses / sizes
	- Sequential parts in a stack will be connected with a stiff, but weak connection to add even more stiffness and counteract wobble

-- Option to make connection strengths weaker to counteract increases in stiffness


-- Joint Stiffness parameters can be tweaked in included config.xml file

	- config value documentation:


General Values

	Type	Name					Default Value		Action

	bool	reinforceAttachNodes			1			--Toggles stiffening of all vessel joints
	bool	multiPartAttachNodeReinforcement	1			--Toggles additional stiffening by connecting parts in a stack one part further, but at a weaker strength
	bool	reinforceDecouplersFurther		1			--Toggles stiffening of interstage connections
	bool	reinforceLaunchClampsFurther		1			--Toggles stiffening of launch clamp connections
	bool	useVolumeNotArea			1			--Switches to calculating connection area based on volume, not area; not technically correct, but allows a better approximation of very large rockets
	bool	debug					0			--Toggles debug output to log; please activate and provide log if making a bug report
	float	massForAdjustment			0.01			--Parts below this mass will not be stiffened
	float	stiffeningExtensionMassRatioThreshold	5			--Sets mass ratio needed between parts to extend Decoupler Stiffening one part further than it normally would have gone; essentially, if the code would have stopped at part A, but part B that it is connected to is >5 times as massive as part A, include part B
	float	decouplerAndClampJointStrength		-1			--Sets breaking strength for joints involved in decoupler and clamp additional strengthening; -1 makes them unbreakable

Angular "Drive" Values (universally scales angular strength of connections)

	Type	Name				Default Value		Action

	float	angularDriveSpring		5e12			--Factor used to scale stiffness of angular connections
	float	angularDriveDamper		25			--Factor used to scale damping of motion in angular connections
	float	angularMaxForceFactor		-1			--Factor used to scale maximum force that can be applied before connection "gives out"; does not control joint strength; -1 makes this value infinite

Joint Strength Values

	Type	Name				Default Value		Action

	float	breakForceMultiplier		1			--Factor scales the failure strength (for forces) of joint connections; 1 gives stock strength
	float	breakTorqueMultiplier		1			--Factor scales the failure strength (for torque) of joint connections; 1 gives stock strength
	float	breakStrengthPerArea		1500			--Overrides above values if not equal to 1; joint strength is based on the area of the part and failure strength is equal to this value times connection area
	float	breakTorquePerMOI		6000			--Same as above value, but for torques rather than forces and is based on the moment of inertia, not area

Decoupler Stiffening Extension Types

	Type	Name					Default Value		Action

	string	decouplerStiffeningExtensionType0	ModuleEngines		--Decoupler stiffening will look for parts beyond this part type to add to stiffening
	string	decouplerStiffeningExtensionType1	ModuleEnginesFX		--Decoupler stiffening will look for parts beyond this part type to add to stiffening
	string	decouplerStiffeningExtensionType2	ModuleHybridEngine	--Decoupler stiffening will look for parts beyond this part type to add to stiffening
	string	decouplerStiffeningExtensionType3	ModuleHybridEngines	--Decoupler stiffening will look for parts beyond this part type to add to stiffening
	string	decouplerStiffeningExtensionType4	ModuleEngineConfigs	--Decoupler stiffening will look for parts beyond this part type to add to stiffening

These types are currently not used, but removing the a in front of them will cause KJR to make use of them again; their lack should not affect stiffening appreciably but does help reduce overhead and strange stiffening situations

	string	adecouplerStiffeningExtensionType5	ModuleDecouple		--Decoupler stiffening will look for parts beyond this part type to add to stiffening
	string	adecouplerStiffeningExtensionType6	ModuleAnchoredDecoupler	--Decoupler stiffening will look for parts beyond this part type to add to stiffening
	string	adecouplerStiffeningExtensionType7	ProceduralFairingBase	--Decoupler stiffening will look for parts beyond this part type to add to stiffening


***********************
****** CHANGELOG ******
***********************

v4.0.0	-> will be the new version of KJR, a complete re-development where just ideas are kept from old versions

v3.x.x	-> previous KJR versions
