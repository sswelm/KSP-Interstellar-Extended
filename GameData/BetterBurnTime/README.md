# BetterBurnTime
KSP mod for a more accurate/reliable "estimated burn time" indicator on the navball.  Also provides estimated time-of-impact for landing on vacuum worlds, and time-to-closest-approach for orbital rendezvous.


##How to install
Unzip into your GameData folder, like any mod.


## What this mod does

* It tweaks the "estimated burn time" display on the navball so that it will be reliable and accurate. It takes into account increasing acceleration as fuel mass is burned.
* When the ship is in vacuum and on a collision course with the ground, it will automatically show time-to-impact, and the estimated burn time to kill your velocity at ground level.
* When the ship is in orbit and has a close rendezvous with a target ship, it will automatically show time-to-closest-approach, and the estimated burn time to kill your velocity relative to the target.

####What this means:

**For maneuver nodes:**

* You will see a burn time that will be accurate to the second.
* You will never see "N/A" unless your vessel actually can't run at all (e.g. is out of fuel or has no active engines).
* Shows a warning if the maneuver would require more fuel than you have (see below).

**For landing on planets/moons without atmosphere:**

* If you don't have any maneuver nodes set, and you're on a collision course with the ground, you'll see an estimated time-until-impact (instead of time-until-maneuver), and an estimated burn time to kill your velocity.
* This is useful when deciding when to do your retro-burn for landing.

**For orbital rendezvous**

* If you can set up a rendezvous that will take you within 10 km of the target, you'll see an estimated time-until-closest-approach (instead of time-until-maneuver), and an estimated burn time to match velocity with the target.



## Why it's needed
The "estimated burn time" indicator provided by stock KSP is very rudimentary. It just keeps track of the maximum acceleration observed for the current ship in the current flight session, and then assumes that. This has several drawbacks:

* You just see "N/A" until the first time you do acceleration in the flight session.
* It doesn't take into account the fact that your acceleration will get better as you burn fuel
* It doesn't deal with engines running out of fuel, being deactivated, etc.
* It happily tells you an estimate even if you don't have the fuel to do the burn.
* You have to do mental math all the time to split the burn across the maneuver node.

This mod addresses all the above problems.

