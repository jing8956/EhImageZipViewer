using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EhImageZipViewer;

public interface ISettingsService
{

}

internal class SettingsSrvice : ISettingsService
{
    private readonly IPreferences _preferences;

    public SettingsSrvice(IPreferences preferences)
    {
        _preferences = preferences;
    }

    public bool Test => _preferences.Get("key", false);


}
