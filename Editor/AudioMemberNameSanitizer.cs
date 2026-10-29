using System.Text;

namespace Astra.Echo.Editor
{
    /// <summary>
    /// Helpers to convert arbitrary asset names (e.g. "ui/button-click 01")
    /// into a valid C# member identifier (<c>UiButtonClick01</c>) suitable
    /// for the generated <c>AudioId</c> registry.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "Core.Audio.Editor", "Core.Audio.Editor", "AudioMemberNameSanitizer")]
    public static class AudioMemberNameSanitizer
    {
        public static string ToPascalCaseIdentifier(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "_Unnamed";

            StringBuilder sb = new StringBuilder(raw.Length);
            bool nextUpper = true;
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c))
                {
                    if (nextUpper)
                    {
                        sb.Append(char.ToUpperInvariant(c));
                        nextUpper = false;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    nextUpper = true;
                }
            }

            if (sb.Length == 0) return "_Unnamed";
            if (char.IsDigit(sb[0])) sb.Insert(0, '_');
            return sb.ToString();
        }
    }
}
