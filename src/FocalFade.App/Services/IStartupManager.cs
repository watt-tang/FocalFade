namespace FocalFade.Services;

public interface IStartupManager
{
    bool IsRegisteredForStartup();
    bool SetStartupRegistration(bool enable);
}
