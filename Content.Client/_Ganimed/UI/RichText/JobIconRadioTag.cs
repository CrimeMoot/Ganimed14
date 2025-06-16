using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Ganimed.UserInterface.RichText;

public sealed class RadioIconTag : IMarkupTag
{
    [Dependency] private readonly IResourceCache _cache = default!;

    public string Name => "radicon";

    /// <inheritdoc/>
    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {

        if (!node.Attributes.TryGetValue("path", out var rawPath))
        {
            control = null;
            return false;
        }

        if (!node.Attributes.TryGetValue("scale", out var scale) || !scale.TryGetLong(out var scaleValue))
        {
            scaleValue = 1;
        }

        control = DrawIcon(rawPath.ToString(), scaleValue.Value);
        return true;
    }

    private Control DrawIcon(string path, long scaleValue)
    {
        var texture = new TextureRect();

        path = ClearString(path);

        texture.TexturePath = path;
        texture.TextureScale = new Vector2(scaleValue, scaleValue);

        return texture;
    }
    private static string ClearString(string str)
    {
        str = str.Replace("=", "");
        str = str.Replace(" ", "");
        str = str.Replace("\"", "");

        return str;
    }
}
