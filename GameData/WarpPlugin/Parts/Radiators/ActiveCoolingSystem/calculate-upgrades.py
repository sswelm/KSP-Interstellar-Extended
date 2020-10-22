from z3 import *

intakeLqdDensity = 0.001
intakeLqdSpHC = 4183
intakeAtmDensity = 0.005
intakeAtmSpHC = 10

minTotalSurfaceArea = 200
maxTotalSurfaceArea = 300

minTotalLqdArea = 50
maxTotalLqdArea = 100

minPumpSpeed = 300
maxPumpSpeed = 2000

s = Solver()

pumpSpeedv0 = Real("pumpSpeedv0")
surfaceAreav0 = Real("surfaceAreav0")
lqdStoragev0 = Real("lqdStoragev0")

pumpSpeedv1 = Real("pumpSpeedv1")
surfaceAreav1 = Real("surfaceAreav1")
lqdStoragev1 = Real("lqdStoragev1")

pumpSpeedv2 = Real("pumpSpeedv2")
surfaceAreav2 = Real("surfaceAreav2")
lqdStoragev2 = Real("lqdStoragev2")

pumpSpeedv3 = Real("pumpSpeedv3")
surfaceAreav3 = Real("surfaceAreav3")
lqdStoragev3 = Real("lqdStoragev3")

pumpSpeedv4 = Real("pumpSpeedv4")
surfaceAreav4 = Real("surfaceAreav4")
lqdStoragev4 = Real("lqdStoragev4")

s.add(pumpSpeedv0 > 1, surfaceAreav0 == 1, lqdStoragev0 == 1)
s.add(pumpSpeedv0 < 20, surfaceAreav0 < 20, lqdStoragev0 < 50)
s.add(pumpSpeedv1 > 1, surfaceAreav1 > 1, lqdStoragev1 > 1)
s.add(pumpSpeedv2 > 1, surfaceAreav2 > 1, lqdStoragev2 > 1)
s.add(pumpSpeedv3 > 1, surfaceAreav3 > 1, lqdStoragev3 > 1)
s.add(pumpSpeedv4 > 1, surfaceAreav4 > 1, lqdStoragev4 > 1)

s.add(lqdStoragev2 < 100, lqdStoragev1 < 100, lqdStoragev3 < 100, lqdStoragev4 < 100)
s.add(pumpSpeedv1 < 100, pumpSpeedv2 < 200, pumpSpeedv3 < 300, pumpSpeedv4 < 400)
s.add(surfaceAreav1 < 1000, surfaceAreav2 < 1000, surfaceAreav3 < 1000, surfaceAreav4 < 1000)

#s.add(surfaceAreav4 > surfaceAreav3)
#s.add(surfaceAreav3 > surfaceAreav2)
s.add(surfaceAreav2 > surfaceAreav1)
#s.add(surfaceAreav1 > surfaceAreav0)

s.add(pumpSpeedv4 > pumpSpeedv3)
#s.add(pumpSpeedv3 > pumpSpeedv2)
#s.add(pumpSpeedv2 > pumpSpeedv1)
#s.add(pumpSpeedv1 > pumpSpeedv0)

#s.add(lqdStoragev4 > lqdStoragev3)
s.add(lqdStoragev3 > lqdStoragev2)
#s.add(lqdStoragev2 > lqdStoragev1)
#s.add(lqdStoragev1 > lqdStoragev0)

#s.add(surfaceAreav0 + surfaceAreav1 + surfaceAreav2 + surfaceAreav3 + surfaceAreav4 > minTotalSurfaceArea)
s.add(surfaceAreav0 + surfaceAreav1 + surfaceAreav2 + surfaceAreav3 + surfaceAreav4 < maxTotalSurfaceArea)

#s.add(lqdStoragev0 + lqdStoragev1 + lqdStoragev2 + lqdStoragev3 + lqdStoragev4 > minTotalLqdArea)
#s.add(lqdStoragev0 + lqdStoragev1 + lqdStoragev2 + lqdStoragev3 + lqdStoragev4 > maxTotalLqdArea)

#s.add(pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3 + pumpSpeedv4 > minPumpSpeed)
#s.add(pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3 + pumpSpeedv4 < maxPumpSpeed)

atmos_modifier = 1
heat_modifier = (1979 - 314.4)

# var jetTech = Convert.ToInt32(hasJetUpgradeTech1) * 1.2f + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f * Convert.ToInt32(hasJetUpgradeTech3) + 2.0736f * Convert.ToInt32(hasJetUpgradeTech4) + 2.48832f * Convert.ToInt32(hasJetUpgradeTech5);
# jetTechBonus = 5 * (1 + (jetTech / 9.92992f));

