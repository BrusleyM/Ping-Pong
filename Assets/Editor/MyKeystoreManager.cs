using UnityEditor;
public class MyKeystoreManager
{
    private readonly string _keystoreName;
    private readonly string _keystorePass;
    private readonly string _keyAliasName;
    private readonly string _keyAliasPass;

    public MyKeystoreManager(string keystoreName, string keystorePass, string keyAliasName, string keyAliasPass)
    {
        _keystoreName = keystoreName;
        _keystorePass = keystorePass;
        _keyAliasName = keyAliasName;
        _keyAliasPass = keyAliasPass;
    }

    public void ApplyKeystore()
    {
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = _keystoreName;
        PlayerSettings.Android.keystorePass = _keystorePass;
        PlayerSettings.Android.keyaliasName = _keyAliasName;
        PlayerSettings.Android.keyaliasPass = _keyAliasPass;
    }
}
