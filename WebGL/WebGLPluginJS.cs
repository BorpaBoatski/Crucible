using System.Runtime.InteropServices;

public static class WebGLPluginJS
{
    [DllImport("__Internal")]
    public static extern void GAME_LOADED();
    [DllImport("__Internal")]
    public static extern void GAME_STOPPED_ERROR(string _Error);
    [DllImport("__Internal")]
    public static extern void GAME_STOPPED(string _Results);
    [DllImport("__Internal")]
    public static extern void GAME_CLOSE();
}
