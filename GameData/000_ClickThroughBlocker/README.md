Having clicked one too many times on one window, only to have it click on the parts underneath (in the Editor), 
or click on an unwanted item in flight, I decided to solve the problem with yet another mod

Mods which use the Click Through Blocker would need to be modified, and this would become a hard dependency for that mod.

The changes are very simple:

Replace all calls to GUILayout.Window with ClickThruBlocker.GUILayoutWindow, the parameters are identical
Replace all calls to GUI.Window with ClickThruBlocker.GUIWindow, the parameters are identical

How it works

Each call first calls the original method (ie: ClickThruBlocker.GUILayoutWindow will call GUILayout.Window).  After the call,
the position of the mouse is checked to see if it was on top of the window Rect, if it is, it then locks the controls so that clicks don't
pass through to any other window.

Usage

	Add the following to the top of the source:
		using ClickThroughFix;
	Replace calls to GUILayout.Window with ClickThruBlocker.GUILayoutWindow
	Replace calls to GUI.Window with ClickThruBlocker.GUIWindow

Functions - Identical to the GUI and GUILayout versions
	Rect GUILayoutWindow(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, GUIStyle style, params GUILayoutOption[] options);
	Rect GUILayoutWindow(int id, Rect screenRect, GUI.WindowFunction func, Texture image, GUIStyle style, params GUILayoutOption[] options);
	Rect GUILayoutWindow(int id, Rect screenRect, GUI.WindowFunction func, string text, GUIStyle style, params GUILayoutOption[] options);
	Rect GUILayoutWindow(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, params GUILayoutOption[] options);
	Rect GUILayoutWindow(int id, Rect screenRect, GUI.WindowFunction func, Texture image, params GUILayoutOption[] options);
	Rect GUILayoutWindow(int id, Rect screenRect, GUI.WindowFunction func, string text, params GUILayoutOption[] options);

	Rect GUIWindow(int id, Rect clientRect, WindowFunction func, Texture image, GUIStyle style);
	Rect GUIWindow(int id, Rect clientRect, WindowFunction func, string text, GUIStyle style);
	Rect GUIWindow(int id, Rect clientRect, WindowFunction func, GUIContent content);
	Rect GUIWindow(int id, Rect clientRect, WindowFunction func, Texture image);
	Rect GUIWindow(int id, Rect clientRect, WindowFunction func, string text);
	Rect GUIWindow(int id, Rect clientRect, WindowFunction func, GUIContent title, GUIStyle style);

Additional functions

bool MouseIsOverWindow(Rect rect)   Returns true if the mouse is over the specified rectangle
