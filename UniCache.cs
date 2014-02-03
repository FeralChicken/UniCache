//-------------------------------------------------------------------------------------
// UniCache - Fast Build Target switching for Unity3D
// Copyright (c) 2014 FanDabbieDozie Studios
//-------------------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace UniCache
{
	//------------------------------------------------------------------------------------
	public class UniCache 
	{
		// As best as I know, these are all of the texture/mesh/audio formats supported by Unity.
		// Switching build targets will cause these to be reimported into a platform-friendly format.
		// Therefore, they're the things we want to cache.
		private static string[] UNITY_SUPPORTED_TEXTURE_FORMATS = { "*.png", "*.jpg", "*.jpeg", "*.rgb", "*.tga", "*.targa", ".gif", "*.tiff", "*.tif", "*.bmp", "*.iff", "*.pict", "*.psd", "*.exr" };
		private static string[] UNITY_SUPPORTED_MESH_FORMATS = { "*.mtl", "*.obj", "*.blend", "*.fbm", "*.fbx", "*.3ds", "*.mb", "*.ma", "*.max", "*.c4d", "*.collada", "*.dxf" };
		private static string[] UNITY_SUPPORTED_AUDIO_FORMATS = { "*.wav", "*.mp3", "*.aif", "*.aiff", "*.ogg" };

		//*****************************************************************************
		public static bool SwitchToBuildTarget(BuildTarget new_build_target)
		{
			// Figure out where Unity keeps it's metadata files.
			// These change whenever a file gets reimported (e.g. during a platform switch...)
			// so these are the files we eventually want to copy into our platform-specific caches.
			string unity_metadata_root = Path.Combine(Application.dataPath, "../Library/metadata");
			unity_metadata_root = Path.GetFullPath(unity_metadata_root);
			if (!Directory.Exists(unity_metadata_root))
			{
				// If the Unity-level metadata directory doesn't exist then... uh... flunge?
				UCDebug.LogError(string.Format("Unity metadata cache {0} doesn't exist?", unity_metadata_root));
				return false;
			}

			if(!CacheCurrentBuildTarget(unity_metadata_root))
			{
				// FIXME: This should still give the user the option of doing a normal target switch.
				UCDebug.LogError("Couldn't cache the current target");
				return false;
			}

			FillBuildTargetFromCache(unity_metadata_root, new_build_target);
		
			// Now we've populated as much of the Unity metadata library as we can from our cache,
			// so we can let Unity go ahead with a normal target switch. At this point the only things
			// which will be reimported are assets which are genuinely new/updated since the last switch
			// and would be stale in our cache.
			EditorUserBuildSettings.SwitchActiveBuildTarget(new_build_target);
			return true;
		}

		//*****************************************************************************
		private static Dictionary<string, FileInfo> BuildFileInfoDict(string root_directory)
		{
			Dictionary<string, FileInfo> info_dict = new Dictionary<string, FileInfo>();

			string[] found_files = Directory.GetFiles(root_directory, "*", SearchOption.AllDirectories);
			foreach(string file in found_files)
			{
				string relative_filename = file.Substring(root_directory.Length+1);
				info_dict[relative_filename] = new FileInfo(file);
			}

			return info_dict;
		}

		//*****************************************************************************
		// Find every texture, mesh and audio asset under the project's root path.
		// Unity apparently doesn't give you a way to programatically find all of it's 
		// supported asset extensions, so we're having to rely on our own list of file 
		// extensions. Robust, huh?
		private static List<string> CollectProjectAssets()
		{
			List<string> found_assets = new List<string>();

			foreach(string texture_format in UNITY_SUPPORTED_TEXTURE_FORMATS)
			{
				string[] found_textures = Directory.GetFiles(Application.dataPath, texture_format, SearchOption.AllDirectories);
				found_assets.AddRange(found_textures);
			}

			foreach(string mesh_format in UNITY_SUPPORTED_MESH_FORMATS)
			{
				string[] found_meshes = Directory.GetFiles(Application.dataPath, mesh_format, SearchOption.AllDirectories);
				found_assets.AddRange(found_meshes);
			}

			foreach(string audio_format in UNITY_SUPPORTED_AUDIO_FORMATS)
			{
				string[] found_audio = Directory.GetFiles(Application.dataPath, audio_format, SearchOption.AllDirectories);
				found_assets.AddRange(found_audio);
			}

			return found_assets;
		}

		//*****************************************************************************
		// Before switching targets, we want to cache all of the current Unity metadata files
		// under <project>/Library in our own platform-specific directory structure.
		private static bool CacheCurrentBuildTarget(string unity_metadata_root)
		{
			// Now, work out the root path of where we're going to cache our data, based on the *current* build target
			// This directory might not exist yet if this is the first time that the user has switched away from
			// this platform.
			string uc_cache_root = Path.Combine(Application.dataPath, "../UniCacheData/" + EditorUserBuildSettings.activeBuildTarget.ToString());
			uc_cache_root = Path.GetFullPath(uc_cache_root);
			if (!Directory.Exists(uc_cache_root))
			{
				try
				{
					Directory.CreateDirectory(uc_cache_root);
				}
				catch (System.Exception e)
				{
					UCDebug.LogError("UniCache: Couldn't create cache directory: " + uc_cache_root + "\nError: " + e.ToString());
					return false;
				}
			}

			Dictionary<string, FileInfo> platform_cached_assets = BuildFileInfoDict(uc_cache_root);
			List<string> project_assets = CollectProjectAssets();
			int curr_file_index = 0;
			foreach(string project_asset in project_assets)
			{
				string asset_relative_path = project_asset.Substring(Application.dataPath.Length-6); // up to "assets/". Ew.
				string guid = AssetDatabase.AssetPathToGUID(asset_relative_path);

				// Ugh, Unity puts it's cached files in directories named after the first 2 chars of the
				// asset's GUID.
				string relative_guid_filepath = Path.Combine(guid.Substring(0, 2), guid);

				string source_cache_filepath = Path.GetFullPath(Path.Combine(unity_metadata_root, relative_guid_filepath));
				string dest_cache_filepath = Path.GetFullPath(Path.Combine(uc_cache_root, relative_guid_filepath));

				if(platform_cached_assets.ContainsKey(relative_guid_filepath))
				{
					// The platform cache already contains this file. We only need to copy over it if
					// the asset it references has changed since we last cached this platform.
					FileInfo asset_fileinfo = new FileInfo(project_asset);
					if(asset_fileinfo.LastWriteTimeUtc > platform_cached_assets[relative_guid_filepath].LastWriteTimeUtc)
					{
						UCDebug.Log("Overwriting cached file: " + asset_relative_path);
						File.Copy(source_cache_filepath, dest_cache_filepath, true);
					}
					else
					{
						UCDebug.Log("File is up to date in cache: " + asset_relative_path);
					}
				}
				else
				{
					UCDebug.Log(string.Format("Copying new cache entry: {0} to: {1}", source_cache_filepath, dest_cache_filepath));
					Directory.CreateDirectory(Path.GetDirectoryName(dest_cache_filepath));
					File.Copy(source_cache_filepath, dest_cache_filepath, false); 
				}

				EditorUtility.DisplayProgressBar("UniCache",
						"Saving current target to cache",
						(float)curr_file_index / (float)project_assets.Count);

				++curr_file_index;
			}

			EditorUtility.ClearProgressBar();

			return true;
		}

		//*****************************************************************************
		private static void FillBuildTargetFromCache(string unity_metadata_root, BuildTarget build_target)
		{
			string uc_cache_root = Path.Combine(Application.dataPath, "../UniCacheData/" + build_target.ToString());
			uc_cache_root = Path.GetFullPath(uc_cache_root);

			if(!Directory.Exists(uc_cache_root))
			{
				return;
			}

			string unity_project_path = Application.dataPath.Substring(0, Application.dataPath.Length - 6);

			int curr_file_index = 0;
			Dictionary<string, FileInfo> platform_cached_assets = BuildFileInfoDict(uc_cache_root);
			foreach(var cached_asset in platform_cached_assets)
			{
				// GUIDToAssetPath returns a valid path even if the asset doesn't exist.
				// (I.e. it returns the path where the asset *would* go were it to exist.)
				string asset_path = AssetDatabase.GUIDToAssetPath(cached_asset.Value.Name);
				if(!File.Exists(asset_path))
				{
					// It looks like the asset doesn't exist in the project any more.
					// That means we can delete it from our cache.
					UCDebug.Log(string.Format("Deleting old cached file: {0} for source {1} ", cached_asset.Key, asset_path));
					cached_asset.Value.Delete();
				}
				else
				{
					// Check the timestamp of the real asset. If it's newer than our cached version, then
					// our cache is stale and we shouldn't copy the file. This is fine, it just means that
					// the asset will be reimported normally when we do the real platform switch later.
					string real_asset_path = Path.Combine(unity_project_path, asset_path);
					if(File.GetLastWriteTimeUtc(real_asset_path) <= cached_asset.Value.LastWriteTimeUtc)
					{
						string cache_destination = Path.Combine(unity_metadata_root, cached_asset.Key);
						UCDebug.Log(string.Format("Copying from {0} to {1}", cached_asset.Value.FullName, cache_destination));
						cached_asset.Value.CopyTo(cache_destination, true);
					}
				}

				EditorUtility.DisplayProgressBar("UniCache",
												"Filling new build target from cache",
												(float)curr_file_index / (float)platform_cached_assets.Count);
				++curr_file_index;
			}

			EditorUtility.ClearProgressBar();
		}
	}
} // namespace UniCache