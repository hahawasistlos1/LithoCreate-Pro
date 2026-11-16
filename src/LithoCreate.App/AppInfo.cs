namespace LithoCreate.App;
public static class AppInfo
{
    public static readonly string Version=typeof(AppInfo).Assembly.GetName().Version!.ToString(3);
    public static string VersionLabel=>"Version "+Version+" · Windows x64";
    public static string ProductLabel=>"LithoCreate Pro · Version "+Version;
}
