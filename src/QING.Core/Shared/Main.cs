namespace QING.Core;
public static class Main {

    public static void Init(BaseLogger? logger = null) {
        AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(5));
        if (logger != null) Logger.Instance = logger;
    }

}