![maneuver](https://raw.githubusercontent.com/KSPSnark/BetterBurnTime/master/screenshots/maneuver.png)


## Things that the mod handles

* It adds up the thrust of your engines to figure out what acceleration you can do.
* It knows how much fuel you have, how much your engines need, and which tanks are turned off (if any).
* It takes into account whether engines are active and have fuel available to run.
* It projects fuel use and compensates for increasing acceleration as fuel mass is burned.
* It uses the scarcest resource as the limit of fuel supply (e.g. if you're going to run out of LF before O)
* It takes the "infinite fuel" cheat into account, if that's turned on.
* If the burn exceeds your available dV, it shows the time in a "warning" format.
* It allows for engines that aren't parallel (e.g. if you have an active engine pointing the wrong way, it ups the estimate).


## The "countdown" indicator
For maneuver nodes and closest-approach, the mod displays a "countdown" indicator. This is a little row of green dots, immediately below the estimated burn time. This row of dots counts down until it's time to start your burn: when the last dot disappears, start the burn.

![countdown](https://raw.githubusercontent.com/KSPSnark/BetterBurnTime/master/screenshots/countdown.png)

The display is logarithmic.  The last three (biggest, leftmost) dots are in seconds:  3, 2, 1, go.  After the first three dots, it's 5 seconds, 10 seconds, 15 seconds, then it doubles for each dot after that.


**Note:** No countdown indicator is currently shown for the "time to impact" indicator; this is because "when should I start?" is more complex, depending on a lot of factors including your descent angle, TWR, etc.  This feature may eventually be added, but until then, you're on your own.

If you don't like this indicator, you can customize its appearance, make it numeric rather than graphic, or turn it off completely (see "How to configure", below).


## The "insufficient fuel" warning

Normally, the mod displays estimated burn time like this (48 seconds in this example):

**Est. Burn: 48s**

If the mod decides that you don't have enough dV to do the specified maneuver, it will display the time like this instead:

**Est. Burn: (~48s)**

Note that it won't do this if you have the "infinite fuel" cheat turned on (since then you always have enough dV!)



## The time-to-impact indicator
Under the right circumstances, the mod will display a "time until impact" indicator (instead of "time until maneuver"), along with an estimated burn time which is how long your engine would need to kill your velocity at ground level.

![Impact tracker](https://raw.githubusercontent.com/KSPSnark/BetterBurnTime/master/screenshots/impact.png)

All of the following conditions must be met for this indicator to be displayed:

* The impact tracker isn't disabled via settings (see "Settings", below)
* You don't have a maneuver node set.
* The planet/moon whose SoI you're in has no atmosphere.  (Someday I may release an update to enable the impact indicator when it's in atmosphere, but not right now.  It gets ugly and would significantly complicate the calculations.)
* You're on a trajectory that intersects the surface.
* You're falling by at least 2 m/s.
* The time of impact is no more than 120 seconds away (though you can tweak this with settings, see below).

Note that the time-to-impact is based on the assumption that you don't do a retro-burn and just coast to your doom.  So if you're figuring out "when do I start my retro-burn to land," you'll generally want to wait a little bit after the point at which time-to-impact equals estimated burn time.


## The time-to-closest-approach indicator
Under the right circumstances, the mod will display a "time until closest approach" indicator (instead of "time until maneuver"), along with an estimated burn time to match velocity with the target.

![Closest approach tracker](https://raw.githubusercontent.com/KSPSnark/BetterBurnTime/master/screenshots/approach.png)

All of the following conditions must be met for this indicator to be displayed:

* The approach tracker isn't disabled via settings (see "Settings", below)
* You don't have a maneuver node set
* The impact tracker (see above) isn't displaying time-to-impact
* You have a target, which is a vessel (e.g. not a planet)
* Neither you nor your target is landed
* You have an upcoming approach within 10 km distance
* The closest approach is no more than 15 minutes from now
* You're not within 200 meters of the target and going under 10 m/s, or within 400 meters and going under 1 m/s

## Caveats
There's a reason this mod is called *BetterBurnTime* and not *PerfectBurnTime*.  There are some conditions that it does *not* handle, as follows:

#### Doesn't predict staging
The mod bases its acceleration and dV estimates on your *current* configuration. It doesn't know about whether or when you're going to stage in the future.

Therefore, if you're going to be staging during the burn, this can cause a couple of inaccuracies:

* Underestimated burn time:  The mod's estimate is based only on the engines that are *currently* active. If your current stage is high-thrust and the next one will be low-thrust, then the mod will underestimate burn time because it's assuming that the whole burn will be with your current (high-thrust) engine.
* False alarm for dV warning:  The mod assumes you're going to be burning all fuel on the current stage. It doesn't allow for the mass you're going to drop when you jettison spent stages. Therefore, if you stage during the burn, the mod will underestimate how much dV your craft can handle, and may show the dV warning even though you're fine.

#### Doesn't predict fuel flow
The mod doesn't know what your fuel is going to do.  It naively assumes that all fuel on the ship (that hasn't been turned off by disabling the tank) is available to all active engines.  Therefore, there are a couple of situations it won't handle:

* If you have a multi-stage rocket, the mod assumes that *all* fuel is available to your *current* stage.  It will base its estimate of "available dV" on that.
* If you have multiple engines active now, but some of them are going to run out of fuel before others due to fuel flow issues, the mod doesn't predict that. It assumes that all fuel is available to all engines for the duration of the burn, and so it would underestimate the time in that case. (However, when the engines actually run out of fuel, the mod would immediately revise its estimate upwards.)

#### Ignores zero-density resources (e.g. electricity)
The mod assumes that any resources you have that don't have mass (e.g. electricity) are replenishable and therefore don't need to be tracked. Therefore, if you have an ion-powered craft and you're going to run out of electricity, the mod won't predict that. It will assume that you're going to have full electricity for the duration of the burn.

#### Time to impact is very simplistic
The calculations for determining when your ship will hit the surface are very simple.  It looks at the elevation directly under the ship, and at your current vertical speed.  It corrects for the acceleration of gravity, but nothing else.  This means that if you're flying over rough terrain, the time-to-impact indicator will be irregular (it will suddenly get shorter when you're flying over an ascending slope, or longer when you're flying over a descending slope).  If you're hurtling horizontally and about to smack into the side of a mountain range looming up in front of you, the mod has no clue.  Be warned.

The mod does make a very rudimentary attempt to keep track of where the *bottom* of your ship is, so that impact time will be actual impact time and not when your probe core up top would hit.  It's only a very rough approximation, though, so don't count on pinpoint accuracy at low speeds.

**Important:** The time-to-impact estimate takes into account your current velocity and the acceleration of gravity, and that's it.  It deliberately does *not* take into account the acceleration of your engines, if you're
firing them.  It's an estimate of "how long would I take to smash into the ground if I turned off all my engines."  So when you're retro-burning to land, the actual time to reach the ground will be **longer** than the displayed estimate, depending on things such as your TWR, throttle setting, angle of approach, etc.  So if you want to time your burn so that you reach zero velocity right when you get to ground level, you'll need to wait a little bit past the point where the estimated time to impact equals the estimated burn time.


## Simple vs. complex acceleration
By default, this mod uses a "complex" calculation of burn time that takes into account that your acceleration will increase as you burn fuel mass.  This is what allows the mod to produce accurate burn times.

There are certain circumstances in which the mod will drop down to a "simple" calculation that just assumes constant acceleration based on your current thrust and mass:

* There is a configuration option you can use to force the mod to use only simple acceleration all the time (see "How to configure," below).
* If the "infinite fuel" cheat is turned on, the mod uses simple acceleration because no fuel will be consumed.
* If the dV required for the maneuver exceeds the mod's calculation of available dV. Then it will assume that you'll have complex acceleration until you're out of fuel, and applies simple-acceleration math beyond that point.


## How to configure

After the first time you run KSP with the mod installed, there will be a configuration file located at under this location in your KSP folder, which you can edit to adjust settings:

GameData/BetterBurnTime/PluginData/BetterBurnTime/config.xml

The following settings are supported:

* **UseSimpleAcceleration:** By default, this is set to 0, meaning that the mod will use complex acceleration in its calculations.  If you set it to 1, then you will force the mod to use simple acceleration for all its calculations all the time.
* **ShowImpactTracker:** By default, this is set to 1.  If you set it to 0, then you will disable the "time to impact" display.
* **MaxTimeToImpact:** This is the maximum time, in seconds, that the impact tracker will predict a collision with terrain.  By default, it's 120 (two minutes). You can raise or lower this.  (Has no effect if ShowImpactTracker is set to 0, since then all impact tracking is turned off.)
* **ShowClosestApproachTracker:** By default, this is set to 1. If you set it to 0, then you will disable the "time until closest approach" display.
* **MaxTimeUntilEncounter:** This is the maximum time, in seconds, that the closest-approach tracker will predict a closest approach. By default, it's 900 (fifteen minutes).
* **MaxClosestApproachDistanceKm:** This is the maximum distance, in kilometers, that a closest approach can be for the closest-approach tracker to show a prediction. By default, it's 10 km.
* **MinTargetDistanceMeters:** The target must be at least this many meters away from your ship for the closest-approach tracker to show a prediction. By default, it's 200 meters.
* **FormatSeconds, etc.:** Various entries named "Format" are used for formatting the time display. You can edit these to change the way time is displayed. See the [.NET numeric formatting rules](https://msdn.microsoft.com/en-us/library/0c899ak8.aspx) for details.
* **CountdownText:** This string is used for displaying the "countdown" indicator. You can customize this to suit yourself, just be sure to separate the "pips" with whitespace.
    * To make it display a numeric value rather than a graphic string of "pips", just include "{0}" in the string. For example: "Start burn in {0}"
    * To turn off the countdown indicator completely, set this to an empty string.
* **CountdownTimes:** This string is a comma-delimited list of threshold times (in seconds) for displaying the number of pips in the countdown indicator. (Ignored if you're using a numeric countdown.)

-------
#### Acknowledgments
Thanks to [FullMetalMachinist](http://forum.kerbalspaceprogram.com/index.php?/profile/156531-fullmetalmachinist/) in the KSP forums for the [excellent suggestion](http://forum.kerbalspaceprogram.com/index.php?/topic/126111-105-betterburntime-v12-accurate-burn-time-indicator-on-navball-no-more-na/&page=4#comment-2422659) of using a row of dots to show the countdown-to-start-burn!  Ask and ye shall receive.  Thanks also to [Gen. Jack D. Ripper](http://forum.kerbalspaceprogram.com/index.php?/profile/144882-gen-jack-d-ripper/) for [usability suggestions](http://forum.kerbalspaceprogram.com/index.php?/topic/126111-105-betterburntime-v13-accurate-burn-time-indicator-on-navball-no-more-na/&do=findComment&comment=2425421) that led me to the updated countdown design.