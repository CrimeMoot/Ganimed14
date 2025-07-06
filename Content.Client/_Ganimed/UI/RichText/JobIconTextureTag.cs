using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Ganimed.UI.RichText
{
    public sealed class TextureTag : IMarkupTag
    {
        public string Name => "tex";

        public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        {
            control = null;

            if (!node.Attributes.TryGetValue("path", out var rawPath))
            {
                return false;
            }

            var path = SanitizeString(rawPath.ToString());
            control = DrawIcon(path);

            return true;
        }

        private Control DrawIcon(string path)
        {
            var textureRect = new TextureRect
            {
                TexturePath = path,
                Stretch = TextureRect.StretchMode.Scale,
                MinSize = new Vector2(20, 20),
                MaxSize = new Vector2(20, 20),
                TextureScale = Vector2.One
            };

            return textureRect;
        }

        private static string SanitizeString(string input)
        {
            return input.Replace("=", "")
                        .Replace(" ", "")
                        .Replace("\"", "");
        }
    }
}
