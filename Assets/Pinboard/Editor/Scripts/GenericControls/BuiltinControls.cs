using UnityEngine;

namespace Pinboard
{
	public static class BuiltinControls
	{
		[GenericControl("Rendering/Ambient Color", true)]
		public static bool ValidateAmbientColor() => true;

		[GenericControl("Rendering/Ambient Color")]
		public static Color GetAmbientColor() => RenderSettings.ambientLight;

		[GenericControl("Rendering/Ambient Color")]
		public static void SetAmbientColor(Color color) => RenderSettings.ambientLight = color;
	}
}
