Tweakscale Documentation - Configuration

Target audience: people who write part configs and ModuleManager patches. 

Table of Contents

1. TWEAKSCALEEXPONENTS
2. SCALETYPE
3. Modules
4. Field Definitions


==================================================

1.  TWEAKSCALEEXPONENTS

When an object is scaled, its properties scale (approximately) with a power law:

p_new = p_old * scaleFactor ^ exponent

The exponent depends on the value, for example it is 1 for length, 2 for surface area 
and 3 for volume. It can be any real number, and will usually be somewhere between -1 and 3. 

ConfigNodes of type "TWEAKSCALEEXPONENTS" are used to define which values should be scaled, 
and which exponent to use. Reasonable values for commonly used values are defined in 
the file ScaleExponents.cfg. 

A TWEAKSCALEEXPONENTS node looks like the following:

    TWEAKSCALEEXPONENTS
    {
        name = Part
        breakingForce = 2
        breakingTorque = 2
        buoyancy = 3
        explosionPotential = 3
        mass = 3
        CrewCapacity = 2
    
        Resources
        {
            !amount = 3
            !maxAmount = 3
            -ignore = ModuleFuelTanks
        }
    
        attachNodes
        {
            breakingForce = 2
            breakingTorque = 2
        }
    }

The name is either "Part" or the name of a partModule. Note that the names of the exponents 
refer to the internal names of C#-variables in the game (or mod) code, which are not always 
identical to the names found in the part config. 

If a field begins with an Exclamation Point (!), then the value is used for 
relative scaling (needed for resource nodes, which are persistent and also written by other 
sources). Unfortunately this conflicts with ModuleManager syntax, so it can not be used in 
MM patches. 

The "-ignore" means "do not touch resource nodes if a part module of name 'ModuleFuelTanks' 
is present". 

==================================================

2.  SCALETYPE

SCALETYPE is used to define a group of standard values which can be referenced 
in a module definition with a "type" value. This is mainly used for the UI
(i.e. ""), but can also have embedded TWEAKSCALEEXPONENTS nodes. 

A typical example looks like this (for more see DefaultScales.cfg):

    SCALETYPE
    {
        name = free
        freeScale = true
        defaultScale = 100
        suffix = %
        scaleFactors   = 10, 50, 100,  200, 400
        incrementSlide =  1,  1,   2,    5
    }


==================================================

3.  TweakScale Modules

To make a part scaleable, Tweakscale needs its module added to the part. A typical 
module will look like the following (see the patches folder for more examples):

    MODULE
    {
        name = TweakScale
        type = stack
        defaultScale = 2.5
    }

The config node can contain overrides for values from the SCALETYPE, as well as 
embedded TWEAKSCALEEXPONENTS nodes (which will affect only this part).

The preferable way is to specify most parameters globally or inside SCALETYPE nodes. 

When writing your own part patches, the simplest way is to search for similar parts 
in the TweakScale/patches folder, and copy from there. For example: 

    @PART[fuelTank] // FL-T400 Fuel Tank
    {
        %MODULE[TweakScale]
        {
            type = stack
            defaultScale = 1.25
        }
    }

==================================================

4. Field Definitions

The individual fields are:
    name = TweakScale         Module name
    type =                    References a defined SCALETYPE
    defaultScale =            The UI scale that corresponds to the unscaled part
    freeScale =               false: only the sizes in the scaleFactors list are available
                              true:  intermediate scales are available (the tweakable has a slider)
    suffix = (m/%)            UI: Suffix for the scale display
    scaleFactors =            UI: Mayor steps for the tweakable (reachable with the arrow buttons). 
    scaleNames =              UI: Scale names (only freescale=false)
    incrementSlide =          UI: step size for the slider (only freescale=true)

Deprecated fields: scaleNodes, minScale, maxScale, incrementLarge, incrementSmall, 