def pump_speed_to_intakeatm(total, bonusLevel):
    jetTechBonus = 0
    if(bonusLevel > 0):
        jetTechBonus += 1.2
    if(bonusLevel > 1):
        jetTechBonus += 1.44
    if(bonusLevel > 2):
        jetTechBonus += 1.728
    if(bonusLevel > 3):
         jetTechBonus += 2.0736
    if(bonusLevel > 4):
        jetTechBonus += 2.48832

    jetTechBonus = 5 * (1 + (jetTechBonus / 9.92992))
    
    return total + (total * jetTechBonus) * 0.02

def calculate_dissipation_v1():
    return intakeLqdDensity * (lqdStoragev0 + lqdStoragev1) * \
           intakeLqdSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1) * (pumpSpeedv0 + pumpSpeedv1) * 0.0007

def air_calculate_dissipation_v1():
    return intakeAtmDensity * \
           pump_speed_to_intakeatm(pumpSpeedv0 + pumpSpeedv1, 1) * \
           intakeAtmSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1) * (pumpSpeedv0 + pumpSpeedv1) * \
           0.0005


def calculate_dissipation_v2():
    return (pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2) * \
            intakeLqdDensity * (lqdStoragev0 + lqdStoragev1 + lqdStoragev2) * \
           intakeLqdSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1 + surfaceAreav2) * \
            0.0007

def air_calculate_dissipation_v2():
    return intakeAtmDensity * \
           pump_speed_to_intakeatm(pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2, 2) * \
           intakeAtmSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1 + surfaceAreav2) * \
           (pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2) * \
           0.0005

def calculate_dissipation_v3():
    return intakeLqdDensity * \
           (lqdStoragev0 + lqdStoragev1 + lqdStoragev2 + lqdStoragev3) * \
           intakeLqdSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1 + surfaceAreav2 + surfaceAreav3) * \
           (pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3) * 0.0007

def air_calculate_dissipation_v3():
    return intakeAtmDensity * \
           pump_speed_to_intakeatm(pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3, 3) * \
           intakeAtmSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1 + surfaceAreav2 + surfaceAreav3) * \
           (pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3) * \
           0.0005

def calculate_dissipation_v4():
    return intakeLqdDensity * \
           (lqdStoragev0 + lqdStoragev1 + lqdStoragev2 + lqdStoragev3 + lqdStoragev4) * \
           intakeLqdSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1 + surfaceAreav2 + surfaceAreav3 + surfaceAreav3) * \
           (pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3 + pumpSpeedv4) * 0.0007

def air_calculate_dissipation_v4():
    return intakeAtmDensity * \
           pump_speed_to_intakeatm(pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3 + pumpSpeedv4, 4) * \
           intakeAtmSpHC * atmos_modifier * heat_modifier * \
           (surfaceAreav0 + surfaceAreav1 + surfaceAreav2 + surfaceAreav3 + surfaceAreav4) * \
           (pumpSpeedv0 + pumpSpeedv1 + pumpSpeedv2 + pumpSpeedv3 + pumpSpeedv4) * \
           0.0005



GW = (1000 * 1000)
MW = (100 * 1000)

lqd_v1_min = 1 * GW
lqd_v1_max = 3 * GW

air_v1_min = 200 * MW # 10 MW
air_v1_max = 400 * MW # 20 MW

lqd_v2_min = 5 * GW
lqd_v2_max = 7 * GW

air_v2_min = 300 * MW
air_v2_max = 500 * MW

lqd_v3_min = 12 * GW
lqd_v3_max = 15 * GW

air_v3_min = 600 * MW
air_v3_max = 1200 * MW

lqd_v4_min = 20 * GW
lqd_v4_max = 30 * GW

air_v4_min = 1.6 * GW
air_v4_max = 2 * GW


s.add(calculate_dissipation_v4() > lqd_v4_min)
s.add(calculate_dissipation_v4() < lqd_v4_max)


s.add(calculate_dissipation_v3() > lqd_v3_min)
s.add(calculate_dissipation_v3() < lqd_v3_max)


s.add(calculate_dissipation_v2() > lqd_v2_min)
s.add(calculate_dissipation_v2() < lqd_v2_max)


s.add(calculate_dissipation_v1() > lqd_v1_min)
s.add(calculate_dissipation_v1() < lqd_v1_max)

#s.add(air_calculate_dissipation_v1() > air_v1_min)
#s.add(air_calculate_dissipation_v1() < air_v1_max)

#s.add(air_calculate_dissipation_v2() > air_v2_min)
#s.add(air_calculate_dissipation_v2() < air_v2_max)

#s.add(air_calculate_dissipation_v3() > air_v3_min)
#s.add(air_calculate_dissipation_v3() < air_v3_max)


#s.add(air_calculate_dissipation_v4() > air_v4_min)
#s.add(air_calculate_dissipation_v4() < air_v4_max)


print(s.check())
print(s.model())

