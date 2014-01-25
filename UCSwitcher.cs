using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace UniCache
{
//-------------------------------------------------------------------------------------
public class UCSwitcher 
{
	//*********************************************************************************
	public static bool SwitchToBuildTarget(BuildTarget new_build_target)
	{
		string unity_metadata_root = "";
		string uc_cache_root = "";

		if(!SetupCacheDirs(new_build_target, ref unity_metadata_root, ref uc_cache_root))
		{
			return false;
		}

		Debug.Log(string.Format("Unity cache path: {0}\nUniCachePath: {1}", unity_metadata_root, uc_cache_root));
		EditorUserBuildSettings.SwitchActiveBuildTarget(new_build_target);

		return true;
	}

	//*********************************************************************************
	private static bool SetupCacheDirs(BuildTarget new_build_target, ref string unity_metadata_root, ref string uc_cache_root)
	{
		// Figure out where Unity keeps it's metadata files.
		// These change whenever a file gets reimported (e.g. during a platform switch...)
		// so these are the files we eventually want to copy into our platform-specific caches.
		unity_metadata_root = Path.Combine(Application.dataPath, "../Library/metadata");
		unity_metadata_root = Path.GetFullPath(unity_metadata_root);
		if(!Directory.Exists(unity_metadata_root))
		{
			// If the Unity-level metadata directory doesn't exist then... uh... flunge?
			Debug.LogError(string.Format("Unity metadata cache {0} doesn't exist?", unity_metadata_root));
			return false;
		}

		// Now, work out the root path of where we're going to cache our data, based on the *current* build target
		// This directory might not exist yet if this is the first time that the user has switched away from
		// this platform.
		uc_cache_root = Path.Combine(Application.dataPath, "../UniCacheData/" + EditorUserBuildSettings.activeBuildTarget.ToString());
		uc_cache_root = Path.GetFullPath(uc_cache_root);
		if(!Directory.Exists(uc_cache_root))
		{
			try
			{
				Directory.CreateDirectory(uc_cache_root);
			}
			catch(Exception e)
			{
				Debug.LogError("UniCache: Couldn't create cache directory: " + uc_cache_root + "\nError: " + e.ToString());
				return false;
			}
		}

		return true;
	}	
}

} // namespace UniCache