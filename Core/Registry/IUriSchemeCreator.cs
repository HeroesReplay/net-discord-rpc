namespace NetDiscordRpc.Core.Registry
{
    public interface IUriSchemeCreator
    {
        bool RegisterUriScheme(UriSchemeRegister register);
    }
}