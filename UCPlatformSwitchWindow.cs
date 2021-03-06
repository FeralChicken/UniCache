//-------------------------------------------------------------------------------------
// UniCache - Fast Build Target switching for Unity3D
// Author: Paul New (2014)
//-------------------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System;

namespace UniCache
{

//-------------------------------------------------------------------------------------
public class UCPlatformSwitchWindow : EditorWindow 
{
	static UCPlatformSwitchWindow Instance;
	bool m_DoSwitch;
	BuildTarget m_SelectedBuildTarget;

	//*********************************************************************************
	[MenuItem ("UniCache/Switch Platform")]
	static void Init()
	{
		if(Instance == null)
		{
			Instance = (UCPlatformSwitchWindow)EditorWindow.GetWindow(typeof(UCPlatformSwitchWindow));
		}
	}

	//*********************************************************************************
	void OnGUI() 
	{
		Array targets = Enum.GetValues(typeof(BuildTarget));
		foreach(BuildTarget build_target in targets)
		{
			if(GUILayout.Button(build_target.ToString()))
			{
				m_DoSwitch = true;
				m_SelectedBuildTarget = build_target;
				break;
			}
		}
	}

	//*********************************************************************************
	void Update()
	{
		if(m_DoSwitch)
		{
			m_DoSwitch = false;
			if(m_SelectedBuildTarget == EditorUserBuildSettings.activeBuildTarget)
			{
				Debug.Log("You're already using that target");
			}
			else
			{
				UniCache.SwitchToBuildTarget(m_SelectedBuildTarget);
			}

		}
	}
}

} // namespace UniCache