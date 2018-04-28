Rem Batch skript to generate MM- and PM-cfg files for modular fuel-switcher
@echo off
setlocal DisableDelayedExpansion
set TAB=	
set INRESOURCES=IFSRFCresources.txt
set INMMFILE=IFSRFCtemplateMM.txt
set INPMFILE=IFSRFCtemplatePM.txt
set SEARCHTEXT01=batVar01
set SEARCHTEXT02=batVar02
set SEARCHTEXT03=batVar03
set OUTMMFILE=./PatchManager/ActiveMMPatches/IFSRFC
set OUTPMFILE=./PatchManager/IFSRFC
for /f "tokens=1,2,3,4 delims=;" %%A in ( '"type %INRESOURCES%"') do (
	set REPLACETEXT01=%%A
	set REPLACETEXT02=%%B
	set REPLACETEXT03=%%C
	set REPLACETEXT04=%%D
	call echo %%REPLACETEXT01%%, %%REPLACETEXT02%%, %%REPLACETEXT03%%, %%REPLACETEXT04%%
	for /f "tokens=1,* delims=¶" %%a in ( '"type %INMMFILE%"') do (
		set string=%%a
		setlocal EnableDelayedExpansion
		call echo %%string%%
		call set modified=%%string:%SEARCHTEXT01%=!REPLACETEXT01!%%
		call echo %%modified%%
		call set modified=%%modified:%SEARCHTEXT02%=!REPLACETEXT02!%%
		call echo %%modified%%
		call set modified=%%modified:%SEARCHTEXT03%=!REPLACETEXT03!%%
		call echo %%modified%%
		>> %OUTMMFILE%!REPLACETEXT01!!REPLACETEXT02!.cfg echo(!modified!
		endlocal
	)
	for /f "tokens=1,* delims=¶" %%a in ( '"type %INPMFILE%"') do (
		SET string=%%a
		setlocal EnableDelayedExpansion
		call echo %%string%%
		call set modified=%%string:%SEARCHTEXT01%=!REPLACETEXT01!%%
		call echo %%modified%%
		call set modified=%%modified:%SEARCHTEXT02%=!REPLACETEXT02!%%
		call echo %%modified%%
		call set modified=%%modified:%SEARCHTEXT03%=!REPLACETEXT03!%%
		call echo %%modified%%
		>> %OUTPMFILE%!REPLACETEXT01!!REPLACETEXT02!.cfg echo(!modified!
		endlocal
	)
	setlocal EnableDelayedExpansion
	call set "shortkey=%TAB%%TAB%#LOC_IFSRFC_PM_!REPLACETEXT02!_short = Add !REPLACETEXT02!"
	call set "longkey=%TAB%%TAB%#LOC_IFSRFC_PM_!REPLACETEXT02!_long = If active, IFS Radioactive Fuel Containers have a !REPLACETEXT04! setup."
	>> en-usRFC.cfg echo(!shortkey!
	>> en-usRFC.cfg echo(!longkey!
	endlocal
)