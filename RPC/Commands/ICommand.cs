using NetDiscordRpc.RPC.Payload;

namespace NetDiscordRpc.RPC.Commands
{
    internal interface ICommand
    {
        IPayload PreparePayload(long nonce);
    }
}