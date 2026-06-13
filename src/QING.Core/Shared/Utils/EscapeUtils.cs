using System.Text.RegularExpressions;

namespace QING.Core;
public static class EscapeUtils {

    /// <summary>
    /// WPF XML 转义。
    /// </summary>
    public static string XmlEscape(string str) {
        if (str.StartsWithF("{")) str = "{}" + str; // #4187
        return str
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;")
            .Replace("\"", "&quot;").ReplaceLineEndings("&#xa;");
    }

    /// <summary>
    /// VB.NET 的 Like 关键字转义。
    /// </summary>
    public static string LikePatternEscape(string input) {
        var builder = new StringBuilder();
        foreach (char c in input) {
            if (c is '[' or ']' or '*' or '?' or '#') {
                builder.Append('[').Append(c).Append(']');
            } else {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// 正则表达式转义。
    /// 等同于 <see cref="Regex.Escape"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RegexEscape(string value) 
        => Regex.Escape(value);

    /// <summary>
    /// 正则表达式去转义。
    /// 等同于 <see cref="Regex.Unescape"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RegexUnescape(string value) 
        => Regex.Unescape(value);

    /// <summary>
    /// URL 转义。
    /// 等同于 <see cref="Uri.EscapeDataString"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UrlEscape(string value) 
        => Uri.EscapeDataString(value);

    /// <summary>
    /// URL 去转义。
    /// 等同于 <see cref="Uri.UnescapeDataString"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UrlUnescape(string value) 
        => Uri.UnescapeDataString(value);

    /// <summary>
    /// 表单转义。
    /// 等同于 <see cref="WebUtility.UrlEncode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormUrlEscape(string value) 
        => WebUtility.UrlEncode(value);

    /// <summary>
    /// 表单去转义。
    /// 等同于 <see cref="WebUtility.UrlDecode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormUrlUnescape(string value) 
        => WebUtility.UrlDecode(value);

    /// <summary>
    /// HTML 转义。
    /// 等同于 <see cref="WebUtility.HtmlEncode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HtmlEscape(string value) 
        => WebUtility.HtmlEncode(value);

    /// <summary>
    /// HTML 去转义。
    /// 等同于 <see cref="WebUtility.HtmlDecode"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HtmlUnescape(string value) 
        => WebUtility.HtmlDecode(value);

}