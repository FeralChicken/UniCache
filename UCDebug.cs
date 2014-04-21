//-------------------------------------------------------------------------------------
// UniCache - Fast Build Target switching for Unity3D
// Author: Paul New (2014)
//-------------------------------------------------------------------------------------

//#define ENABLE_UNICACHE_LOGGING

//------------------------------------------------------------------------------------
public class UCDebug
{
	//*****************************************************************************
	public static void Log(object message, UnityEngine.Object context = null)
	{
		#if ENABLE_UNICACHE_LOGGING
		UnityEngine.Debug.Log(message, context);
		#endif
	}

	//*****************************************************************************
	public static void LogWarning(object message, UnityEngine.Object context = null)
	{
		#if ENABLE_UNICACHE_LOGGING
		UnityEngine.Debug.LogWarning(message, context);
		#endif
	}

	//*****************************************************************************
	public static void LogError(object message, UnityEngine.Object context = null)
	{
		#if ENABLE_UNICACHE_LOGGING
		UnityEngine.Debug.LogError(message, context);
		#endif
	}

	//*****************************************************************************
	public static void LogException(System.Exception exception, UnityEngine.Object context = null)
	{
		#if ENABLE_UNICACHE_LOGGING
		UnityEngine.Debug.LogException(exception, context);
		#endif
	}
}